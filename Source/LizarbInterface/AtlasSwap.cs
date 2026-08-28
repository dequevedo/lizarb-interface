using System.IO;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Substitutes this mod's atlases at DRAW TIME rather than replacing the game's
    /// texture fields. Three reasons, each one learned the hard way:
    ///
    /// 1. A file in Textures/ does nothing. Widgets and TabRecord resolve their atlases
    ///    once in static constructors that run BEFORE mod content is registered, so the
    ///    game keeps drawing vanilla all session. Compare texture FORMAT, not size, to
    ///    spot this, because vanilla ButtonBG is also 64x64.
    /// 2. Writing those static fields crashes. Mod textures get destroyed
    ///    (ModContentHolder.ClearDestroy, Resources.UnloadUnusedAssets) and the field
    ///    is left wrapping a dead native object. It throws every frame, but only after
    ///    loading a game. Vanilla textures never die, so the fields keep them.
    /// 3. Initialisation must NOT happen on first touch: the DrawAtlas prefix fires on
    ///    the loading screen, before mod content exists, and a static constructor runs
    ///    once, caching vanilla-for-vanilla forever. Hence AtlasSwapInit.
    ///
    /// PNG bytes are read straight off disk, so these textures belong to this mod alone
    /// and are rebuilt from the bytes if Unity collects one.
    /// </summary>
    internal static class AtlasSwap
    {
        private sealed class Slot
        {
            public Texture2D Vanilla;   // identity key: what the game passes to DrawAtlas
            public byte[] Png;          // managed, cannot be destroyed by Unity
            public string Name;
            public Texture2D Ours;      // rebuilt on demand
            public bool Tiling;         // Repeat instead of Clamp, for pattern fills
            public bool Shared;         // lives in Skins/Shared, same for every theme
            public string Theme;        // which skin these bytes came from
        }

        // Field initialiser only. No static constructor work here, so an early touch
        // from the draw prefix cannot lock in a wrong answer (TRAP 3).
        private static Slot[] slots = new Slot[0];

        /// <summary>Textures with no vanilla counterpart, e.g. the window frame.</summary>
        private static readonly System.Collections.Generic.Dictionary<string, Slot> owned =
            new System.Collections.Generic.Dictionary<string, Slot>();

        private static string root;

        /// <summary>Folder of the currently selected skin.</summary>
        private static string SkinDir
        {
            get
            {
                string theme = LizarbInterfaceMod.Settings?.theme;
                if (theme.NullOrEmpty())
                {
                    theme = "Brass";
                }

                return Path.Combine(root, "Skins/" + theme);
            }
        }

        private static string CurrentTheme => LizarbInterfaceMod.Settings?.theme ?? "Brass";

        internal static bool Ready => slots.Length > 0;

        /// <summary>
        /// A texture this mod adds rather than replaces. Same rebuild-on-death handling
        /// as the swapped ones, so a window frame can never take the UI down.
        /// </summary>
        internal static Texture2D Own(string fileName, bool tiling = false)
        {
            return Load(fileName, tiling, shared: false);
        }

        /// <summary>
        /// A texture that is the same for every theme, from Skins/Shared. The
        /// architect icons are drawn white so the category colour tints them, which
        /// is what lets one set serve all fourteen skins.
        /// </summary>
        internal static Texture2D Shared(string fileName)
        {
            return Load(fileName, tiling: false, shared: true);
        }

        private static Texture2D Load(string fileName, bool tiling, bool shared)
        {
            if (root == null)
            {
                return null;
            }

            string key = shared ? "shared/" + fileName : fileName;
            if (!owned.TryGetValue(key, out Slot slot))
            {
                slot = new Slot { Name = fileName, Tiling = tiling, Shared = shared };
                owned[key] = slot;
            }

            EnsureLoaded(slot);
            return slot.Ours;
        }

        /// <summary>
        /// Brings a slot up to date, rebuilding the texture if it is missing, was
        /// destroyed by the engine, or belongs to another theme. This is what makes a
        /// theme switch work with no explicit invalidation call.
        /// </summary>
        private static void EnsureLoaded(Slot slot)
        {
            // Shared textures pin a constant here, so a theme change never reloads
            // them and the same instance is reused for the whole session.
            string theme = slot.Shared ? "Shared" : CurrentTheme;
            if (slot.Theme != theme)
            {
                slot.Theme = theme;
                slot.Png = ReadSkinFile(slot.Name, theme);
                slot.Ours = null;
            }

            // Unity's == is true for a destroyed object, so this also heals whatever
            // the engine collected since.
            if (slot.Ours == null)
            {
                slot.Ours = Build(slot);
            }
        }

        private static byte[] ReadSkinFile(string fileName, string theme = null)
        {
            string dir = theme == null ? SkinDir : Path.Combine(root, "Skins/" + theme);
            string path = Path.Combine(dir, fileName + ".png");
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }

            Log.Error("[LizarbInterface] missing skin file: " + path);
            return null;
        }

        /// <summary>
        /// Loads a texture from a specific skin, for the settings previews. Separate
        /// cache from the live slots so previewing a theme never disturbs the one in
        /// use.
        /// </summary>
        internal static Texture2D Preview(string theme, string fileName)
        {
            if (root == null)
            {
                return null;
            }

            string key = theme + "/" + fileName;
            if (previews.TryGetValue(key, out Texture2D tex) && tex != null)
            {
                return tex;
            }

            string path = Path.Combine(root, "Skins/" + theme + "/" + fileName + ".png");
            if (!File.Exists(path))
            {
                return null;
            }

            var slot = new Slot { Name = fileName, Png = File.ReadAllBytes(path) };
            tex = Build(slot);
            previews[key] = tex;
            return tex;
        }

        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> previews =
            new System.Collections.Generic.Dictionary<string, Texture2D>();

        internal static void Init(string rootDir)
        {
            root = rootDir;

            RuntimeHelpers.RunClassConstructor(typeof(Widgets).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(TabRecord).TypeHandle);

            slots = new[]
            {
                Make(typeof(Widgets), "ButtonBGAtlas", "ButtonBG"),
                Make(typeof(Widgets), "ButtonBGAtlasMouseover", "ButtonBGMouseover"),
                Make(typeof(Widgets), "ButtonBGAtlasClick", "ButtonBGClick"),
                Make(typeof(Widgets), "ButtonSubtleAtlas", "ButtonSubtleAtlas"),

                // These arrive through DrawAtlas too, so they cost one line each:
                // the slider rail, float menu rows and the tooltip balloon.
                Make(typeof(Widgets), "SliderRailAtlas", "SliderRail"),
                Make(typeof(TexUI), "FloatMenuOptionBG", "FloatMenuOptionBG"),
                Make(typeof(ActiveTip), "TooltipBGAtlas", "TooltipBG"),
            };
        }

        private static Slot Make(System.Type owner, string fieldName, string fileName)
        {
            var slot = new Slot
            {
                Name = fileName,
                Vanilla = AccessTools.StaticFieldRefAccess<Texture2D>(owner, fieldName),
            };

            // Bytes are loaded lazily by EnsureLoaded so a theme change picks them
            // up; only the identity key is resolved here.
            slot.Theme = null;

            // A missing PNG is worth a warning; a working one is not worth a line in
            // every player's log, so the success case is dev-mode only.
            bool found = File.Exists(Path.Combine(SkinDir, fileName + ".png"));
            if (!found)
            {
                Log.Warning("[LizarbInterface] no skin texture for " + fileName + "; using vanilla.");
            }
            else if (Prefs.DevMode)
            {
                Log.Message("[LizarbInterface] " + owner.Name + "." + fieldName +
                            " keyed on " + Describe(slot.Vanilla));
            }

            return slot;
        }

        /// <summary>Replacement for this texture, or null to leave it alone.</summary>
        internal static Texture2D For(Texture2D original)
        {
            if (original == null)
            {
                return null;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                Slot slot = slots[i];

                // Reference match, deliberately not texture.name: Object.name is a
                // native call that allocates a string, and this runs on every atlas
                // draw in the game.
                if (!ReferenceEquals(slot.Vanilla, original))
                {
                    continue;
                }

                EnsureLoaded(slot);
                return slot.Ours;
            }

            return null;
        }

        private static Texture2D Build(Slot slot)
        {
            if (slot.Png == null)
            {
                return null;
            }

            // NO mipmaps, deliberately. Unity picks the mip level from the LARGEST
            // derivative across the quad, and a 9-slice edge band is 32 texels wide
            // drawn into ~20px while being stretched hugely along the other axis. The
            // wide axis wins, the band drops to mip 1, and the fillet and the 1px
            // outline lose half their resolution: that is the blurred left and right
            // border. Anisotropic sampling would fix it, but UI has nothing to gain
            // from mips in the first place - the art carries no high frequencies.
            //
            // Sharp is what UI wants,
            // unlike the map art that ModContentLoader's import defaults were picked
            // for. Kept readable so Unity can restore it rather than dropping it.
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            tex.LoadImage(slot.Png);
            tex.name = slot.Name;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 0;
            tex.wrapMode = slot.Tiling ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        private static string Describe(Texture2D tex)
        {
            if (tex == null)
            {
                return "NULL";
            }

            return tex.name + " " + tex.width + "x" + tex.height + " " + tex.format +
                   " mips=" + tex.mipmapCount;
        }
    }

    /// <summary>
    /// Runs at the point RimWorld guarantees mod content is loaded. A separate type on
    /// purpose. See TRAP 3 on AtlasSwap.
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class AtlasSwapInit
    {
        static AtlasSwapInit()
        {
            if (LizarbInterfaceMod.RootDir == null)
            {
                Log.Error("[LizarbInterface] mod content root unknown; atlases left as vanilla.");
                return;
            }

            AtlasSwap.Init(LizarbInterfaceMod.RootDir);
            PlainTextures.Init();
        }
    }

    [HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawAtlas), typeof(Rect), typeof(Texture2D), typeof(bool))]
    internal static class Patch_DrawAtlas_Swap
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(ref Rect rect, ref Texture2D atlas)
        {
            // The gate lives here rather than inside AtlasSwap.For, so the theme
            // preview swatches, which go through Preview() rather than For(), keep drawing
            // while the skin is switched off.
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.enabled || !settings.skinButtons)
            {
                return;
            }

            Texture2D mine = AtlasSwap.For(atlas);
            if (mine == null)
            {
                return;
            }

            atlas = mine;

            // Only inset what we skinned. Atlases we do not replace keep vanilla
            // geometry, so nothing else in the game shifts.
            rect = LizarbInterfaceMod.Inset(rect);
        }
    }

}

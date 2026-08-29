using System.IO;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LudeonTK;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    internal static class AtlasSwap
    {
        private sealed class Slot
        {
            public Texture2D Vanilla;
            public byte[] Png;
            public string Name;
            public Texture2D Ours;
            public bool Tiling;
            public bool Shared;
            public string Theme;
            public long Stamp;
            public float Shade;
            public float Checked;
        }

        private static Slot[] slots = new Slot[0];

        private static readonly System.Collections.Generic.Dictionary<string, Slot> owned =
            new System.Collections.Generic.Dictionary<string, Slot>();

        private static string root;

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

        internal static Texture2D Own(string fileName, bool tiling = false)
        {
            return Load(fileName, tiling, shared: false);
        }

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

        private static void EnsureLoaded(Slot slot)
        {
            string theme = slot.Shared ? "Shared" : CurrentTheme;
            if (slot.Theme != theme || Edited(slot, theme))
            {
                slot.Theme = theme;
                slot.Png = ReadSkinFile(slot.Name, theme, out float shade);
                slot.Shade = shade;
                slot.Stamp = StampOf(slot.Name, theme);
                slot.Ours = null;
            }

            if (slot.Ours == null)
            {
                slot.Ours = Build(slot);
            }

            if (slot.Ours != null)
            {
                FilterMode want = DesiredFilter;
                if (slot.Ours.filterMode != want)
                {
                    slot.Ours.filterMode = want;
                }
            }
        }

        private static FilterMode DesiredFilter =>
            LizarbInterfaceMod.Settings?.pointFilter == true ? FilterMode.Point : FilterMode.Bilinear;

        private static string SkinPath(string fileName, string theme)
        {
            string dir = theme == null ? SkinDir : Path.Combine(root, "Skins/" + theme);
            return Path.Combine(dir, fileName + ".png");
        }

        private static readonly System.Collections.Generic.Dictionary<string, string[]> Instead =
            new System.Collections.Generic.Dictionary<string, string[]>
            {
                { "ButtonBGMouseover", new[] { "ButtonBG" } },
                { "ButtonBGClick",     new[] { "ButtonBG" } },
                { "ButtonSubtleAtlas", new[] { "ButtonBG" } },
                { "GizmoBG",           new[] { "ButtonBG" } },
                { "KeyBadge",          new[] { "ButtonBG" } },
                { "BarRound",          new[] { "ButtonBG" } },
                { "TabAtlas",          new[] { "ButtonBG" } },
                { "WindowAtlas",       new[] { "SectionAtlas", "ButtonBG" } },
                { "SectionAtlas",      new[] { "WindowAtlas", "ButtonBG" } },
                { "TooltipBG",         new[] { "SectionAtlas", "WindowAtlas", "ButtonBG" } },
                { "FloatMenuOptionBG", new[] { "SectionAtlas", "WindowAtlas", "ButtonBG" } },
                { "SliderRail",        new[] { "SectionAtlas", "WindowAtlas", "ButtonBG" } },
                { "BarTrough",         new[] { "SectionAtlas", "WindowAtlas", "ButtonBG" } },
            };

        private static readonly System.Collections.Generic.Dictionary<string, float> Derived =
            new System.Collections.Generic.Dictionary<string, float>
            {
                { "ButtonBGMouseover", 1.35f },
                { "ButtonBGClick",     0.72f },
                { "ButtonSubtleAtlas", 0.88f },
            };

        private static string Resolve(string fileName, string theme, out float shade)
        {
            shade = 1f;

            string own = SkinPath(fileName, theme);
            if (File.Exists(own))
            {
                return own;
            }

            if (Instead.TryGetValue(fileName, out string[] alternatives))
            {
                foreach (string alt in alternatives)
                {
                    string path = SkinPath(alt, theme);
                    if (File.Exists(path))
                    {
                        Derived.TryGetValue(fileName, out shade);
                        if (shade <= 0f)
                        {
                            shade = 1f;
                        }

                        return path;
                    }
                }
            }

            string standard = SkinPath(fileName, LizarbInterfaceSettings.DefaultTheme);
            return File.Exists(standard) ? standard : null;
        }

        private static long StampOf(string fileName, string theme)
        {
            try
            {
                string path = Resolve(fileName, theme, out float ignored);
                return path == null ? 0L : File.GetLastWriteTimeUtc(path).Ticks;
            }
            catch
            {
                return 0L;
            }
        }

        private static bool Edited(Slot slot, string theme)
        {
            if (!Prefs.DevMode)
            {
                return false;
            }

            float now = Time.realtimeSinceStartup;
            if (now - slot.Checked < 1f)
            {
                return false;
            }

            slot.Checked = now;
            return StampOf(slot.Name, theme) != slot.Stamp;
        }

        internal static void Forget()
        {
            foreach (Slot s in slots)
            {
                s.Theme = null;
                s.Ours = null;
            }

            foreach (Slot s in owned.Values)
            {
                s.Theme = null;
                s.Ours = null;
            }

            previews.Clear();
        }

        private static byte[] ReadSkinFile(string fileName, string theme, out float shade)
        {
            string path = Resolve(fileName, theme, out shade);
            if (path != null)
            {
                return File.ReadAllBytes(path);
            }

            Log.ErrorOnce("[LizarbInterface] no skin file for " + fileName + " in " + theme,
                          (fileName + theme).GetHashCode());
            return null;
        }

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
            ReadScale();

            RuntimeHelpers.RunClassConstructor(typeof(Widgets).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(TabRecord).TypeHandle);

            slots = new[]
            {
                Make(typeof(Widgets), "ButtonBGAtlas", "ButtonBG"),
                Make(typeof(Widgets), "ButtonBGAtlasMouseover", "ButtonBGMouseover"),
                Make(typeof(Widgets), "ButtonBGAtlasClick", "ButtonBGClick"),
                Make(typeof(Widgets), "ButtonSubtleAtlas", "ButtonSubtleAtlas"),

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

            slot.Theme = null;

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

        internal static Texture2D For(Texture2D original)
        {
            if (original == null)
            {
                return null;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                Slot slot = slots[i];

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

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            tex.LoadImage(slot.Png);
            Shift(tex, slot.Shade);
            tex.name = slot.Name;
            tex.filterMode = DesiredFilter;
            tex.anisoLevel = 0;
            tex.wrapMode = slot.Tiling ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        private static void Shift(Texture2D tex, float shade)
        {
            if (shade <= 0f || Mathf.Approximately(shade, 1f))
            {
                return;
            }

            Color32[] pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                p.r = (byte)Mathf.Clamp(p.r * shade, 0f, 255f);
                p.g = (byte)Mathf.Clamp(p.g * shade, 0f, 255f);
                p.b = (byte)Mathf.Clamp(p.b * shade, 0f, 255f);
                pixels[i] = p;
            }

            tex.SetPixels32(pixels);
        }

        internal static float Scale { get; private set; } = 1f;

        internal static bool Bypass;

        private static void ReadScale()
        {
            Scale = 1f;
            if (root == null)
            {
                return;
            }

            string path = Path.Combine(root, "Skins/atlas-scale.txt");
            if (!File.Exists(path))
            {
                return;
            }

            if (float.TryParse(File.ReadAllText(path).Trim(),
                               System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture,
                               out float parsed) && parsed >= 1f && parsed <= 8f)
            {
                Scale = parsed;
                return;
            }

            Log.Warning("[LizarbInterface] unreadable atlas scale in " + path + "; assuming 1.");
        }

        internal static void DrawScaled(Rect rect, Texture2D atlas, bool drawTop)
        {
            if (atlas == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            rect.x = Mathf.Round(rect.x);
            rect.y = Mathf.Round(rect.y);
            rect.width = Mathf.Round(rect.width);
            rect.height = Mathf.Round(rect.height);
            rect = UIScaling.AdjustRectToUIScaling(rect);

            float a = atlas.width * 0.25f / Scale;
            a = UIScaling.AdjustCoordToUIScalingFloor(GenMath.Min(a, rect.height / 2f, rect.width / 2f));

            if (drawTop)
            {
                Part(new Rect(rect.x, rect.y, a, a), UvTopLeft, atlas);
                Part(new Rect(rect.x + rect.width - a, rect.y, a, a), UvTopRight, atlas);
            }

            Part(new Rect(rect.x, rect.y + rect.height - a, a, a), UvBottomLeft, atlas);
            Part(new Rect(rect.x + rect.width - a, rect.y + rect.height - a, a, a), UvBottomRight, atlas);

            Rect middle = new Rect(rect.x + a, rect.y + a, rect.width - a * 2f, rect.height - a * 2f);
            if (!drawTop)
            {
                middle.height += a;
                middle.y -= a;
            }

            Part(middle, UvCenter, atlas);

            if (drawTop)
            {
                Part(new Rect(rect.x + a, rect.y, rect.width - a * 2f, a), UvTop, atlas);
            }

            Part(new Rect(rect.x + a, rect.y + rect.height - a, rect.width - a * 2f, a), UvBottom, atlas);

            Rect left = new Rect(rect.x, rect.y + a, a, rect.height - a * 2f);
            Rect right = new Rect(rect.x + rect.width - a, rect.y + a, a, rect.height - a * 2f);
            if (!drawTop)
            {
                left.height += a;  left.y -= a;
                right.height += a; right.y -= a;
            }

            Part(left, UvLeft, atlas);
            Part(right, UvRight, atlas);
        }

        internal static void DrawFaceGrain(Rect rect, float corner)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.grainOnButtons ||
                !settings.texturedBackground || settings.backgroundGrain <= 0.001f)
            {
                return;
            }

            Rect face = rect.ContractedBy(corner * 0.5f);
            if (face.width < 6f || face.height < 6f)
            {
                return;
            }

            Texture2D grain = Own("Pattern_" + settings.backgroundPattern, tiling: true);
            if (grain == null)
            {
                return;
            }

            float tileW = grain.width / Scale;
            float tileH = grain.height / Scale;

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, settings.backgroundGrain);
            GUI.DrawTextureWithTexCoords(
                face, grain,
                new Rect(0f, 0f, face.width / tileW, face.height / tileH));
            GUI.color = previous;
        }

        private static void Part(Rect drawRect, Rect uv, Texture2D atlas)
        {
            if (drawRect.width <= 0f || drawRect.height <= 0f)
            {
                return;
            }

            Widgets.DrawTexturePart(drawRect, uv, atlas);
        }

        private static readonly Rect UvTopLeft     = new Rect(0f,    0f,    0.25f, 0.25f);
        private static readonly Rect UvTopRight    = new Rect(0.75f, 0f,    0.25f, 0.25f);
        private static readonly Rect UvBottomLeft  = new Rect(0f,    0.75f, 0.25f, 0.25f);
        private static readonly Rect UvBottomRight = new Rect(0.75f, 0.75f, 0.25f, 0.25f);
        private static readonly Rect UvTop         = new Rect(0.25f, 0f,    0.5f,  0.25f);
        private static readonly Rect UvBottom      = new Rect(0.25f, 0.75f, 0.5f,  0.25f);
        private static readonly Rect UvLeft        = new Rect(0f,    0.25f, 0.25f, 0.5f);
        private static readonly Rect UvRight       = new Rect(0.75f, 0.25f, 0.25f, 0.5f);
        private static readonly Rect UvCenter      = new Rect(0.25f, 0.25f, 0.5f,  0.5f);

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
        private static bool Prefix(Rect rect, Texture2D atlas, bool drawTop)
        {
            if (AtlasSwap.Bypass)
            {
                return true;
            }

            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.enabled || !settings.skinButtons)
            {
                return true;
            }

            Texture2D mine = AtlasSwap.For(atlas);
            if (mine == null)
            {
                return true;
            }

            Rect painted = LizarbInterfaceMod.Inset(rect);
            AtlasSwap.DrawScaled(painted, mine, drawTop);
            AtlasSwap.DrawFaceGrain(painted, mine.width * 0.25f / AtlasSwap.Scale);
            return false;
        }
    }
}

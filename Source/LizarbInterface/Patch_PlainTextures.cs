using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Swaps the widgets drawn as a single stretched image, which the DrawAtlas hook
    /// misses: checkboxes, radios, the slider knob, gizmo plates, bar fills.
    ///
    /// Matched by REFERENCE against the vanilla instance, never by texture.name -
    /// Object.name is a native call that allocates, and this runs hundreds of times
    /// a frame.
    /// </summary>
    internal static class PlainTextures
    {
        private sealed class Entry
        {
            public Texture2D Vanilla;
            public string File;
        }

        private static Entry[] entries = new Entry[0];

        internal static void Init()
        {
            RuntimeHelpersRun(typeof(Widgets));
            RuntimeHelpersRun(typeof(Command));

            entries = new[]
            {
                Make(typeof(Widgets), "CheckboxOnTex", "CheckOn"),
                Make(typeof(Widgets), "CheckboxOffTex", "CheckOff"),
                Make(typeof(Widgets), "CheckboxPartialTex", "CheckPartial"),
                Make(typeof(Widgets), "RadioButOnTex", "RadioButOn"),
                Make(typeof(Widgets), "RadioButOffTex", "RadioButOff"),
                Make(typeof(Widgets), "SliderHandle", "SliderHandle"),
                // ColonistBar.BGTex is the SAME instance as Command.BGTex, so the
                // colonist bar comes along with the gizmo plate for free.
                Make(typeof(Command), "BGTex", "GizmoBG"),

                // Default progress bar fill. Bars that pass their own texture (mood,
                // health, research) are left alone: that colour is information.
                Make(typeof(Widgets), "BarFullTexHor", "BarFill"),
            };
        }

        private static void RuntimeHelpersRun(Type t)
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);
        }

        private static Entry Make(Type owner, string field, string file)
        {
            Texture2D vanilla = null;
            try
            {
                vanilla = AccessTools.StaticFieldRefAccess<Texture2D>(owner, field);
            }
            catch (Exception e)
            {
                // A renamed field in a future game build must not take the mod down.
                Log.Warning("[LizarbInterface] could not read " + owner.Name + "." + field + ": " + e.Message);
            }

            return new Entry { Vanilla = vanilla, File = file };
        }

        /// <summary>Replacement for this texture, or null to leave it alone.</summary>
        internal static Texture2D For(Texture original)
        {
            if (original == null || entries.Length == 0)
            {
                return null;
            }

            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.enabled || !settings.skinWidgets)
            {
                return null;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                if (ReferenceEquals(entries[i].Vanilla, original))
                {
                    return AtlasSwap.Own(entries[i].File);
                }
            }

            return null;
        }
    }

    [HarmonyPatch(typeof(GUI), nameof(GUI.DrawTexture), typeof(Rect), typeof(Texture))]
    internal static class Patch_GUIDrawTexture
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(ref Texture image)
        {
            Texture2D mine = PlainTextures.For(image);
            if (mine != null)
            {
                image = mine;
            }
        }
    }

    /// <summary>Gizmos route through GenUI instead. Same swap, different door.</summary>
    [HarmonyPatch(typeof(GenUI), nameof(GenUI.DrawTextureWithMaterial))]
    internal static class Patch_DrawTextureWithMaterial
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(ref Texture texture)
        {
            Texture2D mine = PlainTextures.For(texture);
            if (mine != null)
            {
                texture = mine;
            }
        }
    }
}

using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
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
                Make(typeof(Command), "BGTex", "GizmoBG"),

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
                Log.Warning("[LizarbInterface] could not read " + owner.Name + "." + field + ": " + e.Message);
            }

            return new Entry { Vanilla = vanilla, File = file };
        }

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

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
            public bool Shared;
        }

        private static Entry[] entries = new Entry[0];

        internal static void Init()
        {
            RuntimeHelpersRun(typeof(Widgets));
            RuntimeHelpersRun(typeof(Command));
            RuntimeHelpersRun(typeof(TexButton));

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

                Icon("ShowZones"),
                Icon("ShowBeauty"),
                Icon("ShowRoomStats"),
                Icon("CategorizedResourceReadout"),
                Icon("ShowColonistBar"),
                Icon("ShowRoofOverlay"),
                Icon("ShowTemperatureOverlay"),
                Icon("ShowFertilityOverlay"),
                Icon("ShowTerrainAffordanceOverlay"),
                Icon("ShowPollutionOverlay"),
                Icon("ShowLearningHelper"),
                Icon("AutoHomeArea"),
                Icon("AutoRebuild"),
                Icon("LockNorthUp"),
                Icon("ShowWorldFeatures"),
                Icon("UsePlanetDayNightSystem"),
                Icon("ShowImportantLocations"),
                Icon("ShowLandmarkIcons"),
                Icon("ShowOtherFactionBases"),
                Icon("CodexButton"),
                Icon("SearchButton"),
                Icon("ShowVacuumOverlay"),
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

        private static Entry Icon(string field)
        {
            Entry e = Make(typeof(TexButton), field, "Icon" + field);
            e.Shared = true;
            return e;
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
                    return entries[i].Shared ? AtlasSwap.Shared(entries[i].File) : AtlasSwap.Own(entries[i].File);
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

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
            public bool Plate;
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

                Icon("CloseXSmall", "IconCloseX"),
                Element("SpeedButtonTextures", 0, "IconSpeedPause", plate: true),
                Element("SpeedButtonTextures", 1, "IconSpeedNormal", plate: true),
                Element("SpeedButtonTextures", 2, "IconSpeedFast", plate: true),
                Element("SpeedButtonTextures", 3, "IconSpeedSuper", plate: true),
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

        private static Entry Icon(string field, string file, bool plate = false)
        {
            Entry e = Make(typeof(TexButton), field, file);
            e.Shared = true;
            e.Plate = plate;
            return e;
        }

        private static Entry Element(string field, int index, string file, bool plate = false)
        {
            Texture2D vanilla = null;
            try
            {
                Texture2D[] all = AccessTools.StaticFieldRefAccess<Texture2D[]>(typeof(TexButton), field);
                if (all != null && index < all.Length)
                {
                    vanilla = all[index];
                }
            }
            catch (Exception e)
            {
                Log.Warning("[LizarbInterface] could not read TexButton." + field + ": " + e.Message);
            }

            return new Entry { Vanilla = vanilla, File = file, Shared = true, Plate = plate };
        }

        internal static bool WantsPlate(Texture original)
        {
            if (original == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                if (ReferenceEquals(entries[i].Vanilla, original))
                {
                    return entries[i].Plate;
                }
            }

            return false;
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

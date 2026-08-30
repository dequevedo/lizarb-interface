using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    [StaticConstructorOnStartup]
    internal static class BetterArchitectCompat
    {
        private const string TargetType =
            "BetterArchitect.ArchitectCategoryTab_DesignationTabOnGUI_Patch";

        static BetterArchitectCompat()
        {
            try
            {
                Harmony harmony = LizarbInterfaceMod.Harmony;
                if (harmony == null)
                {
                    return;
                }

                Type type = AccessTools.TypeByName(TargetType);
                if (type == null)
                {
                    return;
                }

                MethodInfo selected = AccessTools.Method(type, "DrawOptionSelected");
                if (selected != null)
                {
                    harmony.Patch(selected, prefix: new HarmonyMethod(
                        typeof(BetterArchitectCompat), nameof(SelectedPrefix)));
                }

                MethodInfo unselected = AccessTools.Method(type, "DrawOptionUnselected");
                if (unselected != null)
                {
                    harmony.Patch(unselected, prefix: new HarmonyMethod(
                        typeof(BetterArchitectCompat), nameof(UnselectedPrefix)));
                }
            }
            catch (Exception e)
            {
                Log.Warning("[LizarbInterface] could not skin Better Architect Menu: " + e.Message);
            }
        }

        private static bool Paint(Rect rect, string file, float shade)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.enabled || !settings.skinButtons)
            {
                return true;
            }

            Texture2D plate = AtlasSwap.Own(file);
            if (plate == null)
            {
                return true;
            }

            Color previous = GUI.color;
            GUI.color = new Color(shade, shade, shade, previous.a);
            AtlasSwap.DrawScaled(rect, plate, true, null, tiled: true);
            GUI.color = previous;
            return false;
        }

        private static bool SelectedPrefix(Rect rect)
        {
            return Paint(rect, "ButtonBGMouseover", 1f);
        }

        private static bool UnselectedPrefix(Rect rect, bool lowlight)
        {
            return Paint(rect, "ButtonBG", lowlight ? 0.65f : 1f);
        }
    }
}

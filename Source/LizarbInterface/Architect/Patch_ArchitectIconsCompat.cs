using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    [StaticConstructorOnStartup]
    internal static class ArchitectIconsCompat
    {
        private const string TargetType = "ArchitectIcons.ArchitectIconsMod";
        private const string TargetMethod = "DoArchitectButton";

        static ArchitectIconsCompat()
        {
            try
            {
                Harmony harmony = LizarbInterfaceMod.Harmony;
                if (harmony == null)
                {
                    return;
                }

                Type type = AccessTools.TypeByName(TargetType);
                MethodInfo target = type == null ? null : AccessTools.Method(type, TargetMethod);
                if (target == null)
                {
                    return;
                }

                harmony.Patch(target, prefix: new HarmonyMethod(
                    typeof(ArchitectIconsCompat), nameof(Prefix)));
            }
            catch (Exception e)
            {
                Log.Warning("[LizarbInterface] could not defer to Architect Icons: " + e.Message);
            }
        }

        private static bool Prefix(
            Rect rect, string label, float barPercent, float textLeftMargin,
            SoundDef mouseoverSound, Vector2 functionalSizeOffset, Color? labelColor,
            bool highlight, ref bool __result)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.enabled || !settings.architectIcons)
            {
                return true;
            }

            __result = Widgets.ButtonTextSubtle(
                rect, label, barPercent, textLeftMargin, mouseoverSound,
                functionalSizeOffset, labelColor, highlight);

            return false;
        }
    }
}

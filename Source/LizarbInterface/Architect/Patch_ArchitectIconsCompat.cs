using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Stops Architect Icons drawing its icon while this mod draws its own, so the
    /// two do not stack in the same corner of the button.
    ///
    /// Architect Icons transpiles DoCategoryButton to route ButtonTextSubtle through
    /// its own DoArchitectButton, which calls the real thing and then blits an icon.
    /// Prefixing that wrapper reproduces the call and skips only the blit. The
    /// button, the label and the return value stay exactly as that mod made them.
    ///
    /// Patched by hand rather than by attribute: the target type does not exist when
    /// Architect Icons is absent, and an attribute patch on a missing type throws.
    ///
    /// Runs at StaticConstructorOnStartup, not from the Mod constructor: that other
    /// assembly is only guaranteed to be loaded once every mod has been.
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class ArchitectIconsCompat
    {
        private const string TargetType = "ArchitectIcons.ArchitectIconsMod";
        private const string TargetMethod = "DoArchitectButton";

        static ArchitectIconsCompat()
        {
            try
            {
                var harmony = new Harmony("lizarb.interface");
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
                // Losing the compat patch costs a doubled icon, never a broken menu.
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

            // textLeftMargin is passed through untouched: that mod adds 16f for its own
            // icon, and ours reserves its own room in Patch_ButtonTextSubtle.
            __result = Widgets.ButtonTextSubtle(
                rect, label, barPercent, textLeftMargin, mouseoverSound,
                functionalSizeOffset, labelColor, highlight);

            return false;
        }
    }
}

using HarmonyLib;
using RimWorld;
using UnityEngine;

namespace LizarbInterface
{
    internal static class ArchitectPadding
    {
        internal static float Amount
        {
            get
            {
                LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
                if (settings == null || !settings.enabled || !settings.architectSpacing)
                {
                    return 0f;
                }

                return Mathf.Max(0f, settings.architectPadding);
            }
        }

        internal static float InfoOffset => Amount * 3f;

        internal static float ExtraOffset => InfoOffset + Amount;
    }

    [HarmonyPatch(typeof(Verse.DesignatorUtility), nameof(Verse.DesignatorUtility.GUIDoRotationControls))]
    internal static class Patch_ArchitectRotationControls
    {
        private static void Prefix(ref float bottomY)
        {
            if (ArchitectLayout.DrawingTab)
            {
                bottomY -= ArchitectPadding.ExtraOffset;
            }
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Architect), nameof(MainTabWindow_Architect.DoWindowContents))]
    internal static class Patch_ArchitectPadding
    {
        private static void Prefix(ref Rect inRect, out bool __state)
        {
            float pad = ArchitectPadding.Amount;
            __state = pad > 0f;

            if (!__state)
            {
                return;
            }

            GUI.BeginGroup(new Rect(inRect.x + pad, inRect.y + pad,
                                    inRect.width - pad * 2f, inRect.height - pad * 2f));

            inRect = new Rect(0f, 0f, inRect.width - pad * 2f, inRect.height - pad * 2f);
        }

        private static void Finalizer(bool __state)
        {
            if (__state)
            {
                GUI.EndGroup();
            }
        }
    }
}

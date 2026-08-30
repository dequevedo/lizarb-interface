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
                if (settings == null || !settings.enabled)
                {
                    return 0f;
                }

                return Mathf.Max(0f, settings.architectPadding);
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

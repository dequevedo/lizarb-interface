using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    internal static class ArchitectLayout
    {
        internal const float GizmoMargin = 10f;

        internal static bool DrawingTab;

        internal static float MenuWidth
        {
            get
            {
                var window = MainButtonDefOf.Architect?.TabWindow as MainTabWindow_Architect;
                return window == null ? 200f : window.RequestedTabSize.x;
            }
        }
    }

    [HarmonyPatch(typeof(ArchitectCategoryTab), nameof(ArchitectCategoryTab.DesignationTabOnGUI))]
    internal static class Patch_DesignationTab
    {
        private static void Prefix()
        {
            ArchitectLayout.DrawingTab = true;
        }

        private static void Postfix()
        {
            ArchitectLayout.DrawingTab = false;
        }
    }

    [HarmonyPatch(typeof(GizmoGridDrawer), nameof(GizmoGridDrawer.DrawGizmoGrid))]
    internal static class Patch_GizmoGridStart
    {
        private static void Prefix(ref float startX)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.enabled || !settings.architectAutoWidth)
            {
                return;
            }

            if (ArchitectLayout.DrawingTab)
            {
                startX = ArchitectLayout.MenuWidth + ArchitectLayout.GizmoMargin;
            }
        }
    }

    [HarmonyPatch(typeof(ArchitectCategoryTab), nameof(ArchitectCategoryTab.InfoRect), MethodType.Getter)]
    internal static class Patch_ArchitectInfoRect
    {
        private static void Postfix(ref Rect __result)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.enabled)
            {
                return;
            }

            if (settings.architectAutoWidth)
            {
                __result.width = Mathf.Max(__result.width, ArchitectLayout.MenuWidth);
            }

            __result.y -= ArchitectPadding.InfoOffset;
        }
    }
}

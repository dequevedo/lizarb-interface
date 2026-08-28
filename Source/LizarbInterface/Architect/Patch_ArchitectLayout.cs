using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Moves the designator grid and the info box to match a widened Architect menu.
    ///
    /// Vanilla hardcodes both against a 200px menu: DesignationTabOnGUI starts the
    /// gizmo grid at 210, and InfoRect is 200 wide. Widening the tab without these
    /// leaves the first designators, Cancel among them, hidden underneath it.
    ///
    /// Both derive from the menu's ACTUAL width rather than adding a constant of
    /// their own. Architect Icons transpiles the same two numbers by +32; reading the
    /// final width means we land in the right place whoever else has widened it, and
    /// two mods adding constants can never double up.
    /// </summary>
    internal static class ArchitectLayout
    {
        /// <summary>Vanilla's 210 against a 200 menu.</summary>
        internal const float GizmoMargin = 10f;

        /// <summary>Set only while the architect tab is drawing its own grid.</summary>
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

    /// <summary>
    /// DrawGizmoGrid also draws the bar for the current selection, which must not
    /// move, so the flag above is what tells the two apart.
    /// </summary>
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
            if (settings == null || !settings.enabled || !settings.architectAutoWidth)
            {
                return;
            }

            __result.width = Mathf.Max(__result.width, ArchitectLayout.MenuWidth);
        }
    }
}

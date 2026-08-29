using HarmonyLib;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    internal static class GizmoKeyContext
    {
        internal static Rect? Current;
    }

    [HarmonyPatch(typeof(Command), "GizmoOnGUIInt")]
    internal static class Patch_GizmoContext
    {
        private static void Prefix(Rect butRect, out Rect? __state)
        {
            __state = GizmoKeyContext.Current;
            GizmoKeyContext.Current = butRect;
        }

        private static void Postfix(Rect? __state)
        {
            GizmoKeyContext.Current = __state;
        }
    }

    [HarmonyPatch(typeof(Widgets), nameof(Widgets.Label), typeof(Rect), typeof(string))]
    [HarmonyPriority(Priority.High)]
    internal static class Patch_GizmoKeyBadge
    {
        private const float PadX = 3f;
        private const float PadY = 1f;

        private static void Prefix(Rect rect, string label)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.enabled || !settings.skinWidgets)
            {
                return;
            }

            if (Event.current.type != EventType.Repaint || label.NullOrEmpty() || label.Length > 3)
            {
                return;
            }

            Rect? gizmo = GizmoKeyContext.Current;
            if (!gizmo.HasValue || gizmo.Value != rect)
            {
                return;
            }

            Texture2D plate = AtlasSwap.Own("KeyBadge");
            if (plate == null)
            {
                return;
            }

            Vector2 size = Text.CalcSize(label);
            Rect badge = new Rect(rect.x, rect.y, size.x + PadX * 2f, size.y + PadY * 2f);

            AtlasSwap.DrawScaled(badge, plate, true);
        }
    }

    [HarmonyPatch(typeof(Widgets), nameof(Widgets.CloseButtonFor))]
    internal static class Patch_CloseButtonChrome
    {
        private const float Pad = 3f;

        private static void Prefix(Rect rectToClose)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.enabled || !settings.skinWindows)
            {
                return;
            }

            Texture2D plate = AtlasSwap.Own("ButtonBG");
            if (plate == null)
            {
                return;
            }

            Rect icon = new Rect(rectToClose.x + rectToClose.width - 22f, rectToClose.y + 4f, 18f, 18f);
            AtlasSwap.DrawScaled(icon.ExpandedBy(Pad), plate, true);
        }
    }
}

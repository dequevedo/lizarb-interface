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
        private static void Prefix(Rect butRect, GizmoRenderParms parms, out Rect? __state)
        {
            __state = GizmoKeyContext.Current;

            float dx = parms.shrunk ? 3f : 5f;
            float dy = parms.shrunk ? 0f : 3f;
            GizmoKeyContext.Current = new Rect(butRect.x + dx, butRect.y + dy, butRect.width - 10f, 0f);
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

        private static void Prefix(ref Rect rect, string label)
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
            if (!gizmo.HasValue)
            {
                return;
            }

            Rect where = gizmo.Value;
            if (!Mathf.Approximately(where.x, rect.x) || !Mathf.Approximately(where.y, rect.y) ||
                !Mathf.Approximately(where.width, rect.width))
            {
                return;
            }

            Texture2D plate = AtlasSwap.Own("KeyBadge");
            if (plate == null)
            {
                return;
            }

            Vector2 size = Text.CalcSize(label);
            float side = Mathf.Max(size.y, size.x + PadX * 2f);

            AtlasSwap.DrawScaled(new Rect(rect.x, rect.y, side, size.y), plate, true);
            rect.x += (side - size.x) * 0.5f;
        }
    }

    [HarmonyPatch(typeof(Widgets), nameof(Widgets.ButtonImage),
        typeof(Rect), typeof(Texture2D), typeof(Color), typeof(Color), typeof(bool), typeof(string))]
    internal static class Patch_ButtonImagePlate
    {
        private const float Smallest = 10f;

        private static void Prefix(Rect butRect)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.enabled || !settings.skinWidgets || !settings.plateIconButtons)
            {
                return;
            }

            if (butRect.width < Smallest || butRect.height < Smallest)
            {
                return;
            }

            Texture2D plate = AtlasSwap.Own("ButtonBG");
            if (plate != null)
            {
                AtlasSwap.DrawScaled(butRect, plate, true);
            }
        }
    }
}

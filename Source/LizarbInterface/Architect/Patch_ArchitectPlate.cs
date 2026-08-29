using UnityEngine;
using Verse;

namespace LizarbInterface
{
    internal static class ArchitectPlate
    {
        internal static readonly string[] Styles =
        {
            "Plate", "Frame", "Flat",
            "Square", "Circle", "Diamond", "Tag", "Shield", "Hex", "Gradient",
        };

        private static bool IsBadge(string style)
        {
            switch (style)
            {
                case "Square":
                case "Circle":
                case "Diamond":
                case "Tag":
                case "Shield":
                case "Hex":
                    return true;
                default:
                    return false;
            }
        }

        internal static void Draw(Rect plate, string style, Color tint)
        {
            if (Event.current.type != EventType.Repaint || plate.width <= 0f || plate.height <= 0f)
            {
                return;
            }

            switch (style)
            {
                case "Flat":
                    Widgets.DrawBoxSolid(plate, tint);
                    return;

                case "Gradient":
                    Gradient(plate, tint);
                    return;
            }

            if (IsBadge(style))
            {
                float side = Mathf.Min(plate.height, plate.width);
                Badge(new Rect(plate.x, plate.y, side, side), style, tint);
                return;
            }

            Texture2D shape = AtlasSwap.Own(style == "Frame" ? "PlateFrame" : "Plate");
            if (shape == null)
            {
                Widgets.DrawBoxSolid(plate, tint);
                return;
            }

            Color previous = GUI.color;
            GUI.color = tint;
            AtlasSwap.DrawScaled(plate, shape, true);
            GUI.color = previous;
        }

        private static void Gradient(Rect plate, Color tint)
        {
            float side = Mathf.Min(plate.height, plate.width);

            Texture2D head = AtlasSwap.Shared("ShapeFadeHead");
            Texture2D tail = AtlasSwap.Shared("ShapeFade");
            if (head == null || tail == null)
            {
                Widgets.DrawBoxSolid(new Rect(plate.x, plate.y, side, side), tint);
                return;
            }

            Color previous = GUI.color;
            GUI.color = tint;

            GUI.DrawTexture(new Rect(plate.x, plate.y, side, side), head);

            float rest = plate.xMax - (plate.x + side);
            if (rest > 0f)
            {
                GUI.DrawTexture(new Rect(plate.x + side, plate.y, rest, side), tail);
            }

            GUI.color = previous;
        }

        private static void Badge(Rect rect, string shapeName, Color tint)
        {
            Texture2D tex = AtlasSwap.Shared("Shape" + shapeName);
            if (tex == null)
            {
                Widgets.DrawBoxSolid(rect, tint);
                return;
            }

            Color previous = GUI.color;

            if (LizarbInterfaceMod.Settings.architectShapeOutline)
            {
                float px = GUI.matrix.m00 > 0.01f ? 1f / GUI.matrix.m00 : 1f;
                GUI.color = new Color(0f, 0f, 0f, tint.a);
                GUI.DrawTexture(rect.ExpandedBy(px), tex);
            }

            GUI.color = tint;
            GUI.DrawTexture(rect, tex);
            GUI.color = previous;
        }
    }
}

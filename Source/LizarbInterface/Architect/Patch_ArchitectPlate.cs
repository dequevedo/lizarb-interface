using UnityEngine;
using Verse;

namespace LizarbInterface
{
    internal static class ArchitectPlate
    {
        internal static readonly string[] Styles =
        {
            "Plate", "Frame", "Flat",
            "Square", "Circle", "Diamond", "Tag", "Shield", "Hex", "Cascade",
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

                case "Cascade":
                    Cascade(plate, tint);
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

        private static void Cascade(Rect plate, Color tint)
        {
            float side = Mathf.Min(plate.height, plate.width);
            Badge(new Rect(plate.x, plate.y, side, side), "Square", tint);

            float barWidth = side * 0.30f;
            float gap = side * 0.14f;
            float x = plate.xMax - barWidth;

            foreach (float fraction in new[] { 0.1f, 0.2f, 0.4f })
            {
                float h = side * fraction;
                if (x < plate.x + side + gap)
                {
                    return;
                }

                Badge(new Rect(x, plate.y + (side - h) / 2f, barWidth, h), "Square", tint);
                x -= barWidth + gap;
            }
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

using UnityEngine;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Every way the category colour can be shaped, and the one place that draws
    /// them. The settings preview calls straight into this rather than mirroring it,
    /// because a preview that drifts from the real draw is worse than none.
    ///
    /// Two families. Plate and Frame stretch across the button and are 9-slice, per
    /// theme, so they pick up its corner radius. The badges are fixed-aspect masks
    /// from Skins/Shared, drawn into a square of their own: a circle or a diamond
    /// squeezed into a 9-slice edge band would be smeared along its run.
    /// </summary>
    internal static class ArchitectPlate
    {
        internal static readonly string[] Styles =
        {
            "Plate", "Frame", "Flat",
            "Square", "Circle", "Diamond", "Tag", "Shield", "Hex", "Chip",
            "Cascade", "Underline",
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
                case "Chip":
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

                case "Underline":
                    // A rule along the bottom, the full width of the button.
                    float thickness = Mathf.Max(3f, plate.height * 0.18f);
                    Badge(new Rect(plate.x, plate.yMax - thickness, plate.width, thickness),
                          "Square", tint);
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

        /// <summary>
        /// Square, then bars stepping down to its right. Programmatic rather than a
        /// texture because the aspect depends on the button, and a wide mask squeezed
        /// into a narrow button would change the spacing rather than clip it.
        /// </summary>
        private static void Cascade(Rect plate, Color tint)
        {
            float side = Mathf.Min(plate.height, plate.width);
            Badge(new Rect(plate.x, plate.y, side, side), "Square", tint);

            float x = plate.x + side + side * 0.16f;
            float barWidth = side * 0.30f;

            foreach (float fraction in new[] { 0.4f, 0.2f, 0.1f })
            {
                float h = side * fraction;
                if (x + barWidth > plate.xMax)
                {
                    return;
                }

                Badge(new Rect(x, plate.y + (side - h) / 2f, barWidth, h), "Square", tint);
                x += barWidth + side * 0.14f;
            }
        }

        /// <summary>
        /// The outline is painted as the same mask, black, one pixel larger behind.
        /// That works for any convex shape, keeps the toggle honest, and costs one
        /// draw call instead of a second set of files with the ring baked in.
        /// </summary>
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

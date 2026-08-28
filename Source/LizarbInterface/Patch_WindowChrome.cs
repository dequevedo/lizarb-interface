using HarmonyLib;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Draws window and panel chrome as a 9-sliced frame. Vanilla has no texture here
    /// at all. It is a tinted white pixel plus a 1px box, so the drawing is replaced
    /// rather than skinned. Every prefix falls through to vanilla if the frame texture
    /// is missing, so the cost of a bad PNG is the look, never the window.
    /// </summary>
    internal static class Patch_WindowChrome
    {
        private static bool Enabled => LizarbInterfaceMod.Settings.enabled &&
                                     LizarbInterfaceMod.Settings.skinWindows &&
                                     AtlasSwap.Ready;

        /// <summary>
        /// Tiles the background pattern over the interior. It cannot live in the atlas:
        /// the 9-slice centre stretches on both axes and would smear it into streaks.
        /// Drawn with UVs scaled to the rect, so it repeats at its native size instead.
        /// </summary>
        private static void DrawGrain(Rect rect, float inset, float radius)
        {
            var settings = LizarbInterfaceMod.Settings;
            if (!settings.texturedBackground || settings.backgroundGrain <= 0.001f)
            {
                return;
            }

            if (rect.width < inset * 2f + 8f || rect.height < inset * 2f + 8f)
            {
                return;
            }

            // Baked per theme, so the pattern already carries its own colour.
            Texture2D grain = AtlasSwap.Own("Pattern_" + settings.backgroundPattern, tiling: true);
            if (grain == null)
            {
                return;
            }

            Rect interior = rect.ContractedBy(inset);

            // IMGUI has no rounded clip, so the fill goes down as horizontal bands that
            // follow the arc. Three bands would leave the four corners bare.
            float r = Mathf.Min(radius, interior.width / 2f, interior.height / 2f);

            Color previous = GUI.color;
            // White: the pattern carries its own colour. Tinting it dark would undo
            // both the theme inks and the anti-aliasing baked into the PNG.
            GUI.color = new Color(1f, 1f, 1f, settings.backgroundGrain);

            Band(interior, grain, new Rect(interior.x, interior.y + r, interior.width, interior.height - r * 2f));
            Arc(interior, grain, r, top: true);
            Arc(interior, grain, r, top: false);

            GUI.color = previous;
        }

        /// <summary>Steps per rounded end. Six is past the point where more shows.</summary>
        private const int ArcSteps = 6;

        private static void Arc(Rect interior, Texture2D grain, float r, bool top)
        {
            if (r <= 0f)
            {
                return;
            }

            float step = r / ArcSteps;
            for (int i = 0; i < ArcSteps; i++)
            {
                // Inset measured at the step's OUTERMOST row, which is the widest, so no band
                // ever pokes past the curve.
                float depth = i * step;
                float dy = r - depth;
                float inset = r - Mathf.Sqrt(Mathf.Max(0f, r * r - dy * dy));

                float width = interior.width - inset * 2f;
                if (width <= 0f)
                {
                    continue;
                }

                float y = top ? interior.y + depth : interior.yMax - depth - step;
                Band(interior, grain, new Rect(interior.x + inset, y, width, step));
            }
        }

        /// <summary>UVs anchored to the whole interior, so the bands meet without a seam.</summary>
        private static void Band(Rect interior, Texture2D grain, Rect band)
        {
            if (band.width <= 0f || band.height <= 0f)
            {
                return;
            }

            // IMGUI UVs run bottom-up while rects run top-down.
            //
            // NOT divided by AtlasSwap.Scale. The patterns are still authored at 1x:
            // their feature periods are hardcoded and have to divide the tile size for
            // the tiling to close, so doubling them is a rewrite rather than a
            // multiply. Background at 5% opacity is the least resolution-sensitive
            // surface here, so it waits.
            float tileW = grain.width;
            float tileH = grain.height;

            var uv = new Rect(
                (band.x - interior.x) / tileW,
                (interior.yMax - band.yMax) / tileH,
                band.width / tileW,
                band.height / tileH);

            GUI.DrawTextureWithTexCoords(band, grain, uv);
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawWindowBackground), typeof(Rect))]
        internal static class DrawWindowBackground
        {
            private static bool Prefix(Rect rect)
            {
                if (!Enabled)
                {
                    return true;
                }

                Texture2D frame = AtlasSwap.Own("WindowAtlas");
                if (frame == null)
                {
                    return true;
                }

                GUI.color = Color.white;
                Rect area = LizarbInterfaceMod.Inset(rect);
                AtlasSwap.DrawScaled(area, frame, true);
                DrawGrain(area, 5f, 22f);
                return false;
            }
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawWindowBackground), typeof(Rect), typeof(Color))]
        internal static class DrawWindowBackgroundTinted
        {
            private static bool Prefix(Rect rect, Color colorFactor)
            {
                if (!Enabled)
                {
                    return true;
                }

                Texture2D frame = AtlasSwap.Own("WindowAtlas");
                if (frame == null)
                {
                    return true;
                }

                // This overload restores the caller's colour; callers rely on that.
                Color previous = GUI.color;
                GUI.color = colorFactor;
                Rect area = LizarbInterfaceMod.Inset(rect);
                AtlasSwap.DrawScaled(area, frame, true);
                DrawGrain(area, 5f, 22f);
                GUI.color = previous;
                return false;
            }
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawMenuSection))]
        internal static class DrawMenuSection
        {
            private static bool Prefix(Rect rect)
            {
                if (!Enabled)
                {
                    return true;
                }

                Texture2D frame = AtlasSwap.Own("SectionAtlas");
                if (frame == null)
                {
                    return true;
                }

                GUI.color = Color.white;
                Rect area = LizarbInterfaceMod.Inset(rect);
                AtlasSwap.DrawScaled(area, frame, true);
                DrawGrain(area, 3f, 10f);
                return false;
            }
        }
    }
}

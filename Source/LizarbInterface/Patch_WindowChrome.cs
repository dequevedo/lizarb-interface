using HarmonyLib;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    internal static class Patch_WindowChrome
    {
        private static bool Enabled => LizarbInterfaceMod.Settings.enabled &&
                                     LizarbInterfaceMod.Settings.skinWindows &&
                                     AtlasSwap.Ready;

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

            Texture2D grain = AtlasSwap.Own("Pattern_" + settings.backgroundPattern, tiling: true);
            if (grain == null)
            {
                return;
            }

            Rect interior = rect.ContractedBy(inset);

            float r = Mathf.Min(radius, interior.width / 2f, interior.height / 2f);

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, settings.backgroundGrain * previous.a);

            Band(interior, grain, new Rect(interior.x, interior.y + r, interior.width, interior.height - r * 2f));
            Arc(interior, grain, r, top: true);
            Arc(interior, grain, r, top: false);

            GUI.color = previous;
        }

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

        private static void Band(Rect interior, Texture2D grain, Rect band)
        {
            if (band.width <= 0f || band.height <= 0f)
            {
                return;
            }

            float tileW = grain.width / AtlasSwap.Scale;
            float tileH = grain.height / AtlasSwap.Scale;

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

                Color previous = GUI.color;
                GUI.color = Color.white;
                Rect area = LizarbInterfaceMod.Inset(rect);
                AtlasSwap.DrawScaled(area, frame, true, null, tiled: true);
                DrawGrain(area, 5f, 22f);
                GUI.color = previous;
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

                Color previous = GUI.color;
                GUI.color = colorFactor;
                Rect area = LizarbInterfaceMod.Inset(rect);
                AtlasSwap.DrawScaled(area, frame, true, null, tiled: true);
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

                Color previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, previous.a);
                Rect area = LizarbInterfaceMod.Inset(rect);
                AtlasSwap.DrawScaled(area, frame, true, null, tiled: true);
                DrawGrain(area, 3f, 10f);
                GUI.color = previous;
                return false;
            }
        }
    }
}

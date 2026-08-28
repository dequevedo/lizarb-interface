using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    internal static class ArchitectColorContext
    {
        public static Color? Current;

        public static Rect Button;

        public static string Icon;

        public static bool Painting;

        public static void Clear()
        {
            Current = null;
            Icon = null;
            Button = default;
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Architect), "DoCategoryButton")]
    internal static class Patch_DoCategoryButton
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(ArchitectCategoryTab panel, bool enabled)
        {
            ArchitectColorContext.Clear();

            if (!enabled || panel?.def == null)
            {
                return;
            }

            Color hue = CategoryPalette.HueFor(panel.def);
            if (hue != Color.white)
            {
                ArchitectColorContext.Current = hue;
            }

            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings != null && settings.enabled && settings.architectIcons)
            {
                ArchitectColorContext.Icon = CategoryPalette.IconFor(panel.def);
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            if (ArchitectColorContext.Icon != null && Event.current.type == EventType.Repaint)
            {
                ArchitectIcons.Draw(ArchitectColorContext.Button, ArchitectColorContext.Icon);
            }

            ArchitectColorContext.Clear();
            GUI.color = Color.white;
        }
    }

    [HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawAtlas), typeof(Rect), typeof(Texture2D), typeof(bool))]
    internal static class Patch_ArchitectPlate
    {
        private static void Postfix(Rect rect)
        {
            if (!ArchitectColorContext.Current.HasValue || ArchitectColorContext.Painting)
            {
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            float alpha = LizarbInterfaceMod.Settings.architectPlateAlpha;
            if (alpha <= 0.001f)
            {
                return;
            }

            Rect plate = rect.ContractedBy(3f);
            Color tint = ArchitectColorContext.Current.Value.ToTransparent(alpha);

            ArchitectColorContext.Painting = true;
            ArchitectPlate.Draw(plate, LizarbInterfaceMod.Settings.architectPlateStyle, tint);
            ArchitectColorContext.Painting = false;
        }
    }

    [HarmonyPatch(typeof(Widgets), nameof(Widgets.ButtonTextSubtle))]
    internal static class Patch_ButtonTextSubtle
    {
        private static void Prefix(Rect rect, ref float textLeftMargin, ref Color? labelColor)
        {
            if (ArchitectColorContext.Icon != null)
            {
                ArchitectColorContext.Button = rect;

                float current = textLeftMargin < 0f ? rect.width * 0.15f : textLeftMargin;
                textLeftMargin = Mathf.Max(current, ArchitectIcons.MarginFor(rect));
            }

            if (!ArchitectColorContext.Current.HasValue)
            {
                return;
            }

            if (labelColor.HasValue || !LizarbInterfaceMod.Settings.architectColorLabels)
            {
                return;
            }

            labelColor = Readable(ArchitectColorContext.Current.Value);
        }

        internal static Color Readable(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            return Color.HSVToRGB(h, Mathf.Min(s, 0.70f), Mathf.Max(v, 0.95f));
        }
    }

    internal static class ArchitectIcons
    {
        private const float MaxSize = 26f;
        private const float Pad = 5f;

        internal static float MarginFor(Rect rect)
        {
            return Size(rect) + Pad * 2f;
        }

        internal static void Draw(Rect rect, string icon)
        {
            if (rect.width <= 0f)
            {
                return;
            }

            Texture2D tex = AtlasSwap.Shared("Icon" + icon);
            if (tex == null)
            {
                return;
            }

            float size = Size(rect);
            var at = new Rect(rect.x + Pad, rect.y + (rect.height - size) * 0.5f, size, size);

            Color old = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(at, tex);
            GUI.color = old;
        }

        private static float Size(Rect rect)
        {
            return Mathf.Min(MaxSize, Mathf.Max(12f, rect.height - Pad * 2f));
        }
    }
}

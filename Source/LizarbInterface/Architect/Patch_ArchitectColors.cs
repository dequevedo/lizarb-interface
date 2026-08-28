using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    /// <summary>State of the category button being drawn; cleared the rest of the time.</summary>
    internal static class ArchitectColorContext
    {
        public static Color? Current;

        /// <summary>Rect of the button, captured from the ButtonTextSubtle call.</summary>
        public static Rect Button;

        public static string Icon;

        /// <summary>Guards against the plate re-entering our own DrawAtlas postfix.</summary>
        public static bool Painting;

        public static void Clear()
        {
            Current = null;
            Icon = null;
            Button = default;
        }
    }

    /// <summary>
    /// Publishes the category colour and icon for the patches below. Draws nothing.
    ///
    /// Prefix and not a transpiler: Architect Icons transpiles this same method,
    /// replacing the ButtonTextSubtle call, so a second transpiler looking for that
    /// call would silently no-op depending on load order.
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_Architect), "DoCategoryButton")]
    internal static class Patch_DoCategoryButton
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(ArchitectCategoryTab panel, bool enabled)
        {
            ArchitectColorContext.Clear();

            // Vanilla greys out categories the colony cannot build yet; leave those be.
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

        /// <summary>
        /// The icon goes last so it sits above the label. The rect comes from the
        /// ButtonTextSubtle call rather than being recomputed here, which keeps this
        /// working whatever another mod does to the layout.
        /// </summary>
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

    /// <summary>
    /// Paints the colour onto the button plate.
    ///
    /// GUI.color cannot do this: DrawAtlas multiplies a dark atlas, and multiplication
    /// only removes light. The colour has to be drawn on top. A postfix here lands
    /// between the plate and the label, so the text stays unwashed.
    ///
    /// The shape comes from a greyscale 9-slice in the active skin, so it picks up
    /// that theme's corner radius instead of being a square inside a rounded frame.
    /// </summary>
    [HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawAtlas), typeof(Rect), typeof(Texture2D), typeof(bool))]
    internal static class Patch_ArchitectPlate
    {
        private static void Postfix(Rect rect)
        {
            // First line on purpose: DrawAtlas is engine-wide, so every unrelated call
            // in the game must cost one null check and nothing more.
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

            // Inset so the frame survives underneath: enough to clear the outline plus
            // this mod's corner ornament.
            Rect plate = rect.ContractedBy(3f);
            Color tint = ArchitectColorContext.Current.Value.ToTransparent(alpha);

            Texture2D shape = ShapeFor(LizarbInterfaceMod.Settings.architectPlateStyle);
            if (shape == null)
            {
                Widgets.DrawBoxSolid(plate, tint);
                return;
            }

            Color old = GUI.color;
            GUI.color = tint;

            // Painting guards the re-entry: this DrawAtlas would otherwise land back
            // in this same postfix.
            ArchitectColorContext.Painting = true;
            AtlasSwap.DrawScaled(plate, shape, true);
            ArchitectColorContext.Painting = false;

            GUI.color = old;
        }

        /// <summary>Null for the flat style, or when the skin has no plate texture.</summary>
        private static Texture2D ShapeFor(string style)
        {
            switch (style)
            {
                case "Bar":   return AtlasSwap.Own("PlateBar");
                case "Frame": return AtlasSwap.Own("PlateFrame");
                case "Flat":  return null;
                default:      return AtlasSwap.Own("Plate");
            }
        }
    }

    /// <summary>
    /// Colours the category label, and reserves room for the icon.
    ///
    /// textLeftMargin is a real parameter of ButtonTextSubtle, so the label can be
    /// pushed right without touching the rect. Shrinking the rect instead would
    /// shrink the plate and the clickable area with it.
    /// </summary>
    [HarmonyPatch(typeof(Widgets), nameof(Widgets.ButtonTextSubtle))]
    internal static class Patch_ButtonTextSubtle
    {
        private static void Prefix(Rect rect, ref float textLeftMargin, ref Color? labelColor)
        {
            if (ArchitectColorContext.Icon != null)
            {
                ArchitectColorContext.Button = rect;

                // -1 means "use the default", which has to be resolved before it can
                // be compared. Never narrower than what another mod already asked for.
                float current = textLeftMargin < 0f ? rect.width * 0.15f : textLeftMargin;
                textLeftMargin = Mathf.Max(current, ArchitectIcons.MarginFor(rect));
            }

            if (!ArchitectColorContext.Current.HasValue)
            {
                return;
            }

            // A colour already set means "no search match" or "disabled". Both carry
            // meaning, so never overwrite them.
            if (labelColor.HasValue || !LizarbInterfaceMod.Settings.architectColorLabels)
            {
                return;
            }

            labelColor = Readable(ArchitectColorContext.Current.Value);
        }

        /// <summary>Caps saturation and floors brightness so the hue stays legible as text.</summary>
        private static Color Readable(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            return Color.HSVToRGB(h, Mathf.Min(s, 0.70f), Mathf.Max(v, 0.95f));
        }
    }

    /// <summary>
    /// This mod's own category icons: white glyphs with a black outline baked into
    /// the same distance field, so the outline can never gap or drift.
    ///
    /// Drawn white rather than tinted. The plate behind already carries the category
    /// colour, and colouring both leaves the button monochrome and harder to read.
    /// </summary>
    internal static class ArchitectIcons
    {
        private const float MaxSize = 26f;
        private const float Pad = 5f;

        /// <summary>Left margin a button needs for its icon, gap included.</summary>
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

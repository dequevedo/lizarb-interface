using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Widens the Architect menu until the longest category name fits.
    ///
    /// Vanilla hardcodes 200px for a two-column grid, which is enough for the
    /// English Core labels in the stock font and nothing else. Change the font,
    /// nudge the size slider, or install a mod with a longer category name, and the
    /// text clips. Nothing in the game recomputes this.
    ///
    /// DoWindowContents takes butWidth as inRect.width / 2, so widening the tab is
    /// all it takes. Architect Icons postfixes the same getter with a flat +32;
    /// postfixes compose, and the Max below keeps whichever is larger.
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_Architect), nameof(MainTabWindow_Architect.RequestedTabSize), MethodType.Getter)]
    internal static class Patch_ArchitectTabSize
    {
        private static void Postfix(ref Vector2 __result)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.enabled || !settings.architectAutoWidth)
            {
                return;
            }

            __result.x = Mathf.Max(__result.x, ArchitectWidth.Required());
        }
    }

    internal static class ArchitectWidth
    {
        /// <summary>Room after the label, so it never sits against the frame.</summary>
        private const float RightPad = 12f;

        /// <summary>
        /// Vanilla puts the label at rect.width * 0.15f when no margin is given, so
        /// the label owns the other 85%. Solving width = 0.15 * width + text + pad
        /// for width is where the divisor comes from.
        /// </summary>
        private const float LabelShare = 0.85f;

        /// <summary>Absurd fonts should not produce an absurd window.</summary>
        private const float MaxWidth = 560f;

        private static float cached;
        private static string key;

        internal static float Required()
        {
            // Measuring every category on every frame would mean a GUIContent per
            // label per frame. The inputs only change when a setting does.
            LizarbInterfaceSettings s = LizarbInterfaceMod.Settings;
            string now = s.fontName + "|" + s.fontOffsetSmall + "|" + s.architectIcons + "|" +
                         DefDatabase<DesignationCategoryDef>.DefCount + "|" +
                         LanguageDatabase.activeLanguage?.folderName;

            if (now == key)
            {
                return cached;
            }

            key = now;
            try
            {
                cached = Measure(s);
            }
            catch (System.Exception e)
            {
                Log.WarningOnce("[LizarbInterface] could not measure the architect labels: " +
                                e.Message, 0x5A12E);
                cached = 0f;
            }

            return cached;
        }

        private static float Measure(LizarbInterfaceSettings settings)
        {
            GameFont previous = Text.Font;
            Text.Font = GameFont.Small;

            float widest = 0f;
            List<DesignationCategoryDef> defs =
                DefDatabase<DesignationCategoryDef>.AllDefsListForReading;

            for (int i = 0; i < defs.Count; i++)
            {
                // Every def, not just the visible ones: visibility turns on with
                // research and a resize at that moment would be jarring.
                float w = Text.CalcSize(defs[i].LabelCap).x;
                if (w > widest)
                {
                    widest = w;
                }
            }

            Text.Font = previous;

            // Whichever reservation is bigger wins, exactly as ButtonTextSubtle does.
            float byShare = (widest + RightPad) / LabelShare;
            float byIcon = (settings.architectIcons ? IconReserve : 0f) + widest + RightPad;

            return Mathf.Min(MaxWidth, Mathf.Max(byShare, byIcon) * MainTabWindow_Architect.ColumnCount);
        }

        /// <summary>Matches ArchitectIcons.MarginFor for a 32px button.</summary>
        private const float IconReserve = 32f;

        /// <summary>Forces a recount, for when the font is applied.</summary>
        internal static void Invalidate()
        {
            key = null;
        }
    }
}

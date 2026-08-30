using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    [HarmonyPatch(typeof(MainTabWindow_Architect), nameof(MainTabWindow_Architect.RequestedTabSize), MethodType.Getter)]
    internal static class Patch_ArchitectTabSize
    {
        private static void Postfix(ref Vector2 __result)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings != null && settings.enabled && settings.architectAutoWidth)
            {
                __result.x = Mathf.Max(__result.x, ArchitectWidth.Required());
            }

            float pad = ArchitectPadding.Amount;
            __result.x += pad * 2f;
            __result.y += pad * 2f;
        }
    }

    internal static class ArchitectWidth
    {
        private const float RightPad = 12f;

        private const float LabelShare = 0.85f;

        private const float MaxWidth = 560f;

        private static float cached;
        private static string key;

        internal static float Required()
        {
            LizarbInterfaceSettings s = LizarbInterfaceMod.Settings;
            string now = s.fontName + "|" + s.fontOffsetSmall + "|" + s.ownIcons + "|" +
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
                float w = Text.CalcSize(defs[i].LabelCap).x;
                if (w > widest)
                {
                    widest = w;
                }
            }

            Text.Font = previous;

            float byShare = (widest + RightPad) / LabelShare;
            float byIcon = (settings.ownIcons ? IconReserve : 0f) + widest + RightPad;

            return Mathf.Min(MaxWidth, Mathf.Max(byShare, byIcon) * MainTabWindow_Architect.ColumnCount);
        }

        private const float IconReserve = 32f;

        internal static void Invalidate()
        {
            key = null;
        }
    }
}

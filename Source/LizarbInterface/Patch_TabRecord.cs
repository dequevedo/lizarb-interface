using HarmonyLib;
using LudeonTK;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Reimplements TabRecord.Draw. TabAtlas is a private static readonly field that
    /// Draw reads directly, and writing it never took effect, so the texture is passed
    /// in as a local instead. Body mirrors vanilla; only the pixels differ.
    /// </summary>
    [HarmonyPatch(typeof(TabRecord), nameof(TabRecord.Draw))]
    internal static class Patch_TabRecord_Draw
    {
        private const float EndWidth = 30f;
        private const float MiddleGraphicWidth = 4f;

        // No HarmonyPriority on purpose: this returns false, and the first prefix that
        // does short-circuits the rest. Registering last means we yield to other mods.
        private static bool Prefix(TabRecord __instance, Rect rect)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.enabled || !settings.skinTabs)
            {
                return true;
            }

            Texture2D atlas = AtlasSwap.Own("TabAtlas");
            if (atlas == null)
            {
                return true;
            }

            // Shrink only what we paint; the label keeps the original rect so the text
            // stays where vanilla put it.
            Rect area = LizarbInterfaceMod.Inset(rect);

            // TabAtlas is NOT a 9-slice: vanilla cuts it in three with hard pixel
            // offsets (0..30, 30..34 stretched, 34..64), so it must be exactly 64 wide.
            Rect leftRect = new Rect(area) { width = EndWidth };
            Rect rightRect = new Rect(area) { width = EndWidth, x = area.x + area.width - EndWidth };

            Rect middleRect = new Rect(area);
            middleRect.x += EndWidth;
            middleRect.width -= EndWidth * 2f;
            middleRect.xMin = UIScaling.AdjustCoordToUIScalingFloor(middleRect.xMin);
            middleRect.xMax = UIScaling.AdjustCoordToUIScalingCeil(middleRect.xMax);

            float w = atlas.width;
            var leftUV = new Rect(0f, 0f, 15f / 32f, 1f);
            var middleUV = new Rect(EndWidth / w, 0f, MiddleGraphicWidth / w, 1f);
            var rightUV = new Rect(17f / 32f, 0f, 15f / 32f, 1f);

            Widgets.DrawTexturePart(leftRect, leftUV, atlas);
            Widgets.DrawTexturePart(middleRect, middleUV, atlas);
            Widgets.DrawTexturePart(rightRect, rightUV, atlas);

            GUI.color = __instance.labelColor ?? Color.white;

            Rect labelRect = rect;
            labelRect.width -= 10f;
            if (Mouse.IsOver(labelRect))
            {
                GUI.color = Color.yellow;
                labelRect.x += 2f;
                labelRect.y -= 2f;
            }

            if (!__instance.TutorTag.NullOrEmpty())
            {
                UIHighlighter.HighlightOpportunity(labelRect, __instance.TutorTag);
            }

            Text.WordWrap = false;
            Widgets.Label(rect, __instance.label);
            Text.WordWrap = true;
            GUI.color = Color.white;

            if (!__instance.Selected)
            {
                // DrawTexturePart flips the UV Y, so (0.5, 0.01) is the TOP of the PNG.
                Rect underline = new Rect(area) { y = area.y + area.height - 1f, height = 1f };
                Widgets.DrawTexturePart(underline, new Rect(0.5f, 0.01f, 0.01f, 0.01f), atlas);
            }

            return false;
        }
    }
}

using HarmonyLib;
using LudeonTK;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    [HarmonyPatch(typeof(TabRecord), nameof(TabRecord.Draw))]
    internal static class Patch_TabRecord_Draw
    {
        private const float EndWidth = 30f;
        private const float MiddleGraphicWidth = 4f;

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

            Rect area = LizarbInterfaceMod.Inset(rect);

            Rect leftRect = new Rect(area) { width = EndWidth };
            Rect rightRect = new Rect(area) { width = EndWidth, x = area.x + area.width - EndWidth };

            Rect middleRect = new Rect(area);
            middleRect.x += EndWidth;
            middleRect.width -= EndWidth * 2f;
            middleRect.xMin = UIScaling.AdjustCoordToUIScalingFloor(middleRect.xMin);
            middleRect.xMax = UIScaling.AdjustCoordToUIScalingCeil(middleRect.xMax);

            var leftUV = new Rect(0f, 0f, 30f / 64f, 1f);
            var middleUV = new Rect(30f / 64f, 0f, 4f / 64f, 1f);
            var rightUV = new Rect(34f / 64f, 0f, 30f / 64f, 1f);

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
                Rect underline = new Rect(area) { y = area.y + area.height - 1f, height = 1f };
                Widgets.DrawTexturePart(underline, new Rect(0.5f, 0.01f, 0.01f, 0.01f), atlas);
            }

            return false;
        }
    }
}

using HarmonyLib;
using LudeonTK;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    [HarmonyPatch(typeof(TabRecord), nameof(TabRecord.Draw))]
    internal static class Patch_TabRecord_Draw
    {
        private static bool warnedDead;

        private static bool VanillaAlive()
        {
            try
            {
                return AccessTools.StaticFieldRefAccess<Texture2D>(typeof(TabRecord), "TabAtlas") != null;
            }
            catch
            {
                return true;
            }
        }

        private static bool Prefix(TabRecord __instance, Rect rect)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            bool wanted = settings != null && settings.enabled && settings.skinTabs;

            Texture2D atlas = AtlasSwap.Own("TabAtlas");
            if (atlas == null)
            {
                return true;
            }

            if (!wanted)
            {
                if (VanillaAlive())
                {
                    return true;
                }

                if (!warnedDead)
                {
                    warnedDead = true;
                    Log.Warning("[LizarbInterface] the game's tab atlas has been destroyed, " +
                                "most likely by a mod that ships Textures/UI/Widgets/TabAtlas.png; " +
                                "drawing tabs with this mod's own so they keep working.");
                }
            }

            Rect area = LizarbInterfaceMod.Inset(rect);

            if (!AtlasSwap.HasOwn("TabAtlas"))
            {
                AtlasSwap.DrawScaled(area, atlas, true, null, tiled: true);
            }
            else
            {
                const float EndShare = 30f / 64f;
                const float MiddleShare = 4f / 64f;

                float end = Mathf.Min(atlas.width * EndShare / AtlasSwap.Scale, area.width / 2f);
                end = UIScaling.AdjustCoordToUIScalingFloor(end);

                float rows = Mathf.Min(1f, area.height * AtlasSwap.Scale / atlas.height);

                Rect leftRect = new Rect(area) { width = end };
                Rect rightRect = new Rect(area) { width = end, x = area.x + area.width - end };

                Rect middleRect = new Rect(area);
                middleRect.x += end;
                middleRect.width -= end * 2f;
                middleRect.xMin = UIScaling.AdjustCoordToUIScalingFloor(middleRect.xMin);
                middleRect.xMax = UIScaling.AdjustCoordToUIScalingCeil(middleRect.xMax);

                var leftUV = new Rect(0f, 0f, EndShare, rows);
                var middleUV = new Rect(EndShare, 0f, MiddleShare, rows);
                var rightUV = new Rect(1f - EndShare, 0f, EndShare, rows);

                Widgets.DrawTexturePart(leftRect, leftUV, atlas);
                Widgets.DrawTexturePart(middleRect, middleUV, atlas);
                Widgets.DrawTexturePart(rightRect, rightUV, atlas);
            }

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

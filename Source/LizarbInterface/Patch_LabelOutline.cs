using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using LudeonTK;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    internal static class Patch_LabelOutline
    {
        private static readonly Dictionary<int, Vector2[]> Kernels = BuildKernels();

        private static Dictionary<int, Vector2[]> BuildKernels()
        {
            var kernels = new Dictionary<int, Vector2[]>();

            for (int t = 1; t <= 2; t++)
            {
                var points = new List<Vector2>();
                int reach = t * t + t;

                for (int dy = -t; dy <= t; dy++)
                {
                    for (int dx = -t; dx <= t; dx++)
                    {
                        int d2 = dx * dx + dy * dy;
                        if (d2 > 0 && d2 <= reach)
                        {
                            points.Add(new Vector2(dx, dy));
                        }
                    }
                }

                kernels[t] = points.ToArray();
            }

            return kernels;
        }

        private static readonly Dictionary<string, string> withoutColor =
            new Dictionary<string, string>();

        private const int CacheCap = 512;

        private static string WithoutColorTags(string text)
        {
            if (text.IndexOf("<color", StringComparison.Ordinal) < 0)
            {
                return text;
            }

            if (withoutColor.TryGetValue(text, out string cached))
            {
                return cached;
            }

            var builder = new StringBuilder(text.Length);
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '<')
                {
                    if (i + 6 <= text.Length && string.CompareOrdinal(text, i, "<color", 0, 6) == 0)
                    {
                        int close = text.IndexOf('>', i);
                        if (close >= 0)
                        {
                            i = close + 1;
                            continue;
                        }
                    }
                    else if (i + 8 <= text.Length && string.CompareOrdinal(text, i, "</color>", 0, 8) == 0)
                    {
                        i += 8;
                        continue;
                    }
                }

                builder.Append(text[i]);
                i++;
            }

            string result = builder.ToString();

            if (withoutColor.Count >= CacheCap)
            {
                withoutColor.Clear();
            }

            withoutColor[text] = result;
            return result;
        }

        private static bool drawing;

        private static bool Active
        {
            get
            {
                LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
                if (settings == null || !settings.enabled || !settings.textOutline || drawing)
                {
                    return false;
                }

                if (Text.Font == GameFont.Tiny && !settings.outlineTinyText)
                {
                    return false;
                }

                return true;
            }
        }

        private static Rect Snap(Rect rect)
        {
            float half = Prefs.UIScale / 2f;
            if (Prefs.UIScale <= 1f || Mathf.Abs(half - Mathf.Floor(half)) <= float.Epsilon)
            {
                return rect;
            }

            Rect snapped = rect;
            snapped.xMin = UIScaling.AdjustCoordToUIScalingFloor(rect.xMin);
            snapped.yMin = UIScaling.AdjustCoordToUIScalingFloor(rect.yMin);
            snapped.xMax = UIScaling.AdjustCoordToUIScalingCeil(rect.xMax + 1E-05f);
            snapped.yMax = UIScaling.AdjustCoordToUIScalingCeil(rect.yMax + 1E-05f);
            return snapped;
        }

        private static void Outline(Rect rect, string text, bool snap)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings.outlineOpacity <= 0.001f)
            {
                return;
            }

            int thickness = Mathf.Clamp(Mathf.RoundToInt(settings.outlineThickness), 1, 2);
            if (Text.Font < GameFont.Medium)
            {
                thickness = 1;
            }

            if (!Kernels.TryGetValue(thickness, out Vector2[] kernel))
            {
                return;
            }

            float scale = GUI.matrix.m00;
            float px = scale > 0.01f ? 1f / scale : 1f;

            Rect basis = snap ? Snap(rect) : rect;
            Color previous = GUI.color;

            Color ink = LizarbInterfaceMod.OutlineColor;
            GUI.color = new Color(ink.r, ink.g, ink.b, previous.a * settings.outlineOpacity);

            drawing = true;
            GUIStyle style = Text.CurFontStyle;
            text = WithoutColorTags(text);

            for (int i = 0; i < kernel.Length; i++)
            {
                Rect offset = basis;
                offset.x += kernel[i].x * px;
                offset.y += kernel[i].y * px;
                GUI.Label(offset, text, style);
            }

            drawing = false;
            GUI.color = previous;
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.Label), typeof(Rect), typeof(string))]
        internal static class LabelString
        {
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Rect rect, string label)
            {
                if (!Active || label.NullOrEmpty())
                {
                    return;
                }

                Outline(rect, label, snap: true);
            }
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.Label), typeof(Rect), typeof(GUIContent))]
        internal static class LabelContent
        {
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Rect rect, GUIContent content)
            {
                if (!Active || content == null || content.text.NullOrEmpty())
                {
                    return;
                }

                Outline(rect, content.text, snap: false);
            }
        }
    }
}

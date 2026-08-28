using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using LudeonTK;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Draws a real outline by redrawing the text underneath, offset. IMGUI has no
    /// outline and GUIStyle cannot express one.
    ///
    /// THE OFFSETS ARE IN DEVICE PIXELS, NOT GUI UNITS. RimWorld keeps the UI scale in
    /// GUI.matrix, so 1 GUI unit is UIScale device pixels; at 1.25 each copy lands on a
    /// different subpixel phase and Unity rasterises it differently. Thin copies read
    /// as gaps, and the asymmetric error reads as a drop shadow. Offsetting by
    /// 1/scale makes every copy the same rasterisation, moved a whole pixel.
    ///
    /// The kernel is a disc (dx*dx + dy*dy &lt;= t*t + t): the eight neighbours at t=1,
    /// rounded corners at t=2 instead of four nubs on the diagonals.
    /// </summary>
    internal static class Patch_LabelOutline
    {
        /// <summary>
        /// Offsets in device pixels, one entry per supported thickness. Built once:
        /// the shape never changes, only the scale factor applied at draw time.
        /// </summary>
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

        /// <summary>Stripped text keyed by the original. Bounded; cleared wholesale.</summary>
        private static readonly Dictionary<string, string> withoutColor =
            new Dictionary<string, string>();

        private const int CacheCap = 512;

        /// <summary>
        /// Strips &lt;color&gt; tags. THIS IS WHAT KEEPS THE OUTLINE DARK.
        ///
        /// RimWorld labels are rich text and many carry colour markup (pawn titles,
        /// tooltip headings). The tag beats the GUI.color we set, so the copies drew
        /// that span in ITS colour: eight grey copies around grey text, which reads as
        /// a fat smear rather than an outline.
        ///
        /// Safe for layout: markup is zero-width either way. &lt;b&gt;, &lt;i&gt; and
        /// &lt;size&gt; DO change glyph metrics and are left alone.
        /// </summary>
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

                // Never outline Tiny. A 1px ring around a ~10px glyph closes the
                // counters (the holes in a, e, g) and the word turns into a smudge.
                // It showed up first on the backstory titles in the character tab,
                // which are Tiny and coloured, so the outline had the least contrast
                // to work with and did the most damage.
                if (Text.Font == GameFont.Tiny && !settings.outlineTinyText)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Mirrors the pixel snapping in Widgets.Label so the outline starts from the
        /// same rect vanilla will use. NOT what keeps the ring aligned. The
        /// device-pixel offsets do that.
        /// </summary>
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
            if (!Kernels.TryGetValue(thickness, out Vector2[] kernel))
            {
                return;
            }

            // One device pixel in GUI units. Read from the live GUI.matrix, not
            // Prefs.UIScale: the window open animation and other mods scale it too.
            float scale = GUI.matrix.m00;
            float px = scale > 0.01f ? 1f / scale : 1f;

            Rect basis = snap ? Snap(rect) : rect;
            Color previous = GUI.color;

            // Colour comes from the theme, not pure black: against the warm skins a
            // true black ring reads as a hole punched behind the letters.
            //
            // The label's own alpha is MULTIPLIED by the setting rather than replaced
            // by it, so faded text still gets a faded outline instead of a hard ghost.
            Color ink = LizarbInterfaceMod.OutlineColor;
            GUI.color = new Color(ink.r, ink.g, ink.b, previous.a * settings.outlineOpacity);

            // Re-entrancy guard: without it these draws would be caught by our own
            // prefix through anything that routes back into Widgets.
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

                // This overload draws straight through GUI.Label with no snapping, so
                // the outline must not snap either.
                Outline(rect, content.text, snap: false);
            }
        }
    }
}

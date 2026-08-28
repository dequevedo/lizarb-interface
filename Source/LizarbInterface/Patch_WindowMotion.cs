using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using RimWorld;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Opacity and open animation for dialogs, both riding Window.WindowOnGUI.
    /// Main panels are skipped: they are the game's furniture, not dialogs, and they
    /// open constantly.
    /// </summary>
    [HarmonyPatch(typeof(Window), nameof(Window.WindowOnGUI))]
    internal static class Patch_WindowMotion
    {
        private const float MinDuration = 0.01f;
        private const float StartScale = 0.94f;

        private static readonly Dictionary<Window, float> openedAt = new Dictionary<Window, float>();

        private static bool ShouldSkip(Window window)
        {
            return window is MainTabWindow
                   || window is ImmediateWindow
                   || window.layer != WindowLayer.Dialog && window.layer != WindowLayer.SubSuper;
        }

        /// <summary>Harmony allows one __state, so colour and matrix travel together.</summary>
        private struct State
        {
            public Color Color;
            public Matrix4x4 Matrix;
        }

        private static void Prefix(Window __instance, ref State __state)
        {
            __state.Color = GUI.color;
            __state.Matrix = GUI.matrix;

            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.enabled || ShouldSkip(__instance))
            {
                return;
            }

            float alpha = settings.windowOpacity;

            if (settings.windowAnimation)
            {
                if (!openedAt.TryGetValue(__instance, out float start))
                {
                    start = Time.realtimeSinceStartup;
                    openedAt[__instance] = start;
                }

                // Real time, not frames, so it looks the same at 30 and 144 fps.
                float duration = Mathf.Max(MinDuration, settings.animationDuration);
                float progress = Mathf.Clamp01((Time.realtimeSinceStartup - start) / duration);

                if (progress < 1f)
                {
                    float eased = 1f - Mathf.Pow(1f - progress, 3f);
                    float scale = Mathf.Lerp(StartScale, 1f, eased);

                    // Multiplied on the RIGHT: GUI.matrix maps GUI coords to screen and
                    // the pivot is in GUI coords, so the scale must happen before that
                    // mapping. On the left it applies in screen space and the window
                    // lands wrong at any UI scale but 1.
                    Vector2 pivot = __instance.windowRect.center;
                    GUI.matrix = GUI.matrix
                                 * Matrix4x4.TRS(pivot, Quaternion.identity, Vector3.one)
                                 * Matrix4x4.Scale(new Vector3(scale, scale, 1f))
                                 * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);

                    alpha *= eased;
                }
            }

            if (alpha < 0.999f)
            {
                Color c = GUI.color;
                GUI.color = new Color(c.r, c.g, c.b, c.a * alpha);
            }
        }

        /// <summary>
        /// Restore, never force identity: RimWorld keeps the UI scale in GUI.matrix, so
        /// resetting it here redraws everything after this window at the wrong scale.
        /// </summary>
        private static void Postfix(State __state)
        {
            GUI.color = __state.Color;
            GUI.matrix = __state.Matrix;
        }

        internal static void Forget(Window window)
        {
            openedAt.Remove(window);
        }
    }

    /// <summary>Keeps the dictionary above from growing for the whole session.</summary>
    [HarmonyPatch(typeof(WindowStack), nameof(WindowStack.TryRemove), typeof(Window), typeof(bool))]
    internal static class Patch_WindowClosed
    {
        private static void Postfix(Window window)
        {
            if (window != null)
            {
                Patch_WindowMotion.Forget(window);
            }
        }
    }
}

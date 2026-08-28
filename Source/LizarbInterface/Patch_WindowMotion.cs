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

        private static readonly Dictionary<Window, float> openedAt = new Dictionary<Window, float>();

        /// <summary>
        /// Which toggle governs this window. Type is checked before layer because a
        /// MainTabWindow sits on the same layer as plenty of things that are not one.
        /// </summary>
        private static bool Animates(Window window, LizarbInterfaceSettings settings)
        {
            if (window is MainTabWindow)
            {
                return settings.animateMainTabs;
            }

            if (window is ImmediateWindow)
            {
                return settings.animateImmediate;
            }

            if (window.layer == WindowLayer.Dialog || window.layer == WindowLayer.SubSuper)
            {
                return settings.windowAnimation;
            }

            return settings.animateOtherLayers;
        }

        /// <summary>
        /// Opacity keeps the ORIGINAL scope on purpose. Letting it follow the new
        /// animation toggles would make the Architect menu translucent the moment
        /// someone ticked "animate main panels", which is not what that asks for.
        /// </summary>
        private static bool Fades(Window window)
        {
            return !(window is MainTabWindow)
                   && !(window is ImmediateWindow)
                   && (window.layer == WindowLayer.Dialog || window.layer == WindowLayer.SubSuper);
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
            if (settings == null || !settings.enabled)
            {
                return;
            }

            float alpha = Fades(__instance) ? settings.windowOpacity : 1f;

            if (Animates(__instance, settings))
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
                    Vector2 pivot = __instance.windowRect.center;
                    if (WindowAnimation.Transform(settings.windowAnimationStyle, progress, pivot,
                                                  out Matrix4x4 transform, out float fade))
                    {
                        // Multiplied on the RIGHT: GUI.matrix maps GUI coords to screen
                        // and the pivot is in GUI coords, so the transform has to happen
                        // before that mapping. On the left it applies in screen space and
                        // the window lands wrong at any UI scale but 1.
                        GUI.matrix = GUI.matrix * transform;
                    }

                    alpha *= fade;
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

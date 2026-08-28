using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using RimWorld;
using Verse;

namespace LizarbInterface
{
    [HarmonyPatch(typeof(Window), nameof(Window.WindowOnGUI))]
    internal static class Patch_WindowMotion
    {
        private const float MinDuration = 0.01f;

        private static readonly Dictionary<Window, float> openedAt = new Dictionary<Window, float>();

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
            if (settings == null || !settings.enabled || !Animates(__instance, settings))
            {
                return;
            }

            if (!openedAt.TryGetValue(__instance, out float start))
            {
                start = Time.realtimeSinceStartup;
                openedAt[__instance] = start;
            }

            float duration = Mathf.Max(MinDuration, settings.animationDuration);
            float progress = Mathf.Clamp01((Time.realtimeSinceStartup - start) / duration);
            if (progress >= 1f)
            {
                return;
            }

            Vector2 pivot = __instance.windowRect.center;
            if (WindowAnimation.Transform(settings.windowAnimationStyle, progress, pivot,
                                          out Matrix4x4 transform))
            {
                GUI.matrix = GUI.matrix * transform;
            }
        }

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

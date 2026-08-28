using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    [HarmonyPatch(typeof(Widgets), nameof(Widgets.FillableBar),
        typeof(Rect), typeof(float), typeof(Texture2D), typeof(Texture2D), typeof(bool))]
    internal static class Patch_FillableBar
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(ref Texture2D bgTex)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings == null || !settings.enabled || !settings.skinWidgets || bgTex == null)
            {
                return;
            }

            if (!ReferenceEquals(bgTex, BaseContent.BlackTex))
            {
                return;
            }

            Texture2D mine = AtlasSwap.Own("BarBG");
            if (mine != null)
            {
                bgTex = mine;
            }
        }
    }

    [HarmonyPatch(typeof(Widgets), nameof(Widgets.BeginScrollView))]
    internal static class Patch_ScrollbarSkin
    {
        private static bool applied;
        private static string appliedTheme;

        private sealed class Original
        {
            public GUIStyle Style;
            public Texture2D Normal, Hover, Active, Focused;
            public RectOffset Border;
            public float FixedWidth, FixedHeight;
        }

        private static readonly List<Original> originals = new List<Original>();

        private static void Remember(GUIStyle style)
        {
            if (style == null)
            {
                return;
            }

            originals.Add(new Original
            {
                Style = style,
                Normal = style.normal.background,
                Hover = style.hover.background,
                Active = style.active.background,
                Focused = style.focused.background,
                Border = style.border,
                FixedWidth = style.fixedWidth,
                FixedHeight = style.fixedHeight,
            });
        }

        private static void Restore()
        {
            foreach (Original o in originals)
            {
                o.Style.normal.background = o.Normal;
                o.Style.hover.background = o.Hover;
                o.Style.active.background = o.Active;
                o.Style.focused.background = o.Focused;
                o.Style.border = o.Border;
                o.Style.fixedWidth = o.FixedWidth;
                o.Style.fixedHeight = o.FixedHeight;
            }

            originals.Clear();
            applied = false;
            appliedTheme = null;
        }

        private static void Prefix()
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            bool wanted = settings != null && settings.enabled && settings.skinScrollbars;

            if (!wanted)
            {
                if (applied)
                {
                    Restore();
                }

                return;
            }

            if (applied && appliedTheme == settings.theme)
            {
                return;
            }

            Texture2D track = AtlasSwap.Own("ScrollTrack");
            Texture2D thumb = AtlasSwap.Own("ScrollThumb");
            if (track == null || thumb == null || GUI.skin == null)
            {
                return;
            }

            if (!applied)
            {
                Remember(GUI.skin.verticalScrollbar);
                Remember(GUI.skin.horizontalScrollbar);
                Remember(GUI.skin.verticalScrollbarThumb);
                Remember(GUI.skin.horizontalScrollbarThumb);
                Remember(GUI.skin.verticalScrollbarUpButton);
                Remember(GUI.skin.verticalScrollbarDownButton);
                Remember(GUI.skin.horizontalScrollbarLeftButton);
                Remember(GUI.skin.horizontalScrollbarRightButton);
            }

            applied = true;
            appliedTheme = settings.theme;

            Skin(GUI.skin.verticalScrollbar, track, vertical: true);
            Skin(GUI.skin.horizontalScrollbar, track, vertical: false);
            Skin(GUI.skin.verticalScrollbarThumb, thumb, vertical: true);
            Skin(GUI.skin.horizontalScrollbarThumb, thumb, vertical: false);

            Blank(GUI.skin.verticalScrollbarUpButton);
            Blank(GUI.skin.verticalScrollbarDownButton);
            Blank(GUI.skin.horizontalScrollbarLeftButton);
            Blank(GUI.skin.horizontalScrollbarRightButton);
        }

        private static void Skin(GUIStyle style, Texture2D tex, bool vertical)
        {
            if (style == null)
            {
                return;
            }

            style.normal.background = tex;
            style.hover.background = tex;
            style.active.background = tex;
            style.focused.background = tex;

            style.border = vertical
                ? new RectOffset(0, 0, 6, 6)
                : new RectOffset(6, 6, 0, 0);
        }

        private static void Blank(GUIStyle style)
        {
            if (style == null)
            {
                return;
            }

            style.normal.background = null;
            style.hover.background = null;
            style.active.background = null;
            style.fixedWidth = 0f;
            style.fixedHeight = 0f;
        }
    }
}

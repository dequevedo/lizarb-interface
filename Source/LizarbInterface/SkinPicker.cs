using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace LizarbInterface
{
    internal sealed class Dialog_SkinPicker : Window
    {
        private const float SwatchHeight = 96f;
        private const float MinSwatchWidth = 150f;

        private static readonly string[] Order = { "Handpainted", "Squared", "Rounded", "Development" };

        private static readonly string[] Heading =
        {
            "LizarbInterface.ThemeHandpainted",
            "LizarbInterface.ThemeSquared",
            "LizarbInterface.ThemeRounded",
            "LizarbInterface.ThemeDevelopment",
        };

        private readonly Action<string> accept;
        private readonly string current;
        private Vector2 scroll;
        private float contentHeight = 600f;

        internal Dialog_SkinPicker(string selected, Action<string> onAccept)
        {
            current = selected;
            accept = onAccept;

            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(700f, 640f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;

            var title = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            Widgets.Label(title, "LizarbInterface.SkinPicker".Translate());

            Rect body = inRect;
            body.yMin += 36f;

            var view = new Rect(0f, 0f, body.width - 20f, contentHeight);
            Widgets.BeginScrollView(body, ref scroll, view);

            var listing = new Listing_Standard { maxOneColumn = true };
            listing.Begin(view);

            for (int i = 0; i < Order.Length; i++)
            {
                if (Order[i] == "Development" && !Prefs.DevMode)
                {
                    continue;
                }

                var bucket = new List<ThemeInfo>();
                foreach (ThemeInfo info in LizarbInterfaceMod.AllThemes)
                {
                    if (info.Group == Order[i])
                    {
                        bucket.Add(info);
                    }
                }

                if (bucket.Count > 0)
                {
                    DrawRow(listing, Heading[i], bucket);
                }
            }

            contentHeight = Mathf.Max(listing.CurHeight + 12f, body.height);
            listing.End();
            Widgets.EndScrollView();
        }

        private void DrawRow(Listing_Standard listing, string heading, List<ThemeInfo> skins)
        {
            listing.Gap(10f);
            Widgets.Label(listing.GetRect(24f), heading.Translate());
            listing.Gap(4f);

            int perRow = Mathf.Clamp(Mathf.FloorToInt(listing.ColumnWidth / MinSwatchWidth), 1, 4);
            int rows = Mathf.CeilToInt(skins.Count / (float)perRow);
            Rect block = listing.GetRect(rows * (SwatchHeight + 6f));
            float cell = block.width / perRow;

            for (int i = 0; i < skins.Count; i++)
            {
                Draw(new Rect(
                    block.x + (i % perRow) * cell,
                    block.y + (i / perRow) * (SwatchHeight + 6f),
                    cell - 8f,
                    SwatchHeight), skins[i]);
            }
        }

        private void Draw(Rect area, ThemeInfo skin)
        {
            Preset shown = Presets.FromTheme(skin);
            LizarbInterfaceMod.DrawPresetPreview(area, shown);

            if (skin.Id == current)
            {
                Widgets.DrawBox(area, 2);
            }
            else if (Mouse.IsOver(area))
            {
                Widgets.DrawHighlight(area);
            }

            if (Widgets.ButtonInvisible(area))
            {
                accept(skin.Id);
                RimWorld.SoundDefOf.Click.PlayOneShotOnCamera();
                Close();
            }
        }
    }
}

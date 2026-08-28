using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.Profile;
using Verse.Sound;

namespace LizarbInterface
{
    public class LizarbInterfaceSettings : ModSettings
    {
        public bool enabled = true;

        public string theme = "Foundry";

        public float inset = 1f;

        public const string DefaultFont = "Bungee";

        public string fontName = DefaultFont;

        public int fontOffsetTiny;
        public int fontOffsetSmall;
        public int fontOffsetMedium;

        public bool textOutline = true;

        public float outlineThickness = 2f;

        public float outlineOpacity = 0.7f;

        public bool outlineTinyText;

        public bool showAllFonts;

        public bool texturedBackground = true;

        public string backgroundPattern = "Hatch";

        public float backgroundGrain = 0.05f;

        public bool pointFilter;

        public bool grainOnButtons = true;

        public bool windowAnimation = true;

        public float animationDuration = 0.35f;

        public string windowAnimationStyle = "Slide";

        public bool animateMainTabs = true;

        public bool animateImmediate = true;

        public bool animateOtherLayers = true;

        public bool skinButtons = true;

        public bool skinWindows = true;

        public bool skinTabs = true;

        public bool skinWidgets = true;

        public bool skinScrollbars = true;

        public bool architectColors = true;

        public float architectPlateAlpha = 1f;

        public bool architectColorLabels;

        public bool architectAutoColor = true;

        public string architectPlateStyle = "Plate";

        public bool architectIcons = true;

        public bool architectAutoWidth = true;

        public bool architectShapeOutline = true;

        public int FontSizeOffset(int gameFontIndex)
        {
            switch (gameFontIndex)
            {
                case 0: return fontOffsetTiny;
                case 1: return fontOffsetSmall;
                default: return fontOffsetMedium;
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "enabled", defaultValue: true);
            Scribe_Values.Look(ref theme, "theme", "Foundry");
            Scribe_Values.Look(ref inset, "inset", 1f);
            Scribe_Values.Look(ref fontName, "fontName", DefaultFont);
            Scribe_Values.Look(ref fontOffsetTiny, "fontOffsetTiny", 0);
            Scribe_Values.Look(ref fontOffsetSmall, "fontOffsetSmall", 0);
            Scribe_Values.Look(ref fontOffsetMedium, "fontOffsetMedium", 0);
            Scribe_Values.Look(ref textOutline, "textOutline", defaultValue: true);
            Scribe_Values.Look(ref outlineThickness, "outlineThickness", 2f);
            Scribe_Values.Look(ref outlineOpacity, "outlineOpacity", 0.7f);
            Scribe_Values.Look(ref outlineTinyText, "outlineTinyText", defaultValue: false);
            Scribe_Values.Look(ref showAllFonts, "showAllFonts", defaultValue: false);
            Scribe_Values.Look(ref texturedBackground, "texturedBackground", defaultValue: true);
            Scribe_Values.Look(ref backgroundPattern, "backgroundPattern", "Hatch");
            Scribe_Values.Look(ref backgroundGrain, "backgroundGrain", 0.05f);
            Scribe_Values.Look(ref pointFilter, "pointFilter", defaultValue: false);
            Scribe_Values.Look(ref grainOnButtons, "grainOnButtons", defaultValue: true);
            Scribe_Values.Look(ref windowAnimation, "windowAnimation", defaultValue: true);
            Scribe_Values.Look(ref animationDuration, "animationDuration", 0.35f);
            Scribe_Values.Look(ref windowAnimationStyle, "windowAnimationStyle", "Slide");
            Scribe_Values.Look(ref animateMainTabs, "animateMainTabs", defaultValue: true);
            Scribe_Values.Look(ref animateImmediate, "animateImmediate", defaultValue: true);
            Scribe_Values.Look(ref animateOtherLayers, "animateOtherLayers", defaultValue: true);
            Scribe_Values.Look(ref skinButtons, "skinButtons", defaultValue: true);
            Scribe_Values.Look(ref skinWindows, "skinWindows", defaultValue: true);
            Scribe_Values.Look(ref skinTabs, "skinTabs", defaultValue: true);
            Scribe_Values.Look(ref skinWidgets, "skinWidgets", defaultValue: true);
            Scribe_Values.Look(ref skinScrollbars, "skinScrollbars", defaultValue: true);
            Scribe_Values.Look(ref architectColors, "architectColors", defaultValue: true);
            Scribe_Values.Look(ref architectPlateAlpha, "architectPlateAlpha", 1f);
            Scribe_Values.Look(ref architectColorLabels, "architectColorLabels", defaultValue: false);
            Scribe_Values.Look(ref architectAutoColor, "architectAutoColor", defaultValue: true);
            Scribe_Values.Look(ref architectPlateStyle, "architectPlateStyle", "Plate");
            Scribe_Values.Look(ref architectIcons, "architectIcons", defaultValue: true);
            Scribe_Values.Look(ref architectAutoWidth, "architectAutoWidth", defaultValue: true);
            Scribe_Values.Look(ref architectShapeOutline, "architectShapeOutline", defaultValue: true);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (architectPlateStyle == "Bar")
                {
                    architectPlateStyle = "Square";
                }

                if (System.Array.IndexOf(ArchitectPlate.Styles, architectPlateStyle) < 0)
                {
                    architectPlateStyle = "Plate";
                }

                if (System.Array.IndexOf(WindowAnimation.Styles, windowAnimationStyle) < 0)
                {
                    windowAnimationStyle = "Slide";
                }
            }
            base.ExposeData();
        }
    }

    public class LizarbInterfaceMod : Mod
    {
        public static LizarbInterfaceSettings Settings { get; private set; }

        public static string RootDir { get; private set; }

        public static ModContentPack Pack { get; private set; }

        private static readonly (string Id, string Pattern, Color Outline)[] Themes =
        {
            ("Brass",    "Hatch",    new Color(0.10f, 0.07f, 0.04f)),
            ("Iron",     "Bricks",   new Color(0.05f, 0.06f, 0.07f)),
            ("Royal",    "Medieval", new Color(0.06f, 0.05f, 0.11f)),
            ("Obsidian", "Chevron",  new Color(0.03f, 0.03f, 0.04f)),
            ("Verdant",  "Scales",   new Color(0.04f, 0.08f, 0.05f)),
            ("Bone",     "Dots",      new Color(0.09f, 0.08f, 0.05f)),
            ("Crimson",  "Scales",   new Color(0.11f, 0.04f, 0.04f)),
            ("Arcane",   "Dots",      new Color(0.04f, 0.03f, 0.10f)),
            ("Wood",     "Woodgrain", new Color(0.09f, 0.06f, 0.03f)),
            ("Flesh",    "Hatch",     new Color(0.10f, 0.04f, 0.04f)),
            ("Gothic",   "Medieval",  new Color(0.03f, 0.03f, 0.03f)),
            ("Aero",     "Dots",      new Color(0.03f, 0.06f, 0.09f)),
            ("Copper",   "Scales",    new Color(0.04f, 0.06f, 0.06f)),
            ("Ash",      "Dots",      new Color(0.05f, 0.05f, 0.05f)),
            ("Grimoire", "Hatch",     new Color(0.06f, 0.03f, 0.03f)),
            ("Foundry",  "Bricks",    new Color(0.05f, 0.04f, 0.04f)),
        };

        public static Color OutlineColor
        {
            get
            {
                string id = Settings?.theme;
                foreach (var t in Themes)
                {
                    if (t.Id == id)
                    {
                        return t.Outline;
                    }
                }

                return new Color(0.06f, 0.06f, 0.06f);
            }
        }

        private static readonly string[] Patterns =
        {
            "Hatch", "Medieval", "Scales", "Bricks", "Dots", "Chevron", "Woodgrain",
        };

        internal const string HarmonyId = "lizarb.interface";

        internal static Harmony Harmony { get; private set; }

        public LizarbInterfaceMod(ModContentPack content) : base(content)
        {
            RootDir = content.RootDir;
            Pack = content;
            Settings = GetSettings<LizarbInterfaceSettings>();
            Harmony = new Harmony(HarmonyId);
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static Rect Inset(Rect rect)
        {
            float i = Settings == null ? 0f : Settings.inset;
            if (i <= 0f || rect.width < i * 2f + 8f || rect.height < i * 2f + 8f)
            {
                return rect;
            }

            return rect.ContractedBy(i);
        }

        public override string SettingsCategory() => "Lizarb Interface";

        private readonly float[] contentHeight = { 600f, 600f, 600f, 600f, 600f, 600f };

        private enum Tab
        {
            Theme,
            Text,
            Surfaces,
            Windows,
            Architect,
            Components,
        }

        private static Tab tab = Tab.Theme;

        private readonly Vector2[] scrolls = new Vector2[6];

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var header = new Rect(inRect.x, inRect.y, inRect.width, 24f);
            Widgets.CheckboxLabeled(header, "LizarbInterface.Enabled".Translate(), ref Settings.enabled);

            var body = new Rect(
                inRect.x,
                inRect.y + header.height + TabDrawer.TabHeight,
                inRect.width,
                inRect.height - header.height - TabDrawer.TabHeight);

            Widgets.DrawMenuSection(body);

            var tabs = new List<TabRecord>
            {
                MakeTab(Tab.Theme), MakeTab(Tab.Text), MakeTab(Tab.Surfaces),
                MakeTab(Tab.Windows), MakeTab(Tab.Architect), MakeTab(Tab.Components),
            };

            TabDrawer.DrawTabs(body, tabs);

            Rect inner = body.ContractedBy(14f);
            int index = (int)tab;

            var view = new Rect(0f, 0f, inner.width - 24f, contentHeight[index]);
            Widgets.BeginScrollView(inner, ref scrolls[index], view);

            var listing = new Listing_Standard { maxOneColumn = true };
            listing.Begin(view);

            switch (tab)
            {
                case Tab.Theme:
                    Section(listing, "theme", DoThemeSection);
                    break;
                case Tab.Text:
                    Section(listing, "font", DoFontSection);
                    break;
                case Tab.Surfaces:
                    Section(listing, "surfaces", DoSurfacesSection);
                    break;
                case Tab.Windows:
                    Section(listing, "window", DoWindowSection);
                    break;
                case Tab.Architect:
                    Section(listing, "architect", DoArchitectSection);
                    break;
                case Tab.Components:
                    Section(listing, "components", DoComponentsSection);
                    break;
            }

            contentHeight[index] = Mathf.Max(listing.CurHeight + 24f, inner.height);

            listing.End();
            Widgets.EndScrollView();

            if (fontDirty)
            {
                fontDirty = false;
                FontEngine.Apply();

                ArchitectWidth.Invalidate();
            }
        }

        private static TabRecord MakeTab(Tab which)
        {
            Tab captured = which;
            return new TabRecord(
                ("LizarbInterface.Tab." + which).Translate(),
                () => tab = captured,
                tab == which);
        }

        private static readonly HashSet<string> reportedSections = new HashSet<string>();

        private static void Section(Listing_Standard listing, string name, Action<Listing_Standard> body)
        {
            try
            {
                body(listing);
            }
            catch (Exception e)
            {
                if (reportedSections.Add(name))
                {
                    Log.Error("[LizarbInterface] settings section '" + name + "' failed: " + e);
                }

                listing.Label("LizarbInterface.SectionFailed".Translate(name));
            }
        }

        private void DoThemeSection(Listing_Standard listing)
        {
            listing.Label("LizarbInterface.ThemeSection".Translate());

            const float SwatchHeight = 74f;
            const int PerRow = 4;
            int rows = Mathf.CeilToInt(Themes.Length / (float)PerRow);

            Rect block = listing.GetRect(rows * (SwatchHeight + 6f));
            float cell = block.width / PerRow;

            for (int i = 0; i < Themes.Length; i++)
            {
                var area = new Rect(
                    block.x + (i % PerRow) * cell,
                    block.y + (i / PerRow) * (SwatchHeight + 6f),
                    cell - 8f,
                    SwatchHeight);

                DrawThemeSwatch(area, Themes[i].Id);
            }
        }

        private void DrawThemeSwatch(Rect area, string theme)
        {
            bool selected = Settings.theme == theme;

            Texture2D frame = AtlasSwap.Preview(theme, "WindowAtlas");
            Texture2D button = AtlasSwap.Preview(theme, "ButtonBG");

            if (frame != null)
            {
                Widgets.DrawAtlas(area, frame);
            }

            if (button != null)
            {
                Widgets.DrawAtlas(new Rect(area.x + 12f, area.yMax - 34f, area.width - 24f, 24f), button);
            }

            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(area.x, area.y + 8f, area.width, 24f),
                          ("LizarbInterface.Theme." + theme).Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            if (selected)
            {
                GUI.color = Color.white;
                Widgets.DrawBox(area, 2);
                GUI.color = Color.white;
            }
            else if (Mouse.IsOver(area))
            {
                Widgets.DrawHighlight(area);
            }

            if (Widgets.ButtonInvisible(area))
            {
                Settings.theme = theme;

                foreach (var entry in Themes)
                {
                    if (entry.Id == theme)
                    {
                        Settings.backgroundPattern = entry.Pattern;
                        break;
                    }
                }

                RimWorld.SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void DoFontSection(Listing_Standard listing)
        {
            listing.Label("LizarbInterface.FontSection".Translate());

            string current = Settings.fontName.NullOrEmpty()
                ? "LizarbInterface.FontVanilla".Translate().ToString()
                : Settings.fontName;

            if (listing.ButtonText(current))
            {
                List<string> names = Settings.showAllFonts
                    ? FontEngine.InstalledFonts()
                    : FontEngine.CuratedFonts();

                Find.WindowStack.Add(new Dialog_FontPicker(names, Settings.fontName, SetFont));
            }

            listing.CheckboxLabeled(
                "LizarbInterface.ShowAllFonts".Translate(),
                ref Settings.showAllFonts,
                "LizarbInterface.ShowAllFonts.Tip".Translate());

            listing.CheckboxLabeled(
                "LizarbInterface.TextOutline".Translate(),
                ref Settings.textOutline,
                "LizarbInterface.TextOutline.Tip".Translate());

            if (Settings.textOutline)
            {
                listing.Label("LizarbInterface.OutlineThickness".Translate(Settings.outlineThickness.ToString("0")));
                Settings.outlineThickness = Mathf.Round(listing.Slider(Settings.outlineThickness, 1f, 2f));

                listing.Label("LizarbInterface.OutlineOpacity".Translate(Settings.outlineOpacity.ToStringPercent()));
                Settings.outlineOpacity = listing.Slider(Settings.outlineOpacity, 0f, 1f);

                listing.CheckboxLabeled(
                    "LizarbInterface.OutlineTiny".Translate(),
                    ref Settings.outlineTinyText,
                    "LizarbInterface.OutlineTiny.Tip".Translate());
            }

            listing.Gap();

            listing.Label("LizarbInterface.FontSize.Tiny".Translate(Signed(Settings.fontOffsetTiny)));
            Settings.fontOffsetTiny = SizeSlider(listing, Settings.fontOffsetTiny);

            listing.Label("LizarbInterface.FontSize.Small".Translate(Signed(Settings.fontOffsetSmall)));
            Settings.fontOffsetSmall = SizeSlider(listing, Settings.fontOffsetSmall);

            listing.Label("LizarbInterface.FontSize.Medium".Translate(Signed(Settings.fontOffsetMedium)));
            Settings.fontOffsetMedium = SizeSlider(listing, Settings.fontOffsetMedium);

            if (listing.ButtonText("LizarbInterface.FontReset".Translate(), null, 0.35f))
            {
                Settings.fontOffsetTiny = 0;
                Settings.fontOffsetSmall = 0;
                Settings.fontOffsetMedium = 0;
                SetFont(LizarbInterfaceSettings.DefaultFont);
            }
        }

        private int SizeSlider(Listing_Standard listing, int value)
        {
            int next = Mathf.RoundToInt(listing.Slider(value, -4f, 8f));
            if (next != value)
            {
                QueueFontApply();
            }

            return next;
        }

        private static bool fontDirty;

        internal static void QueueFontApply()
        {
            fontDirty = true;
        }

        private static string Signed(int v) => v > 0 ? "+" + v : v.ToString();

        private void SetFont(string name)
        {
            Settings.fontName = name;
            QueueFontApply();
        }

        private void DoSurfacesSection(Listing_Standard listing)
        {
            listing.Label("LizarbInterface.Inset".Translate(Settings.inset.ToString("0")));
            Settings.inset = Mathf.Round(listing.Slider(Settings.inset, 0f, 4f));
            listing.Label("LizarbInterface.Inset.Tip".Translate());

            listing.CheckboxLabeled(
                "LizarbInterface.PointFilter".Translate(),
                ref Settings.pointFilter,
                "LizarbInterface.PointFilter.Tip".Translate());

            listing.GapLine();
            DoBackgroundSection(listing);
        }

        private void DoWindowSection(Listing_Standard listing)
        {
            listing.CheckboxLabeled(
                "LizarbInterface.WindowAnimation".Translate(),
                ref Settings.windowAnimation,
                "LizarbInterface.WindowAnimation.Tip".Translate());

            listing.CheckboxLabeled(
                "LizarbInterface.AnimateMainTabs".Translate(),
                ref Settings.animateMainTabs,
                "LizarbInterface.AnimateMainTabs.Tip".Translate());

            listing.CheckboxLabeled(
                "LizarbInterface.AnimateImmediate".Translate(),
                ref Settings.animateImmediate,
                "LizarbInterface.AnimateImmediate.Tip".Translate());

            listing.CheckboxLabeled(
                "LizarbInterface.AnimateOther".Translate(),
                ref Settings.animateOtherLayers,
                "LizarbInterface.AnimateOther.Tip".Translate());

            bool anyAnimation = Settings.windowAnimation || Settings.animateMainTabs ||
                                Settings.animateImmediate || Settings.animateOtherLayers;
            if (anyAnimation)
            {
                listing.Label("LizarbInterface.AnimationDuration".Translate(
                    Mathf.RoundToInt(Settings.animationDuration * 1000f).ToString()));
                Settings.animationDuration = listing.Slider(Settings.animationDuration, 0.05f, 0.6f);

                listing.Gap(4f);
                listing.Label("LizarbInterface.AnimationStyle".Translate());
                DoAnimationStyleGrid(listing);
            }
        }

        private void DoArchitectSection(Listing_Standard listing)
        {
            listing.Label("LizarbInterface.Architect.Section".Translate());
            listing.Gap(4f);

            listing.CheckboxLabeled(
                "LizarbInterface.Architect.Icons".Translate(),
                ref Settings.architectIcons,
                "LizarbInterface.Architect.Icons.Tip".Translate());

            listing.CheckboxLabeled(
                "LizarbInterface.Architect.AutoWidth".Translate(),
                ref Settings.architectAutoWidth,
                "LizarbInterface.Architect.AutoWidth.Tip".Translate());

            if (Settings.architectIcons && ArchitectIconsModPresent)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = Color.yellow;
                listing.Label("LizarbInterface.Architect.Icons.Clash".Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            listing.GapLine();

            listing.CheckboxLabeled(
                "LizarbInterface.Architect.Enabled".Translate(),
                ref Settings.architectColors);

            if (!Settings.architectColors)
            {
                return;
            }

            listing.Gap();
            listing.Label("LizarbInterface.Architect.PlateStyle".Translate());
            DoPlateStyleGrid(listing);

            listing.CheckboxLabeled(
                "LizarbInterface.Architect.ShapeOutline".Translate(),
                ref Settings.architectShapeOutline,
                "LizarbInterface.Architect.ShapeOutline.Tip".Translate());

            listing.Gap();
            listing.Label("LizarbInterface.Architect.PlateAlpha".Translate(
                Settings.architectPlateAlpha.ToStringPercent()));
            Settings.architectPlateAlpha = listing.Slider(Settings.architectPlateAlpha, 0f, 1f);

            listing.CheckboxLabeled(
                "LizarbInterface.Architect.ColorLabels".Translate(),
                ref Settings.architectColorLabels,
                "LizarbInterface.Architect.ColorLabels.Tip".Translate());

            listing.CheckboxLabeled(
                "LizarbInterface.Architect.AutoColor".Translate(),
                ref Settings.architectAutoColor,
                "LizarbInterface.Architect.AutoColor.Tip".Translate());
        }

        private static string[] PlateStyles => ArchitectPlate.Styles;

        private void DoAnimationStyleGrid(Listing_Standard listing)
        {
            const float CellHeight = 28f;
            const float MinCellWidth = 150f;

            string[] styles = WindowAnimation.Styles;
            float width = listing.ColumnWidth;
            int columns = Mathf.Clamp(Mathf.FloorToInt(width / MinCellWidth), 1, 4);
            int rows = Mathf.CeilToInt(styles.Length / (float)columns);

            Rect area = listing.GetRect(rows * CellHeight);
            float cellWidth = width / columns;

            for (int i = 0; i < styles.Length; i++)
            {
                var cell = new Rect(
                    area.x + (i % columns) * cellWidth,
                    area.y + (i / columns) * CellHeight,
                    cellWidth,
                    CellHeight).ContractedBy(2f);

                bool selected = Settings.windowAnimationStyle == styles[i];
                if (selected)
                {
                    Widgets.DrawHighlightSelected(cell);
                }
                else if (Mouse.IsOver(cell))
                {
                    Widgets.DrawHighlight(cell);
                }

                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(cell, ("LizarbInterface.Animation." + styles[i]).Translate());
                Text.Anchor = TextAnchor.UpperLeft;

                if (Widgets.ButtonInvisible(cell))
                {
                    Settings.windowAnimationStyle = styles[i];
                    ReplayAnimation();
                    RimWorld.SoundDefOf.Click.PlayOneShotOnCamera();
                }
            }
        }

        private static void ReplayAnimation()
        {
            Window window = Find.WindowStack?.WindowOfType<RimWorld.Dialog_ModSettings>();
            if (window != null)
            {
                Patch_WindowMotion.Forget(window);
            }
        }

        private void DoPlateStyleGrid(Listing_Standard listing)
        {
            const float CellHeight = 40f;
            const float MinCellWidth = 290f;

            string[] styles = PlateStyles;
            float width = listing.ColumnWidth;
            int columns = Mathf.Clamp(Mathf.FloorToInt(width / MinCellWidth), 1, 3);
            int rows = Mathf.CeilToInt(styles.Length / (float)columns);

            Rect area = listing.GetRect(rows * CellHeight);
            float cellWidth = width / columns;

            for (int i = 0; i < styles.Length; i++)
            {
                var cell = new Rect(
                    area.x + (i % columns) * cellWidth,
                    area.y + (i / columns) * CellHeight,
                    cellWidth,
                    CellHeight);

                DoPlateStyleCell(cell, styles[i]);
            }
        }

        private void DoPlateStyleCell(Rect cell, string style)
        {
            bool selected = Settings.architectPlateStyle == style;
            Rect inner = cell.ContractedBy(2f);

            if (selected)
            {
                Widgets.DrawHighlightSelected(inner);
            }
            else if (Mouse.IsOver(inner))
            {
                Widgets.DrawHighlight(inner);
            }

            float sampleWidth = Mathf.Min(150f, inner.width * 0.55f);
            var sample = new Rect(inner.x + 4f, inner.y + 3f, sampleWidth, inner.height - 6f);
            DrawPlateSample(sample, style);

            Rect label = inner;
            label.xMin = sample.xMax + 10f;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(label, ("LizarbInterface.Architect.PlateStyle." + style).Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            TooltipHandler.TipRegion(inner,
                ("LizarbInterface.Architect.PlateStyle." + style + ".Tip").Translate());

            if (Widgets.ButtonInvisible(inner))
            {
                Settings.architectPlateStyle = style;
                RimWorld.SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }
        private void DrawPlateSample(Rect rect, string style)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Texture2D button = AtlasSwap.Own("ButtonSubtleAtlas");
            if (button == null)
            {
                Widgets.DrawBoxSolid(rect, new Color(0.2f, 0.18f, 0.16f));
            }
            else
            {
                AtlasSwap.DrawScaled(rect, button, true);
            }

            Color tint = new Color(205f / 255f, 137f / 255f, 95f / 255f, Settings.architectPlateAlpha);
            ArchitectPlate.Draw(rect.ContractedBy(3f), style, tint);

            ArchitectIcons.Draw(rect, "Production");

            Color label = Settings.architectColorLabels
                ? Patch_ButtonTextSubtle.Readable(tint)
                : Color.white;

            Color previous = GUI.color;
            GUI.color = label;
            Text.Anchor = TextAnchor.MiddleLeft;
            float textLeft = ArchitectIcons.MarginFor(rect);
            Widgets.Label(new Rect(rect.x + textLeft, rect.y, rect.width - textLeft - 4f, rect.height), "Production");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = previous;
        }
        private static bool ArchitectIconsModPresent =>
            ModLister.GetActiveModWithIdentifier("com.bymarcin.architecticons", ignorePostfix: true) != null;

        private void DoComponentsSection(Listing_Standard listing)
        {
            listing.Label("LizarbInterface.ComponentsSection".Translate());
            listing.Gap(4f);

            listing.CheckboxLabeled(
                "LizarbInterface.SkinButtons".Translate(),
                ref Settings.skinButtons,
                "LizarbInterface.SkinButtons.Tip".Translate());

            listing.CheckboxLabeled(
                "LizarbInterface.SkinWindows".Translate(),
                ref Settings.skinWindows,
                "LizarbInterface.SkinWindows.Tip".Translate());

            listing.CheckboxLabeled(
                "LizarbInterface.SkinTabs".Translate(),
                ref Settings.skinTabs,
                "LizarbInterface.SkinTabs.Tip".Translate());

            listing.CheckboxLabeled(
                "LizarbInterface.SkinWidgets".Translate(),
                ref Settings.skinWidgets,
                "LizarbInterface.SkinWidgets.Tip".Translate());

            listing.CheckboxLabeled(
                "LizarbInterface.SkinScrollbars".Translate(),
                ref Settings.skinScrollbars,
                "LizarbInterface.SkinScrollbars.Tip".Translate());

            listing.GapLine();

            if (listing.ButtonText("LizarbInterface.ResetAll".Translate(), null, 0.4f))
            {
                ResetAll();
            }
        }

        private void ResetAll()
        {
            var fresh = new LizarbInterfaceSettings();
            foreach (FieldInfo field in typeof(LizarbInterfaceSettings)
                     .GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                field.SetValue(Settings, field.GetValue(fresh));
            }

            QueueFontApply();
            RimWorld.SoundDefOf.Click.PlayOneShotOnCamera();
        }

        private void DoBackgroundSection(Listing_Standard listing)
        {
            listing.Label("LizarbInterface.BackgroundSection".Translate());
            listing.CheckboxLabeled(
                "LizarbInterface.TexturedBackground".Translate(),
                ref Settings.texturedBackground,
                "LizarbInterface.TexturedBackground.Tip".Translate());

            if (Settings.texturedBackground)
            {
                if (listing.ButtonText(("LizarbInterface.Pattern." + Settings.backgroundPattern).Translate()))
                {
                    var options = new List<FloatMenuOption>();
                    foreach (string p in Patterns)
                    {
                        string captured = p;
                        options.Add(new FloatMenuOption(
                            ("LizarbInterface.Pattern." + captured).Translate(),
                            () => Settings.backgroundPattern = captured));
                    }

                    Find.WindowStack.Add(new FloatMenu(options));
                }

                listing.Label("LizarbInterface.BackgroundGrain".Translate(Settings.backgroundGrain.ToStringPercent()));
                Settings.backgroundGrain = listing.Slider(Settings.backgroundGrain, 0f, 1f);

                listing.CheckboxLabeled(
                    "LizarbInterface.GrainOnButtons".Translate(),
                    ref Settings.grainOnButtons,
                    "LizarbInterface.GrainOnButtons.Tip".Translate());
            }
        }
    }

    [HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.UnloadUnusedUnityAssets))]
    internal static class Patch_UnloadUnusedUnityAssets
    {
        private static void Postfix()
        {
            LongEventHandler.ExecuteWhenFinished(FontEngine.Apply);
        }
    }

    [StaticConstructorOnStartup]
    internal static class FontEngineInit
    {
        static FontEngineInit()
        {
            FontEngine.Apply();
        }
    }
}

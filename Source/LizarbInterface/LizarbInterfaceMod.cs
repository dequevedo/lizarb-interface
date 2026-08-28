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
        /// <summary>Kill switch for the whole reskin. Restores the stock look.</summary>
        public bool enabled = true;

        /// <summary>Skin folder under Skins/. Changing it reloads every texture.</summary>
        public string theme = "Foundry";

        /// <summary>
        /// Pixels shaved off every skinned element on all four sides. Vanilla draws
        /// buttons edge to edge, so 1px is enough to read as a gap between them
        /// without moving any content or changing a single layout number.
        /// </summary>
        public float inset = 1f;

        /// <summary>Empty means the game's own fonts.</summary>
        /// <summary>Family name as the AssetBundle reports it.</summary>
        public const string DefaultFont = "Bungee";

        /// <summary>
        /// Ships in Fonts/. Falls back to the vanilla font when it is not installed,
        /// so this default is safe even if the bundle ever fails to load.
        /// </summary>
        public string fontName = DefaultFont;

        public int fontOffsetTiny;
        public int fontOffsetSmall;
        public int fontOffsetMedium;

        /// <summary>Black outline behind every label. On by default.</summary>
        public bool textOutline = true;

        /// <summary>Outline offset in pixels. 1 is a hairline, 2 reads as ink.</summary>
        public float outlineThickness = 2f;

        /// <summary>
        /// Multiplies the outline alpha. The label's own alpha is still inherited on
        /// top of this, so faded text keeps a faded outline rather than a hard ghost.
        /// </summary>
        public float outlineOpacity = 0.7f;

        /// <summary>
        /// Outline the smallest font too. Off by default: at ~10px the ring closes the
        /// counters of a, e and g and the word smudges.
        /// </summary>
        public bool outlineTinyText;

        /// <summary>Offer every installed font instead of the curated shortlist.</summary>
        public bool showAllFonts;

        /// <summary>Pattern overlay on window and panel interiors.</summary>
        public bool texturedBackground = true;

        /// <summary>Which pattern file to use, without the "Pattern_" prefix.</summary>
        public string backgroundPattern = "Hatch";

        /// <summary>Opacity of that overlay.</summary>
        public float backgroundGrain = 0.05f;

        /// <summary>Nearest-neighbour sampling for the skin textures instead of bilinear.</summary>
        public bool pointFilter;

        /// <summary>Tile the background pattern across button and tooltip faces too.</summary>
        public bool grainOnButtons = true;

        /// <summary>Opacity of dialog windows. 1 = solid.</summary>
        public float windowOpacity = 1f;

        /// <summary>Scale-and-fade when a dialog opens.</summary>
        public bool windowAnimation = true;

        /// <summary>
        /// How long that animation lasts, in real seconds. Short on purpose: this
        /// plays every time any dialog opens, and what feels elegant on the first
        /// open feels like lag on the fiftieth.
        /// </summary>
        public float animationDuration = 0.35f;

        /// <summary>Animate the main panels: Architect, Work, Schedule and the rest.</summary>
        public bool animateMainTabs = true;

        /// <summary>Animate immediate windows, e.g. the inspect pane.</summary>
        public bool animateImmediate = true;

        /// <summary>Animate anything on a layer the three toggles above do not cover.</summary>
        public bool animateOtherLayers = true;

        // Which surfaces the mod is allowed to touch.
        //
        // A reskin this broad has exactly one recurring compatibility complaint -
        // "it fights with mod X", and X is almost always a single surface. Being
        // able to hand back the scrollbars, or the gizmos, without giving up the
        // whole skin turns that from an uninstall into a checkbox.

        /// <summary>Buttons, subtle buttons, tooltips, float menu rows, slider rail.</summary>
        public bool skinButtons = true;

        /// <summary>Window frames and panel sections.</summary>
        public bool skinWindows = true;

        /// <summary>Rounded tabs.</summary>
        public bool skinTabs = true;

        /// <summary>Checkboxes, radios, slider knob, gizmos, the colonist bar, bar fills.</summary>
        public bool skinWidgets = true;

        /// <summary>Scrollbar track and thumb.</summary>
        public bool skinScrollbars = true;

        /// <summary>Colour each architect category button by what the category is for.</summary>
        public bool architectColors = true;

        /// <summary>Opacity of the colour painted over the plate. 0 is vanilla.</summary>
        public float architectPlateAlpha = 1f;

        /// <summary>Tint the label too. Off by default: same hue on hue loses contrast.</summary>
        public bool architectColorLabels;

        /// <summary>Give categories with no palette entry a stable generated hue.</summary>
        public bool architectAutoColor = true;

        /// <summary>Shape of the colour behind a category button: Plate, Bar, Frame or Flat.</summary>
        public string architectPlateStyle = "Plate";

        /// <summary>Draw this mod own category icons instead of leaving the button text-only.</summary>
        public bool architectIcons = true;

        /// <summary>Widen the Architect menu until the longest category name fits.</summary>
        public bool architectAutoWidth = true;

        /// <summary>Black outline around the badge shapes.</summary>
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
            Scribe_Values.Look(ref windowOpacity, "windowOpacity", 1f);
            Scribe_Values.Look(ref windowAnimation, "windowAnimation", defaultValue: true);
            Scribe_Values.Look(ref animationDuration, "animationDuration", 0.35f);
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

            // A saved style that no longer exists draws nothing, and draws nothing
            // SILENTLY, which is the worst way for a setting to break. Renames get
            // mapped; anything else unrecognised falls back rather than vanishing.
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
            }
            base.ExposeData();
        }
    }

    public class LizarbInterfaceMod : Mod
    {
        public static LizarbInterfaceSettings Settings { get; private set; }

        /// <summary>Mod folder on disk, so the atlases can be read straight from the PNGs.</summary>
        public static string RootDir { get; private set; }

        /// <summary>Kept for FontBundle, which needs the mod's loaded AssetBundles.</summary>
        public static ModContentPack Pack { get; private set; }


        /// <summary>
        /// Must match the folders under Skins/. Each theme names the pattern it was
        /// designed around and applies it on selection; the pattern stays a separate
        /// setting afterwards.
        /// </summary>
        private static readonly (string Id, string Pattern, Color Outline)[] Themes =
        {
            // Outline ink comes from the theme: pure black fought the warm skins.
            // Always very dark, or it stops reading as an outline.
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
            ("Grimoire", "Medieval",  new Color(0.06f, 0.03f, 0.03f)),
            ("Foundry",  "Bricks",    new Color(0.05f, 0.04f, 0.04f)),
        };

        /// <summary>Outline colour of the active theme, dark grey as a fallback.</summary>
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

        /// <summary>Must match the Pattern_*.png files the generator writes.</summary>
        private static readonly string[] Patterns =
        {
            "Hatch", "Medieval", "Scales", "Bricks", "Dots", "Chevron", "Woodgrain",
        };

        public LizarbInterfaceMod(ModContentPack content) : base(content)
        {
            RootDir = content.RootDir;
            Pack = content;
            Settings = GetSettings<LizarbInterfaceSettings>();
            new Harmony("lizarb.interface").PatchAll(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// Shrinks a rect before drawing skinned chrome, which is what produces the gap
        /// between adjacent buttons. Nothing in the layout moves: the game computes the
        /// same rects and places text identically, we just paint inside them. Skipped on
        /// rects too small to survive it.
        /// </summary>
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

        /// <summary>
        /// Content height measured on the previous frame: a scroll view needs its view
        /// rect declared before the content is laid out.
        ///
        /// maxOneColumn on the Listing is load-bearing. Listing_Standard wraps into a
        /// second column past the rect height, and CurHeight is the CURRENT column's
        /// cursor, so a wrap collapses this number, shrinks the view, and wraps even
        /// sooner next frame. Self-reinforcing, never recovers.
        /// </summary>
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

        /// <summary>
        /// Static so the page reopens where it was left. Settings windows get opened,
        /// closed and reopened constantly while tuning a look, and losing the tab every
        /// time is the kind of small friction that makes a page feel hostile.
        /// </summary>
        private static Tab tab = Tab.Theme;

        /// <summary>One scroll position per tab, for the same reason.</summary>
        private readonly Vector2[] scrolls = new Vector2[6];

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // The master switch lives ABOVE the tabs because it governs all of them.
            // Filing it under any one tab would imply it only affects that tab.
            var header = new Rect(inRect.x, inRect.y, inRect.width, 24f);
            Widgets.CheckboxLabeled(header, "LizarbInterface.Enabled".Translate(), ref Settings.enabled);

            // TabDrawer draws the tabs ABOVE the rect it is given, so the body has to
            // start a full tab height down or they would be drawn off the window.
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

            // maxOneColumn is load-bearing: without it Listing_Standard wraps into a
            // second column once the content passes the rect height, CurHeight (which
            // is the cursor of the CURRENT column) collapses, and the height fed back
            // below poisons itself into a permanently broken page.
            var listing = new Listing_Standard { maxOneColumn = true };
            listing.Begin(view);

            // Each tab body is isolated. A settings page is the one place where a
            // single broken widget must not take the rest down with it: an exception
            // halfway through a Listing_Standard silently blanks everything below it,
            // which reads as "the mod broke" rather than "one section failed".
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

            // After the layout, never inside it. See QueueFontApply.
            if (fontDirty)
            {
                fontDirty = false;
                FontEngine.Apply();

                // The architect width is measured in the current font, so it has to
                // be remeasured whenever that font changes.
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

        /// <summary>Names of sections that have already reported a failure.</summary>
        private static readonly HashSet<string> reportedSections = new HashSet<string>();

        private static void Section(Listing_Standard listing, string name, Action<Listing_Standard> body)
        {
            try
            {
                body(listing);
            }
            catch (Exception e)
            {
                // Logged once per section: an exception here would repeat every frame.
                if (reportedSections.Add(name))
                {
                    Log.Error("[LizarbInterface] settings section '" + name + "' failed: " + e);
                }

                listing.Label("LizarbInterface.SectionFailed".Translate(name));
            }
        }

        /// <summary>
        /// Theme picker drawn as swatches rather than a dropdown: each option is
        /// painted with its OWN skin, so the choice is made by looking instead of by
        /// reading a name. Since a theme changes corner radius and fillet weight as
        /// well as colour, a text list hides most of what is different.
        /// </summary>
        private void DoThemeSection(Listing_Standard listing)
        {
            listing.Label("LizarbInterface.ThemeSection".Translate());

            // Two rows of four. One row of eight would give each swatch about 130px,
            // too narrow to show the corner treatment that distinguishes them.
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
                // A button inside the frame shows the pairing that actually matters:
                // how the two read against each other in this theme.
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

                // A skin is ornament + geometry + backdrop, so the theme brings its
                // pattern along. It stays a separate setting afterwards, so anyone who
                // wants Obsidian with scales can still pick that.
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

        /// <summary>
        /// Marks the font for reapplying once this frame's layout is finished.
        ///
        /// Applying inline is what makes the size sliders unusable. FontEngine.Apply
        /// changes the shared GUIStyles and Text.lineHeights, so every widget the
        /// Listing places after the slider suddenly measures differently: the rows
        /// below shift, the IMGUI control ids shift with them, and the value lands a
        /// drag behind.
        ///
        /// LongEventHandler.ExecuteWhenFinished does NOT work for this. It runs the
        /// action immediately when no long event is in progress, which is always the
        /// case in the settings window, so it defers nothing at all.
        /// </summary>
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

        /// <summary>
        /// Everything about the painted surface itself: how far in it is painted, and
        /// what is laid over the fill.
        /// </summary>
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
            listing.Label("LizarbInterface.WindowOpacity".Translate(Settings.windowOpacity.ToStringPercent()));
            Settings.windowOpacity = listing.Slider(Settings.windowOpacity, 0.5f, 1f);
            listing.Label("LizarbInterface.WindowOpacity.Tip".Translate());

            listing.GapLine();

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

        /// <summary>
        /// The shape picker, as a grid of real architect buttons rather than a list
        /// of names. The shapes differ by geometry, so words hide most of the choice,
        /// and twelve of them down a column pushed everything else off the page.
        /// </summary>
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
        /// <summary>
        /// <summary>
        /// Calls the same draw the game uses, so the two cannot drift.
        /// </summary>
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

            // The same call the real button makes. Sizing the icon here by hand is
            // what put it a pixel off the shape: the button uses
            // min(26, max(12, h - 10)) and this used min(24, h - 8), so at a row
            // height of 32 one said 22 and the other 24.
            ArchitectIcons.Draw(rect, "Production");

            // The label uses the SAME rule the real button does, so ticking "colour
            // the button text" shows its effect here before it is applied anywhere.
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
        /// <summary>
        /// Architect Icons draws its own icon in the same place ours goes, so the
        /// settings page says so rather than letting the player find two overlapping.
        /// </summary>
        private static bool ArchitectIconsModPresent =>
            ModLister.GetActiveModWithIdentifier("com.bymarcin.architecticons", ignorePostfix: true) != null;

        /// <summary>
        /// Per-surface opt-out. A reskin this broad collects one kind of complaint -
        /// "it fights with mod X", and X is nearly always a single surface.
        /// </summary>
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

        /// <summary>
        /// Back to defaults. Assigns a fresh settings object field by field rather than
        /// replacing the reference, because Mod.GetSettings handed out that instance and
        /// every patch holds it.
        /// </summary>
        private void ResetAll()
        {
            var fresh = new LizarbInterfaceSettings();
            foreach (FieldInfo field in typeof(LizarbInterfaceSettings)
                     .GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                field.SetValue(Settings, field.GetValue(fresh));
            }

            // The font is applied to shared GUIStyles, so restoring the value is not
            // enough. It has to be pushed back into Verse.Text, and after the layout
            // rather than inside it.
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

    /// <summary>
    /// Starting or loading a game runs Resources.UnloadUnusedAssets, which can destroy
    /// the dynamic fonts we created. Re-applying afterwards is cheaper than trying to
    /// out-argue Unity's collector. Textures heal themselves on next use; the font
    /// styles have to be pointed at a live Font again.
    /// </summary>
    [HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.UnloadUnusedUnityAssets))]
    internal static class Patch_UnloadUnusedUnityAssets
    {
        private static void Postfix()
        {
            // Their unload is queued through LongEventHandler, so queueing ours right
            // after guarantees we run once it has actually happened.
            LongEventHandler.ExecuteWhenFinished(FontEngine.Apply);
        }
    }

    /// <summary>Applies the saved font once mod content and Verse.Text are both up.</summary>
    [StaticConstructorOnStartup]
    internal static class FontEngineInit
    {
        static FontEngineInit()
        {
            FontEngine.Apply();
        }
    }
}

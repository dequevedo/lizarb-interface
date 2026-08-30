using System;
using System.IO;
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

        public const string DefaultTheme = "Orcish";

        public const string DefaultPattern = "Hatch";

        public const float DefaultGrain = 0.15f;

        public string theme = DefaultTheme;

        public float inset = 1f;

        public const string DefaultFont = "Rajdhani";

        public const string DefaultPlateStyle = "Gradient";

        public string fontName = DefaultFont;

        public int fontOffsetTiny;
        public int fontOffsetSmall;
        public int fontOffsetMedium;

        public bool textOutline = true;

        public float outlineThickness = 2f;

        public float outlineOpacity = 0.7f;

        public bool outlineTinyText = true;

        public bool showAllFonts;

        public bool texturedBackground = true;

        public string backgroundPattern = DefaultPattern;

        public float backgroundGrain = DefaultGrain;

        public bool pointFilter;

        public bool grainOnButtons = true;

        public bool plateIconButtons = true;

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

        public string architectPlateStyle = DefaultPlateStyle;

        public bool ownIcons = true;

        public bool architectAutoWidth = true;

        public bool architectShapeOutline = true;

        public string preset = DefaultTheme;

        public List<Preset> presets = new List<Preset>();

        public bool architectSpacing = true;

        public float architectPadding = 4f;

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
            Scribe_Values.Look(ref theme, "theme", DefaultTheme);
            Scribe_Values.Look(ref inset, "inset", 1f);
            Scribe_Values.Look(ref fontName, "fontName", DefaultFont);
            Scribe_Values.Look(ref fontOffsetTiny, "fontOffsetTiny", 0);
            Scribe_Values.Look(ref fontOffsetSmall, "fontOffsetSmall", 0);
            Scribe_Values.Look(ref fontOffsetMedium, "fontOffsetMedium", 0);
            Scribe_Values.Look(ref textOutline, "textOutline", defaultValue: true);
            Scribe_Values.Look(ref outlineThickness, "outlineThickness", 2f);
            Scribe_Values.Look(ref outlineOpacity, "outlineOpacity", 0.7f);
            Scribe_Values.Look(ref outlineTinyText, "outlineTinyText", defaultValue: true);
            Scribe_Values.Look(ref showAllFonts, "showAllFonts", defaultValue: false);
            Scribe_Values.Look(ref texturedBackground, "texturedBackground", defaultValue: true);
            Scribe_Values.Look(ref backgroundPattern, "backgroundPattern", DefaultPattern);
            Scribe_Values.Look(ref backgroundGrain, "backgroundGrain", DefaultGrain);
            Scribe_Values.Look(ref pointFilter, "pointFilter", defaultValue: false);
            Scribe_Values.Look(ref grainOnButtons, "grainOnButtons", defaultValue: true);
            Scribe_Values.Look(ref plateIconButtons, "plateIconButtons", defaultValue: true);
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
            Scribe_Values.Look(ref architectPlateStyle, "architectPlateStyle", DefaultPlateStyle);
            Scribe_Values.Look(ref ownIcons, "architectIcons", defaultValue: true);
            Scribe_Values.Look(ref architectAutoWidth, "architectAutoWidth", defaultValue: true);
            Scribe_Values.Look(ref architectShapeOutline, "architectShapeOutline", defaultValue: true);
            Scribe_Values.Look(ref architectSpacing, "architectSpacing", defaultValue: true);
            Scribe_Values.Look(ref architectPadding, "architectPadding", 4f);
            Scribe_Values.Look(ref preset, "preset", null);
            Scribe_Collections.Look(ref presets, "presets", LookMode.Deep);

            if (presets == null)
            {
                presets = new List<Preset>();
            }

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (architectPlateStyle == "Bar")
                {
                    architectPlateStyle = "Square";
                }

                if (architectPlateStyle == "Cascade")
                {
                    architectPlateStyle = DefaultPlateStyle;
                }

                if (System.Array.IndexOf(ArchitectPlate.Styles, architectPlateStyle) < 0)
                {
                    architectPlateStyle = DefaultPlateStyle;
                }

                if (System.Array.IndexOf(WindowAnimation.Styles, windowAnimationStyle) < 0)
                {
                    windowAnimationStyle = "Slide";
                }

                if (System.Array.IndexOf(LizarbInterfaceMod.Patterns, backgroundPattern) < 0)
                {
                    backgroundPattern = DefaultPattern;
                }

                if (!LizarbInterfaceMod.HasTheme(theme))
                {
                    theme = DefaultTheme;
                }

                if (preset == null)
                {
                    preset = theme;
                }
            }
            base.ExposeData();
        }
    }

    internal sealed class ThemeInfo
    {
        internal readonly string Id;
        internal readonly string Pattern;
        internal readonly Color Outline;
        internal readonly string Group;

        internal float Grain;
        internal float Scale;
        internal bool Tile;
        internal bool? Background;
        internal bool? PointFilter;

        internal ThemeInfo(string id, string pattern, Color outline, string group)
        {
            Id = id;
            Pattern = pattern;
            Outline = outline;
            Group = group;
        }
    }

    public class LizarbInterfaceMod : Mod
    {
        public static LizarbInterfaceSettings Settings { get; private set; }

        public static string RootDir { get; private set; }

        public static ModContentPack Pack { get; private set; }

        internal static readonly ThemeInfo[] Themes =
        {
            new ThemeInfo("Slate",    "Bricks",    new Color(0.04f, 0.05f, 0.05f), "Squared"),
            new ThemeInfo("Wood",     "Woodgrain", new Color(0.09f, 0.06f, 0.03f), "Squared"),
            new ThemeInfo("Rivet",    "Hatch",     new Color(0.06f, 0.05f, 0.04f), "Squared"),
            new ThemeInfo("Vellum",   "Medieval",  new Color(0.08f, 0.06f, 0.04f), "Squared"),
            new ThemeInfo("Grimoire", "Hatch",     new Color(0.06f, 0.03f, 0.03f), "Squared"),
            new ThemeInfo("Bulwark",  "Chevron",   new Color(0.04f, 0.05f, 0.03f), "Squared"),
            new ThemeInfo("Iron",     "Bricks",    new Color(0.05f, 0.06f, 0.07f), "Squared"),
            new ThemeInfo("Gothic",   "Medieval",  new Color(0.03f, 0.03f, 0.03f), "Squared"),
            new ThemeInfo("Foundry",  "Bricks",    new Color(0.05f, 0.04f, 0.04f), "Squared"),
            new ThemeInfo("Obsidian", "Chevron",   new Color(0.03f, 0.03f, 0.04f), "Squared"),

            new ThemeInfo("Aero",     "Dots",      new Color(0.03f, 0.06f, 0.09f), "Rounded"),
            new ThemeInfo("Ash",      "Dots",      new Color(0.05f, 0.05f, 0.05f), "Rounded"),
            new ThemeInfo("Crimson",  "Scales",    new Color(0.11f, 0.04f, 0.04f), "Rounded"),
            new ThemeInfo("Verdant",  "Scales",    new Color(0.04f, 0.08f, 0.05f), "Rounded"),
            new ThemeInfo("Copper",   "Scales",    new Color(0.04f, 0.06f, 0.06f), "Rounded"),
            new ThemeInfo("Brass",    "Hatch",     new Color(0.10f, 0.07f, 0.04f), "Rounded"),
            new ThemeInfo("Arcane",   "Dots",      new Color(0.04f, 0.03f, 0.10f), "Rounded"),
            new ThemeInfo("Royal",    "Medieval",  new Color(0.06f, 0.05f, 0.11f), "Rounded"),
            new ThemeInfo("Bone",     "Dots",      new Color(0.09f, 0.08f, 0.05f), "Rounded"),
            new ThemeInfo("Flesh",    "Hatch",     new Color(0.10f, 0.04f, 0.04f), "Rounded"),

            new ThemeInfo("DebugSlices", "Hatch",  new Color(0.10f, 0.02f, 0.08f), "Development"),
            new ThemeInfo("DebugCoarse", "Hatch",  new Color(0.10f, 0.02f, 0.08f), "Development"),
            new ThemeInfo("DebugSparse", "Hatch",  new Color(0.10f, 0.02f, 0.08f), "Development"),
        };

        private static List<ThemeInfo> all;

        internal static List<ThemeInfo> AllThemes
        {
            get
            {
                if (all == null)
                {
                    all = new List<ThemeInfo>(Themes);
                    Discover();
                }

                return all;
            }
        }

        private static void Discover()
        {
            if (RootDir.NullOrEmpty())
            {
                return;
            }

            string skins = Path.Combine(RootDir, "Skins");
            if (!Directory.Exists(skins))
            {
                return;
            }

            foreach (string dir in Directory.GetDirectories(skins))
            {
                string id = Path.GetFileName(dir);
                if (id == "Shared" || IsBuiltIn(id))
                {
                    continue;
                }

                if (Directory.GetFiles(dir, "*.png").Length == 0)
                {
                    continue;
                }

                string group = id.StartsWith("Debug") ? "Development" : "Handpainted";
                all.Add(Describe(id, group, dir));
            }
        }

        private static ThemeInfo Describe(string id, string group, string dir)
        {
            string pattern = LizarbInterfaceSettings.DefaultPattern;
            Color outline = new Color(0.05f, 0.05f, 0.05f);
            var read = new Dictionary<string, string>();

            string file = Path.Combine(dir, "theme.txt");
            if (File.Exists(file))
            {
                foreach (string line in File.ReadAllLines(file))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        read[parts[0].Trim().ToLowerInvariant()] = parts[1].Trim();
                    }
                }
            }

            if (read.TryGetValue("pattern", out string named) && System.Array.IndexOf(Patterns, named) >= 0)
            {
                pattern = named;
            }

            if (read.TryGetValue("outline", out string tint) && ParseColor(tint, out Color c))
            {
                outline = c;
            }

            var info = new ThemeInfo(id, pattern, outline, group)
            {
                Grain = Mathf.Clamp01(Number(read, "grain", 0f)),
                Scale = Number(read, "scale", 0f),
                Tile = Flag(read, "tile") == true,
                Background = Flag(read, "background"),
                PointFilter = Flag(read, "pointfilter"),
            };

            if (info.Scale > 0f)
            {
                info.Scale = Mathf.Clamp(info.Scale, 0.125f, 8f);
            }

            return info;
        }

        private static float Number(Dictionary<string, string> read, string key, float fallback)
        {
            if (read.TryGetValue(key, out string text) &&
                float.TryParse(text, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out float value))
            {
                return value;
            }

            return fallback;
        }

        private static bool? Flag(Dictionary<string, string> read, string key)
        {
            if (!read.TryGetValue(key, out string text))
            {
                return null;
            }

            text = text.ToLowerInvariant();
            if (text == "true" || text == "on" || text == "yes" || text == "1")
            {
                return true;
            }

            if (text == "false" || text == "off" || text == "no" || text == "0")
            {
                return false;
            }

            return null;
        }

        private static bool ParseColor(string text, out Color colour)
        {
            colour = Color.black;
            string[] parts = text.Split(',');
            if (parts.Length != 3)
            {
                return false;
            }

            var v = new float[3];
            for (int i = 0; i < 3; i++)
            {
                if (!float.TryParse(parts[i].Trim(), System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out v[i]))
                {
                    return false;
                }
            }

            colour = new Color(v[0], v[1], v[2]);
            return true;
        }

        internal static ThemeInfo Info(string id)
        {
            if (id.NullOrEmpty())
            {
                return null;
            }

            foreach (var entry in AllThemes)
            {
                if (entry.Id == id)
                {
                    return entry;
                }
            }

            return null;
        }

        internal static void Rediscover()
        {
            all = null;
        }

        private static bool IsBuiltIn(string id)
        {
            foreach (var entry in Themes)
            {
                if (entry.Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasTheme(string id)
        {
            foreach (var entry in AllThemes)
            {
                if (entry.Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        public static Color OutlineColor
        {
            get
            {
                string id = Settings?.theme;
                foreach (var t in AllThemes)
                {
                    if (t.Id == id)
                    {
                        return t.Outline;
                    }
                }

                return new Color(0.06f, 0.06f, 0.06f);
            }
        }

        internal static readonly string[] Patterns =
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

        private enum Tab
        {
            Presets,
            Icons,
            Architect,
            Windows,
            Compatibility,
        }

        private static readonly Tab[] TabOrder =
        {
            Tab.Presets, Tab.Icons, Tab.Architect, Tab.Windows, Tab.Compatibility,
        };

        private static Tab tab = Tab.Presets;

        private readonly float[] contentHeight = { 600f, 600f, 600f, 600f, 600f, 600f };

        private readonly Vector2[] scrolls = new Vector2[6];

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var body = new Rect(
                inRect.x,
                inRect.y + TabDrawer.TabHeight,
                inRect.width,
                inRect.height - TabDrawer.TabHeight);

            Widgets.DrawMenuSection(body);

            var tabs = new List<TabRecord>();
            foreach (Tab which in TabOrder)
            {
                tabs.Add(MakeTab(which));
            }

            TabDrawer.DrawTabs(body, tabs);

            Rect inner = body.ContractedBy(14f);

            if (tab == Tab.Presets)
            {
                DoThemeLayout(inner);
                FlushFont();
                return;
            }

            int index = (int)tab;

            var view = new Rect(0f, 0f, inner.width - 24f, contentHeight[index]);
            Widgets.BeginScrollView(inner, ref scrolls[index], view);

            var listing = new Listing_Standard { maxOneColumn = true };
            listing.Begin(view);

            switch (tab)
            {
                case Tab.Icons:
                    Section(listing, "icons", DoIconsTab);
                    break;
                case Tab.Architect:
                    Section(listing, "architect", DoArchitectTab);
                    break;
                case Tab.Windows:
                    Section(listing, "windows", DoWindowsTab);
                    break;
                case Tab.Compatibility:
                    Section(listing, "compatibility", DoCompatibilityTab);
                    break;
            }

            contentHeight[index] = Mathf.Max(listing.CurHeight + 24f, inner.height);

            listing.End();
            Widgets.EndScrollView();

            FlushFont();
        }

        private static void FlushFont()
        {
            if (!fontDirty)
            {
                return;
            }

            fontDirty = false;
            FontEngine.Apply();
            ArchitectWidth.Invalidate();
        }

        private Vector2 themeScroll;

        private Vector2 optionScroll;

        private float themeHeight = 600f;

        private float optionHeight = 400f;

        private void DoThemeLayout(Rect area)
        {
            float left = Mathf.Round(area.width * 0.52f);
            var listRect = new Rect(area.x, area.y, left, area.height);
            var sideRect = new Rect(area.x + left + 18f, area.y, area.width - left - 18f, area.height);

            Color previous = GUI.color;
            GUI.color = RuleColor;
            Widgets.DrawLineVertical(area.x + left + 8f, area.y, area.height);
            GUI.color = previous;

            var view = new Rect(0f, 0f, listRect.width - 20f, themeHeight);
            Widgets.BeginScrollView(listRect, ref themeScroll, view);

            var themes = new Listing_Standard { maxOneColumn = true };
            themes.Begin(view);
            Section(themes, "presets", DoPresetGrid);
            themeHeight = Mathf.Max(themes.CurHeight + 12f, listRect.height);
            themes.End();

            Widgets.EndScrollView();

            var sideView = new Rect(0f, 0f, sideRect.width - 20f, optionHeight);
            Widgets.BeginScrollView(sideRect, ref optionScroll, sideView);

            var options = new Listing_Standard { maxOneColumn = true };
            options.Begin(sideView);
            Section(options, "preset options", DoPresetOptions);
            optionHeight = Mathf.Max(options.CurHeight + 12f, sideRect.height);
            options.End();

            Widgets.EndScrollView();
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

        private const float RowHeight = 30f;

        private static readonly Color DimText = new Color(0.72f, 0.70f, 0.67f);

        private static readonly Color RuleColor = new Color(1f, 1f, 1f, 0.22f);

        private static Rect NextRow(Listing_Standard listing, out Rect label, out Rect control)
        {
            Rect row = listing.GetRect(RowHeight);
            listing.Gap(2f);

            float wide = Mathf.Clamp(row.width * 0.44f, 140f, 300f);
            control = new Rect(row.xMax - wide, row.y, wide, row.height);
            label = new Rect(row.x + 6f, row.y, row.width - wide - 14f, row.height);
            return row;
        }

        private static void Write(Rect rect, string text, TextAnchor anchor)
        {
            TextAnchor previous = Text.Anchor;
            Text.Anchor = anchor;
            Widgets.Label(rect, text);
            Text.Anchor = previous;
        }

        private static void Tip(Rect rect, string key)
        {
            string tip = key + ".Tip";
            if (tip.CanTranslate())
            {
                TooltipHandler.TipRegion(rect, tip.Translate());
            }
        }

        private static void Head(Listing_Standard listing, string key)
        {
            listing.Gap(12f);

            Rect rect = listing.GetRect(24f);
            Write(rect, key.Translate(), TextAnchor.MiddleLeft);

            Color previous = GUI.color;
            GUI.color = RuleColor;
            Widgets.DrawLineHorizontal(rect.x, rect.yMax + 2f, rect.width);
            GUI.color = previous;

            listing.Gap(8f);
        }

        private static void Note(Listing_Standard listing, string text, Color colour)
        {
            Color previous = GUI.color;
            GUI.color = colour;
            Text.Font = GameFont.Tiny;
            listing.Label(text);
            Text.Font = GameFont.Small;
            GUI.color = previous;
            listing.Gap(6f);
        }

        private static bool Toggle(Listing_Standard listing, string key, bool value, bool enabled = true)
        {
            Rect row = NextRow(listing, out Rect label, out Rect control);

            if (enabled && Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
            }

            Color previous = GUI.color;
            if (!enabled)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
            }

            Write(label, key.Translate(), TextAnchor.MiddleLeft);
            Widgets.CheckboxDraw(control.xMax - 26f, control.y + 3f, value, !enabled);
            GUI.color = previous;

            Tip(row, key);

            if (enabled && Widgets.ButtonInvisible(row))
            {
                value = !value;
                (value ? RimWorld.SoundDefOf.Checkbox_TurnedOn : RimWorld.SoundDefOf.Checkbox_TurnedOff).PlayOneShotOnCamera();
            }

            return value;
        }

        private static float Slide(Listing_Standard listing, string key, float value,
                                   float min, float max, Func<float, string> format,
                                   float roundTo = -1f, bool enabled = true)
        {
            Rect row = NextRow(listing, out Rect label, out Rect control);

            if (enabled && Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
            }

            Color previous = GUI.color;
            if (!enabled)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
            }

            Write(label, key.Translate(), TextAnchor.MiddleLeft);

            const float ReadoutWidth = 58f;
            var bar = new Rect(control.x, control.y + 9f, control.width - ReadoutWidth, 12f);

            bool was = GUI.enabled;
            GUI.enabled = enabled;
            float next = Widgets.HorizontalSlider(bar, value, min, max, false, null, null, null, roundTo);
            GUI.enabled = was;

            if (!enabled)
            {
                next = value;
            }

            Write(new Rect(control.xMax - ReadoutWidth + 8f, control.y, ReadoutWidth - 8f, control.height),
                  format(next), TextAnchor.MiddleRight);

            GUI.color = previous;
            Tip(row, key);
            return next;
        }

        private static void Pick(Listing_Standard listing, string key, string current, Action onClick,
                                 bool enabled = true)
        {
            Rect row = NextRow(listing, out Rect label, out Rect control);

            Color previous = GUI.color;
            if (!enabled)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
            }

            Write(label, key.Translate(), TextAnchor.MiddleLeft);

            var button = new Rect(control.x, control.y + 2f, control.width, control.height - 4f);
            bool clicked = Widgets.ButtonText(button, current, active: enabled);

            GUI.color = previous;
            Tip(row, key);

            if (clicked && enabled)
            {
                onClick();
            }
        }

        private static string Percent(float v) => v.ToStringPercent();

        private static string Pixels(float v) => Mathf.RoundToInt(v) + " px";

        private static string Millis(float v) => Mathf.RoundToInt(v * 1000f) + " ms";

        private static string Signed(float v)
        {
            int i = Mathf.RoundToInt(v);
            return i > 0 ? "+" + i : i.ToString();
        }

        private void DoPresetOptions(Listing_Standard listing)
        {
            Preset current = Presets.Capture();
            bool builtIn = Presets.IsBuiltIn(Settings.preset);

            Head(listing, "LizarbInterface.PresetSection");

            Rect row = listing.GetRect(RowHeight);
            listing.Gap(2f);

            float third = (row.width - 12f) / 3f;
            if (Widgets.ButtonText(new Rect(row.x, row.y + 2f, third, row.height - 4f),
                                   "LizarbInterface.PresetSaveAs".Translate()))
            {
                SavePreset(current);
            }

            bool canUpdate = !builtIn && FindUserPreset(Settings.preset) != null;
            if (Widgets.ButtonText(new Rect(row.x + third + 6f, row.y + 2f, third, row.height - 4f),
                                   "LizarbInterface.PresetUpdate".Translate(), active: canUpdate))
            {
                Preset saved = FindUserPreset(Settings.preset);
                if (saved != null)
                {
                    string keep = saved.name;
                    Settings.presets[Settings.presets.IndexOf(saved)] = current;
                    current.name = keep;
                    RimWorld.SoundDefOf.Click.PlayOneShotOnCamera();
                }
            }

            if (Widgets.ButtonText(new Rect(row.x + third * 2f + 12f, row.y + 2f, third, row.height - 4f),
                                   "LizarbInterface.PresetDelete".Translate(), active: canUpdate))
            {
                Preset saved = FindUserPreset(Settings.preset);
                if (saved != null)
                {
                    Settings.presets.Remove(saved);
                    Settings.preset = "";
                    RimWorld.SoundDefOf.Click.PlayOneShotOnCamera();
                }
            }

            Head(listing, "LizarbInterface.FontSection");

            string face = Settings.fontName.NullOrEmpty()
                ? "LizarbInterface.FontVanilla".Translate().ToString()
                : Settings.fontName;

            Pick(listing, "LizarbInterface.FontFace", face, OpenFontPicker);
            Settings.showAllFonts = Toggle(listing, "LizarbInterface.ShowAllFonts", Settings.showAllFonts);

            Settings.fontOffsetTiny = SizeSlide(listing, "LizarbInterface.FontSize.Tiny", Settings.fontOffsetTiny);
            Settings.fontOffsetSmall = SizeSlide(listing, "LizarbInterface.FontSize.Small", Settings.fontOffsetSmall);
            Settings.fontOffsetMedium = SizeSlide(listing, "LizarbInterface.FontSize.Medium", Settings.fontOffsetMedium);

            Head(listing, "LizarbInterface.OutlineSection");

            Settings.textOutline = Toggle(listing, "LizarbInterface.TextOutline", Settings.textOutline);

            bool outline = Settings.textOutline;

            Settings.outlineThickness = Slide(listing, "LizarbInterface.OutlineThickness",
                                              Settings.outlineThickness, 1f, 2f, Pixels, 1f, outline);

            Settings.outlineOpacity = Slide(listing, "LizarbInterface.OutlineOpacity",
                                            Settings.outlineOpacity, 0f, 1f, Percent, -1f, outline);

            Settings.outlineTinyText = Toggle(listing, "LizarbInterface.OutlineTiny",
                                              Settings.outlineTinyText, outline);

            Head(listing, "LizarbInterface.BackgroundSection");

            Settings.texturedBackground = Toggle(listing,
                "LizarbInterface.TexturedBackground", Settings.texturedBackground);

            bool grain = Settings.texturedBackground;

            Pick(listing, "LizarbInterface.BackgroundPattern",
                 ("LizarbInterface.Pattern." + Settings.backgroundPattern).Translate(),
                 OpenPatternMenu, grain);

            Settings.backgroundGrain = Slide(listing, "LizarbInterface.BackgroundGrain",
                                             Settings.backgroundGrain, 0f, 1f, Percent, -1f, grain);

            Settings.grainOnButtons = Toggle(listing,
                "LizarbInterface.GrainOnButtons", Settings.grainOnButtons, grain);

            Head(listing, "LizarbInterface.DrawingSection");

            Settings.inset = Slide(listing, "LizarbInterface.Inset", Settings.inset, 0f, 4f, Pixels, 1f);
            Settings.pointFilter = Toggle(listing, "LizarbInterface.PointFilter", Settings.pointFilter);
            Head(listing, "LizarbInterface.Architect.ColourSection");

            Settings.architectColors = Toggle(listing,
                "LizarbInterface.Architect.Enabled", Settings.architectColors);

            bool colours = Settings.architectColors;

            Settings.architectPlateAlpha = Slide(listing, "LizarbInterface.Architect.PlateAlpha",
                                                 Settings.architectPlateAlpha, 0f, 1f, Percent, -1f, colours);

            Settings.architectShapeOutline = Toggle(listing,
                "LizarbInterface.Architect.ShapeOutline", Settings.architectShapeOutline, colours);

            Settings.architectColorLabels = Toggle(listing,
                "LizarbInterface.Architect.ColorLabels", Settings.architectColorLabels, colours);

            Settings.architectAutoColor = Toggle(listing,
                "LizarbInterface.Architect.AutoColor", Settings.architectAutoColor, colours);

            Head(listing, "LizarbInterface.Architect.PlateStyle");
            DoPlateStyleGrid(listing, colours);
        }

        private Preset FindUserPreset(string name)
        {
            if (Settings.presets == null || name.NullOrEmpty())
            {
                return null;
            }

            foreach (Preset p in Settings.presets)
            {
                if (p.name == name)
                {
                    return p;
                }
            }

            return null;
        }

        private void SavePreset(Preset captured)
        {
            string seed = Settings.preset.NullOrEmpty()
                ? "LizarbInterface.PresetNewName".Translate().ToString()
                : Settings.preset;

            Find.WindowStack.Add(new Dialog_PresetName(Presets.UniqueName(seed), chosen =>
            {
                captured.name = Presets.UniqueName(chosen);
                Settings.presets.Add(captured);
                Settings.preset = captured.name;
            }));
        }

        private void DoPresetGrid(Listing_Standard listing)
        {
            var builtIn = new Dictionary<string, List<Preset>>();
            var mine = new List<Preset>();

            foreach (Preset p in Presets.All())
            {
                ThemeInfo theme = Info(p.name);
                if (theme == null)
                {
                    mine.Add(p);
                    continue;
                }

                if (!builtIn.TryGetValue(theme.Group, out List<Preset> bucket))
                {
                    bucket = new List<Preset>();
                    builtIn[theme.Group] = bucket;
                }

                bucket.Add(p);
            }

            DrawPresetRow(listing, null, new List<Preset> { null });

            if (mine.Count > 0)
            {
                DrawPresetRow(listing, "LizarbInterface.PresetMine", mine);
            }

            DrawGroup(listing, builtIn, "Handpainted", "LizarbInterface.ThemeHandpainted");
            DrawGroup(listing, builtIn, "Squared", "LizarbInterface.ThemeSquared");
            DrawGroup(listing, builtIn, "Rounded", "LizarbInterface.ThemeRounded");

            if (Prefs.DevMode)
            {
                DrawGroup(listing, builtIn, "Development", "LizarbInterface.ThemeDevelopment");
            }
        }

        private void DrawGroup(Listing_Standard listing, Dictionary<string, List<Preset>> groups,
                               string key, string heading)
        {
            if (groups.TryGetValue(key, out List<Preset> bucket) && bucket.Count > 0)
            {
                DrawPresetRow(listing, heading, bucket);
            }
        }

        private const float SwatchHeight = 96f;

        private void DrawPresetRow(Listing_Standard listing, string heading, List<Preset> presets)
        {
            const float MinSwatchWidth = 148f;

            if (heading != null)
            {
                Head(listing, heading);
            }

            int perRow = Mathf.Clamp(Mathf.FloorToInt(listing.ColumnWidth / MinSwatchWidth), 1, 4);
            int rows = Mathf.CeilToInt(presets.Count / (float)perRow);
            Rect block = listing.GetRect(rows * (SwatchHeight + 6f));
            float cell = block.width / perRow;

            for (int i = 0; i < presets.Count; i++)
            {
                DrawPresetSwatch(new Rect(
                    block.x + (i % perRow) * cell,
                    block.y + (i / perRow) * (SwatchHeight + 6f),
                    cell - 8f,
                    SwatchHeight), presets[i]);
            }
        }

        private static string Label(string id)
        {
            string key = "LizarbInterface.Theme." + id;
            return key.CanTranslate() ? key.Translate().ToString() : id;
        }

        private void DrawPresetSwatch(Rect area, Preset preset)
        {
            bool vanilla = preset == null;
            bool selected = vanilla
                ? !Settings.enabled
                : Settings.enabled && Settings.preset == preset.name;

            if (vanilla)
            {
                DrawVanillaPreview(area);
            }
            else
            {
                DrawPresetPreview(area, preset);
            }

            if (selected)
            {
                Widgets.DrawBox(area, 2);
            }
            else if (Mouse.IsOver(area))
            {
                Widgets.DrawHighlight(area);
            }

            if (Widgets.ButtonInvisible(area))
            {
                if (vanilla)
                {
                    Settings.enabled = false;
                    Settings.preset = "";
                    QueueFontApply();
                }
                else
                {
                    Presets.Apply(preset);
                }

                RimWorld.SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }

        private void DrawPresetPreview(Rect area, Preset preset)
        {
            ThemeInfo skin = Info(preset.theme);

            Texture2D frame = AtlasSwap.Preview(preset.theme, "WindowAtlas");
            if (frame != null)
            {
                AtlasSwap.DrawScaled(area, frame, true, skin, tiled: true);
            }

            if (preset.texturedBackground && preset.backgroundGrain > 0.001f)
            {
                Texture2D grain = AtlasSwap.Preview(preset.theme, "Pattern_" + preset.backgroundPattern, tiling: true);
                if (grain != null)
                {
                    Rect face = area.ContractedBy(10f);
                    float unit = grain.width / AtlasSwap.DefaultScale;
                    Color previous = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, preset.backgroundGrain);
                    GUI.DrawTextureWithTexCoords(face, grain,
                        new Rect(0f, 0f, face.width / unit, face.height / unit));
                    GUI.color = previous;
                }
            }

            Texture2D button = AtlasSwap.Preview(preset.theme, "ButtonBG");
            if (button != null)
            {
                AtlasSwap.DrawScaled(new Rect(area.x + 10f, area.yMax - 30f, area.width - 20f, 22f),
                                     button, true, skin, tiled: true);
            }

            WriteInFont(new Rect(area.x + 4f, area.y + 10f, area.width - 8f, 28f),
                        Label(preset.name), preset);
        }

        private static void WriteInFont(Rect rect, string text, Preset preset)
        {
            Font font = FontEngine.Preview(preset.fontName, 12 + preset.fontOffsetSmall);
            GUIStyle style = Text.CurFontStyle;
            Font previousFont = style.font;
            int previousSize = style.fontSize;

            if (font != null)
            {
                style.font = font;
                style.fontSize = Mathf.Clamp(12 + preset.fontOffsetSmall, 6, 22);
            }

            try
            {
                Write(rect, text, TextAnchor.UpperCenter);
            }
            finally
            {
                style.font = previousFont;
                style.fontSize = previousSize;
            }
        }

        private void OpenPatternMenu()
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

        private static Color vanillaBorder = new ColorInt(97, 108, 122).ToColor;

        private static void DrawVanillaPreview(Rect area)
        {
            Widgets.DrawBoxSolid(area, Widgets.WindowBGFillColor);

            Color previous = GUI.color;
            GUI.color = vanillaBorder;
            Widgets.DrawBox(area);
            GUI.color = previous;

            Texture2D button = Widgets.ButtonBGAtlas;
            if (button == null)
            {
                return;
            }

            AtlasSwap.Bypass = true;
            try
            {
                Widgets.DrawAtlas(new Rect(area.x + 12f, area.yMax - 34f, area.width - 24f, 24f), button);
            }
            finally
            {
                AtlasSwap.Bypass = false;
            }
        }


        private void DoTextTab(Listing_Standard listing)
        {
            Head(listing, "LizarbInterface.FontSection");

            string current = Settings.fontName.NullOrEmpty()
                ? "LizarbInterface.FontVanilla".Translate().ToString()
                : Settings.fontName;

            Pick(listing, "LizarbInterface.FontFace", current, OpenFontPicker);

            Settings.showAllFonts = Toggle(listing, "LizarbInterface.ShowAllFonts", Settings.showAllFonts);

            Settings.fontOffsetTiny = SizeSlide(listing, "LizarbInterface.FontSize.Tiny", Settings.fontOffsetTiny);
            Settings.fontOffsetSmall = SizeSlide(listing, "LizarbInterface.FontSize.Small", Settings.fontOffsetSmall);
            Settings.fontOffsetMedium = SizeSlide(listing, "LizarbInterface.FontSize.Medium", Settings.fontOffsetMedium);

            Rect reset = listing.GetRect(RowHeight);
            listing.Gap(2f);
            if (Widgets.ButtonText(new Rect(reset.x, reset.y + 2f, 200f, reset.height - 4f),
                                   "LizarbInterface.FontReset".Translate()))
            {
                Settings.fontOffsetTiny = 0;
                Settings.fontOffsetSmall = 0;
                Settings.fontOffsetMedium = 0;
                SetFont(LizarbInterfaceSettings.DefaultFont);
            }

            Head(listing, "LizarbInterface.OutlineSection");

            Settings.textOutline = Toggle(listing, "LizarbInterface.TextOutline", Settings.textOutline);

            bool outline = Settings.textOutline;

            Settings.outlineThickness = Slide(listing, "LizarbInterface.OutlineThickness",
                                              Settings.outlineThickness, 1f, 2f, Pixels, 1f, outline);

            Settings.outlineOpacity = Slide(listing, "LizarbInterface.OutlineOpacity",
                                            Settings.outlineOpacity, 0f, 1f, Percent, -1f, outline);

            Settings.outlineTinyText = Toggle(listing, "LizarbInterface.OutlineTiny",
                                              Settings.outlineTinyText, outline);
        }

        private void OpenFontPicker()
        {
            List<string> names = Settings.showAllFonts
                ? FontEngine.InstalledFonts()
                : FontEngine.CuratedFonts();

            Find.WindowStack.Add(new Dialog_FontPicker(names, Settings.fontName, SetFont));
        }

        private int SizeSlide(Listing_Standard listing, string key, int value)
        {
            int next = Mathf.RoundToInt(Slide(listing, key, value, -4f, 8f, Signed, 1f));
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

        private void SetFont(string name)
        {
            Settings.fontName = name;
            QueueFontApply();
        }

        private static readonly string[] IconSamples =
        {
            "IconOrders", "IconProduction", "IconSecurity", "IconMedical", "IconStorage",
            "IconShowZones", "IconShowBeauty", "IconSearchButton", "IconSpeedNormal", "IconSpeedFast",
        };

        private void DoIconsTab(Listing_Standard listing)
        {
            Note(listing, "LizarbInterface.IconsSection".Translate(), DimText);

            Settings.ownIcons = Toggle(listing, "LizarbInterface.OwnIcons", Settings.ownIcons);

            DrawIconStrip(listing, Settings.ownIcons);

            Head(listing, "LizarbInterface.IconButtonSection");

            Settings.plateIconButtons = Toggle(listing,
                "LizarbInterface.PlateIconButtons", Settings.plateIconButtons);

            Head(listing, "LizarbInterface.IconCreditSection");
            Note(listing, "LizarbInterface.IconCredit".Translate(), DimText);
        }

        private void DrawIconStrip(Listing_Standard listing, bool enabled)
        {
            const float Cell = 40f;

            listing.Gap(4f);
            Rect strip = listing.GetRect(Cell);
            listing.Gap(8f);

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Texture2D plate = AtlasSwap.Own("ButtonBG");

            for (int i = 0; i < IconSamples.Length; i++)
            {
                var cell = new Rect(strip.x + 6f + i * (Cell + 4f), strip.y, Cell, Cell);
                if (cell.xMax > strip.xMax)
                {
                    break;
                }

                if (plate != null)
                {
                    AtlasSwap.DrawScaled(cell, plate, true, null, tiled: true);
                }

                Texture2D icon = AtlasSwap.Shared(IconSamples[i]);
                if (icon == null)
                {
                    continue;
                }

                Color previous = GUI.color;
                GUI.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.28f);
                GUI.DrawTexture(cell.ContractedBy(8f), icon);
                GUI.color = previous;
            }
        }

        private void DoArchitectTab(Listing_Standard listing)
        {
            Note(listing, "LizarbInterface.Architect.Section".Translate(), DimText);

            Settings.architectAutoWidth = Toggle(listing,
                "LizarbInterface.Architect.AutoWidth", Settings.architectAutoWidth);

            Settings.architectSpacing = Toggle(listing,
                "LizarbInterface.Architect.Spacing", Settings.architectSpacing);

            Settings.architectPadding = Slide(listing, "LizarbInterface.Architect.Padding",
                                             Settings.architectPadding, 0f, 12f, Pixels, 1f,
                                             Settings.architectSpacing);

            Note(listing, "LizarbInterface.Architect.InPreset".Translate(), DimText);
        }

        private void DoWindowsTab(Listing_Standard listing)
        {
            Head(listing, "LizarbInterface.AnimateSection");

            Settings.windowAnimation = Toggle(listing,
                "LizarbInterface.WindowAnimation", Settings.windowAnimation);

            Settings.animateMainTabs = Toggle(listing,
                "LizarbInterface.AnimateMainTabs", Settings.animateMainTabs);

            Settings.animateImmediate = Toggle(listing,
                "LizarbInterface.AnimateImmediate", Settings.animateImmediate);

            Settings.animateOtherLayers = Toggle(listing,
                "LizarbInterface.AnimateOther", Settings.animateOtherLayers);

            bool any = Settings.windowAnimation || Settings.animateMainTabs ||
                       Settings.animateImmediate || Settings.animateOtherLayers;

            Settings.animationDuration = Slide(listing, "LizarbInterface.AnimationDuration",
                                               Settings.animationDuration, 0.05f, 0.6f, Millis, -1f, any);

            Head(listing, "LizarbInterface.AnimationStyle");
            DoAnimationStyleGrid(listing, any);
        }

        private void DoCompatibilityTab(Listing_Standard listing)
        {
            Note(listing, "LizarbInterface.ComponentsSection".Translate(), DimText);

            Settings.skinButtons = Toggle(listing, "LizarbInterface.SkinButtons", Settings.skinButtons);
            Settings.skinWindows = Toggle(listing, "LizarbInterface.SkinWindows", Settings.skinWindows);
            Settings.skinTabs = Toggle(listing, "LizarbInterface.SkinTabs", Settings.skinTabs);
            Settings.skinWidgets = Toggle(listing, "LizarbInterface.SkinWidgets", Settings.skinWidgets);
            Settings.skinScrollbars = Toggle(listing, "LizarbInterface.SkinScrollbars", Settings.skinScrollbars);

            Head(listing, "LizarbInterface.ResetSection");

            Rect row = listing.GetRect(RowHeight);
            listing.Gap(2f);
            if (Widgets.ButtonText(new Rect(row.x, row.y + 2f, 220f, row.height - 4f),
                                   "LizarbInterface.ResetAll".Translate()))
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

        private void DoAnimationStyleGrid(Listing_Standard listing, bool enabled)
        {
            const float CellHeight = 30f;
            const float MinCellWidth = 150f;

            string[] styles = WindowAnimation.Styles;
            float width = listing.ColumnWidth;
            int columns = Mathf.Clamp(Mathf.FloorToInt(width / MinCellWidth), 1, 4);
            int rows = Mathf.CeilToInt(styles.Length / (float)columns);

            Rect area = listing.GetRect(rows * CellHeight);
            float cellWidth = width / columns;

            Color previous = GUI.color;
            if (!enabled)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
            }

            for (int i = 0; i < styles.Length; i++)
            {
                var cell = new Rect(
                    area.x + (i % columns) * cellWidth,
                    area.y + (i / columns) * CellHeight,
                    cellWidth,
                    CellHeight).ContractedBy(2f);

                if (Settings.windowAnimationStyle == styles[i])
                {
                    Widgets.DrawHighlightSelected(cell);
                }
                else if (enabled && Mouse.IsOver(cell))
                {
                    Widgets.DrawHighlight(cell);
                }

                Write(cell, ("LizarbInterface.Animation." + styles[i]).Translate(), TextAnchor.MiddleCenter);

                if (enabled && Widgets.ButtonInvisible(cell))
                {
                    Settings.windowAnimationStyle = styles[i];
                    ReplayAnimation();
                    RimWorld.SoundDefOf.Click.PlayOneShotOnCamera();
                }
            }

            GUI.color = previous;
        }

        private static void ReplayAnimation()
        {
            Window window = Find.WindowStack?.WindowOfType<RimWorld.Dialog_ModSettings>();
            if (window != null)
            {
                Patch_WindowMotion.Forget(window);
            }
        }

        private static string[] PlateStyles => ArchitectPlate.Styles;

        private void DoPlateStyleGrid(Listing_Standard listing, bool enabled)
        {
            const float CellHeight = 42f;
            const float MinCellWidth = 290f;

            string[] styles = PlateStyles;
            float width = listing.ColumnWidth;
            int columns = Mathf.Clamp(Mathf.FloorToInt(width / MinCellWidth), 1, 3);
            int rows = Mathf.CeilToInt(styles.Length / (float)columns);

            Rect area = listing.GetRect(rows * CellHeight);
            float cellWidth = width / columns;

            Color previous = GUI.color;
            if (!enabled)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
            }

            for (int i = 0; i < styles.Length; i++)
            {
                var cell = new Rect(
                    area.x + (i % columns) * cellWidth,
                    area.y + (i / columns) * CellHeight,
                    cellWidth,
                    CellHeight);

                DoPlateStyleCell(cell, styles[i], enabled);
            }

            GUI.color = previous;
        }

        private void DoPlateStyleCell(Rect cell, string style, bool enabled)
        {
            Rect inner = cell.ContractedBy(2f);

            if (Settings.architectPlateStyle == style)
            {
                Widgets.DrawHighlightSelected(inner);
            }
            else if (enabled && Mouse.IsOver(inner))
            {
                Widgets.DrawHighlight(inner);
            }

            float sampleWidth = Mathf.Min(150f, inner.width * 0.55f);
            var sample = new Rect(inner.x + 4f, inner.y + 3f, sampleWidth, inner.height - 6f);
            DrawPlateSample(sample, style);

            Rect label = inner;
            label.xMin = sample.xMax + 10f;
            Write(label, ("LizarbInterface.Architect.PlateStyle." + style).Translate(), TextAnchor.MiddleLeft);

            TooltipHandler.TipRegion(inner,
                ("LizarbInterface.Architect.PlateStyle." + style + ".Tip").Translate());

            if (enabled && Widgets.ButtonInvisible(inner))
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
                AtlasSwap.DrawScaled(rect, button, true, null, tiled: true);
            }

            Color tint = new Color(205f / 255f, 137f / 255f, 95f / 255f, Settings.architectPlateAlpha);
            ArchitectPlate.Draw(rect.ContractedBy(3f), style, tint);

            ArchitectIcons.Draw(rect, "Production");

            Color label = Settings.architectColorLabels
                ? Patch_ButtonTextSubtle.Readable(tint)
                : Color.white;

            Color previous = GUI.color;
            GUI.color = label;
            float textLeft = ArchitectIcons.MarginFor(rect);
            Write(new Rect(rect.x + textLeft, rect.y, rect.width - textLeft - 4f, rect.height),
                  "Production", TextAnchor.MiddleLeft);
            GUI.color = previous;
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

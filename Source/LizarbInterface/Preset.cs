using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    public class Preset : IExposable
    {
        public string name = "";
        public string theme = LizarbInterfaceSettings.DefaultTheme;

        public string fontName = LizarbInterfaceSettings.DefaultFont;
        public int fontOffsetTiny;
        public int fontOffsetSmall;
        public int fontOffsetMedium;

        public bool textOutline = true;
        public float outlineThickness = 2f;
        public float outlineOpacity = 0.7f;
        public bool outlineTinyText = true;

        public bool texturedBackground = true;
        public string backgroundPattern = LizarbInterfaceSettings.DefaultPattern;
        public float backgroundGrain = LizarbInterfaceSettings.DefaultGrain;
        public bool grainOnButtons = true;

        public float inset = 1f;
        public bool pointFilter;

        public bool architectColors = true;
        public bool architectAutoColor = true;
        public string architectPlateStyle = LizarbInterfaceSettings.DefaultPlateStyle;
        public float architectPlateAlpha = 1f;
        public bool architectShapeOutline = true;
        public bool architectColorLabels;

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name", "");
            Scribe_Values.Look(ref theme, "theme", LizarbInterfaceSettings.DefaultTheme);
            Scribe_Values.Look(ref fontName, "fontName", LizarbInterfaceSettings.DefaultFont);
            Scribe_Values.Look(ref fontOffsetTiny, "fontOffsetTiny", 0);
            Scribe_Values.Look(ref fontOffsetSmall, "fontOffsetSmall", 0);
            Scribe_Values.Look(ref fontOffsetMedium, "fontOffsetMedium", 0);
            Scribe_Values.Look(ref textOutline, "textOutline", defaultValue: true);
            Scribe_Values.Look(ref outlineThickness, "outlineThickness", 2f);
            Scribe_Values.Look(ref outlineOpacity, "outlineOpacity", 0.7f);
            Scribe_Values.Look(ref outlineTinyText, "outlineTinyText", defaultValue: true);
            Scribe_Values.Look(ref texturedBackground, "texturedBackground", defaultValue: true);
            Scribe_Values.Look(ref backgroundPattern, "backgroundPattern", LizarbInterfaceSettings.DefaultPattern);
            Scribe_Values.Look(ref backgroundGrain, "backgroundGrain", LizarbInterfaceSettings.DefaultGrain);
            Scribe_Values.Look(ref grainOnButtons, "grainOnButtons", defaultValue: true);
            Scribe_Values.Look(ref inset, "inset", 1f);
            Scribe_Values.Look(ref pointFilter, "pointFilter", defaultValue: false);
            Scribe_Values.Look(ref architectColors, "architectColors", defaultValue: true);
            Scribe_Values.Look(ref architectAutoColor, "architectAutoColor", defaultValue: true);
            Scribe_Values.Look(ref architectPlateStyle, "architectPlateStyle", LizarbInterfaceSettings.DefaultPlateStyle);
            Scribe_Values.Look(ref architectPlateAlpha, "architectPlateAlpha", 1f);
            Scribe_Values.Look(ref architectShapeOutline, "architectShapeOutline", defaultValue: true);
            Scribe_Values.Look(ref architectColorLabels, "architectColorLabels", defaultValue: false);
        }

        public Preset Copy()
        {
            return (Preset)MemberwiseClone();
        }

        public bool SameAs(Preset other)
        {
            return other != null &&
                   theme == other.theme &&
                   fontName == other.fontName &&
                   fontOffsetTiny == other.fontOffsetTiny &&
                   fontOffsetSmall == other.fontOffsetSmall &&
                   fontOffsetMedium == other.fontOffsetMedium &&
                   textOutline == other.textOutline &&
                   Mathf.Approximately(outlineThickness, other.outlineThickness) &&
                   Mathf.Approximately(outlineOpacity, other.outlineOpacity) &&
                   outlineTinyText == other.outlineTinyText &&
                   texturedBackground == other.texturedBackground &&
                   backgroundPattern == other.backgroundPattern &&
                   Mathf.Approximately(backgroundGrain, other.backgroundGrain) &&
                   grainOnButtons == other.grainOnButtons &&
                   Mathf.Approximately(inset, other.inset) &&
                   pointFilter == other.pointFilter &&
                   architectColors == other.architectColors &&
                   architectAutoColor == other.architectAutoColor &&
                   architectPlateStyle == other.architectPlateStyle &&
                   Mathf.Approximately(architectPlateAlpha, other.architectPlateAlpha) &&
                   architectShapeOutline == other.architectShapeOutline &&
                   architectColorLabels == other.architectColorLabels;
        }
    }

    internal static class Presets
    {
        internal const string Vanilla = "";

        internal static List<Preset> All()
        {
            var all = new List<Preset>();

            foreach (ThemeInfo theme in LizarbInterfaceMod.AllThemes)
            {
                all.Add(FromTheme(theme));
            }

            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            if (settings?.presets != null)
            {
                all.AddRange(settings.presets);
            }

            return all;
        }

        internal static Preset FromTheme(ThemeInfo theme)
        {
            var preset = new Preset { name = theme.Id, theme = theme.Id };

            preset.backgroundPattern = theme.Pattern;

            if (theme.Grain > 0f)
            {
                preset.texturedBackground = true;
                preset.backgroundGrain = theme.Grain;
            }

            if (theme.Background.HasValue)
            {
                preset.texturedBackground = theme.Background.Value;
            }

            if (theme.PointFilter.HasValue)
            {
                preset.pointFilter = theme.PointFilter.Value;
            }

            return preset;
        }

        internal static Preset Capture()
        {
            LizarbInterfaceSettings s = LizarbInterfaceMod.Settings;

            return new Preset
            {
                theme = s.theme,
                fontName = s.fontName,
                fontOffsetTiny = s.fontOffsetTiny,
                fontOffsetSmall = s.fontOffsetSmall,
                fontOffsetMedium = s.fontOffsetMedium,
                textOutline = s.textOutline,
                outlineThickness = s.outlineThickness,
                outlineOpacity = s.outlineOpacity,
                outlineTinyText = s.outlineTinyText,
                texturedBackground = s.texturedBackground,
                backgroundPattern = s.backgroundPattern,
                backgroundGrain = s.backgroundGrain,
                grainOnButtons = s.grainOnButtons,
                inset = s.inset,
                pointFilter = s.pointFilter,
                architectColors = s.architectColors,
                architectAutoColor = s.architectAutoColor,
                architectPlateStyle = s.architectPlateStyle,
                architectPlateAlpha = s.architectPlateAlpha,
                architectShapeOutline = s.architectShapeOutline,
                architectColorLabels = s.architectColorLabels,
            };
        }

        internal static void Apply(Preset preset)
        {
            LizarbInterfaceSettings s = LizarbInterfaceMod.Settings;

            s.enabled = true;
            s.theme = preset.theme;
            s.fontName = preset.fontName;
            s.fontOffsetTiny = preset.fontOffsetTiny;
            s.fontOffsetSmall = preset.fontOffsetSmall;
            s.fontOffsetMedium = preset.fontOffsetMedium;
            s.textOutline = preset.textOutline;
            s.outlineThickness = preset.outlineThickness;
            s.outlineOpacity = preset.outlineOpacity;
            s.outlineTinyText = preset.outlineTinyText;
            s.texturedBackground = preset.texturedBackground;
            s.backgroundPattern = preset.backgroundPattern;
            s.backgroundGrain = preset.backgroundGrain;
            s.grainOnButtons = preset.grainOnButtons;
            s.inset = preset.inset;
            s.pointFilter = preset.pointFilter;
            s.architectColors = preset.architectColors;
            s.architectAutoColor = preset.architectAutoColor;
            s.architectPlateStyle = preset.architectPlateStyle;
            s.architectPlateAlpha = preset.architectPlateAlpha;
            s.architectShapeOutline = preset.architectShapeOutline;
            s.architectColorLabels = preset.architectColorLabels;
            s.preset = preset.name;

            LizarbInterfaceMod.QueueFontApply();
        }

        internal static string UniqueName(string wanted)
        {
            var taken = new HashSet<string>();
            foreach (Preset p in All())
            {
                taken.Add(p.name);
            }

            if (!taken.Contains(wanted))
            {
                return wanted;
            }

            for (int i = 2; i < 999; i++)
            {
                string candidate = wanted + " " + i;
                if (!taken.Contains(candidate))
                {
                    return candidate;
                }
            }

            return wanted;
        }

        internal static bool IsBuiltIn(string name)
        {
            foreach (ThemeInfo theme in LizarbInterfaceMod.AllThemes)
            {
                if (theme.Id == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

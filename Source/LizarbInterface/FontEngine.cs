using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    internal static class FontEngine
    {
        private const int Count = 3;

        private static readonly Font[] vanillaFont = new Font[Count];
        private static readonly int[] vanillaSize = new int[Count];
        private static bool captured;

        private static readonly Dictionary<string, Font> cache = new Dictionary<string, Font>();

        private static readonly string[] Shortlist =
        {
            "Philosopher",
            "Quattrocento Sans",
            "Amaranth",

            "Metamorphous",
            "Uncial Antiqua",
            "Grenze Gotisch",
            "MedievalSharp",

            "EB Garamond",
            "Marcellus",
            "Cinzel",
            "Cormorant Garamond",
            "Alegreya",
            "Vollkorn",
            "Spectral",
            "IM FELL English",

            "Trajan Pro",
            "Book Antiqua",
            "Palatino Linotype",
            "Garamond",
            "Adobe Garamond Pro",
            "Goudy Old Style",
            "Centaur",
            "Constantia",
            "Cambria",
            "Bell MT",
            "Calisto MT",
            "Baskerville Old Face",
            "Perpetua",
            "Bookman Old Style",
            "Georgia",
            "Times New Roman",
        };

        private static readonly Dictionary<string, bool> available = new Dictionary<string, bool>();

        private static bool reportedAvailability;

        private const string ControlName = "__LizarbInterface_no_such_font__";

        private const int ProbeSize = 16;
        private const string ProbeChars = "AWgjMq";

        private static Font control;

        private static bool Available(string name)
        {
            if (available.TryGetValue(name, out bool known))
            {
                return known;
            }

            bool ok = FontBundle.Get(name) != null || DiffersFromFallback(name);
            available[name] = ok;
            return ok;
        }

        private static bool DiffersFromFallback(string name)
        {
            Font probe = Font.CreateDynamicFontFromOSFont(name, ProbeSize);
            if (probe == null)
            {
                return false;
            }

            if (control == null)
            {
                control = Font.CreateDynamicFontFromOSFont(ControlName, ProbeSize);
                if (control != null)
                {
                    control.RequestCharactersInTexture(ProbeChars, ProbeSize);
                }
            }

            if (control == null)
            {
                return true;
            }

            probe.RequestCharactersInTexture(ProbeChars, ProbeSize);

            if (!Mathf.Approximately(probe.lineHeight, control.lineHeight))
            {
                return true;
            }

            return AdvanceOf(probe, 'W') != AdvanceOf(control, 'W')
                || AdvanceOf(probe, 'g') != AdvanceOf(control, 'g');
        }

        private static float AdvanceOf(Font font, char c)
        {
            return font.GetCharacterInfo(c, out CharacterInfo info, ProbeSize) ? info.advance : -1f;
        }

        internal static List<string> CuratedFonts()
        {
            var result = new List<string>();
            var missing = new List<string>();

            foreach (string name in FontBundle.Names())
            {
                if (!result.Contains(name))
                {
                    result.Add(name);
                }
            }

            foreach (string name in Shortlist)
            {
                if (result.Contains(name))
                {
                    continue;
                }

                if (Available(name))
                {
                    result.Add(name);
                }
                else
                {
                    missing.Add(name);
                }
            }

            result.Sort(System.StringComparer.OrdinalIgnoreCase);

            if (!reportedAvailability && Prefs.DevMode)
            {
                reportedAvailability = true;
                Log.Message(
                    "[LizarbInterface] fonts usable: " + string.Join(", ", result.ToArray()) +
                    "\n[LizarbInterface] fonts not found: " + string.Join(", ", missing.ToArray()) +
                    "\n[LizarbInterface] metrics " + Metrics(ControlName) +
                    " " + Metrics("EB Garamond") +
                    " " + Metrics("Georgia") +
                    " " + Metrics("Philosopher"));
            }

            return result;
        }

        private static string Metrics(string name)
        {
            Font f = Font.CreateDynamicFontFromOSFont(name, ProbeSize);
            if (f == null)
            {
                return name + "=NULL";
            }

            f.RequestCharactersInTexture(ProbeChars, ProbeSize);
            return name + "[lh=" + f.lineHeight.ToString("0.0") +
                   " W=" + AdvanceOf(f, 'W').ToString("0.0") +
                   " g=" + AdvanceOf(f, 'g').ToString("0.0") + "]";
        }

        internal static List<string> InstalledFonts()
        {
            var names = new List<string>(FontBundle.Names());
            foreach (string name in Font.GetOSInstalledFontNames())
            {
                if (!names.Contains(name))
                {
                    names.Add(name);
                }
            }

            names.Sort(System.StringComparer.OrdinalIgnoreCase);
            return names;
        }

        internal static void Apply()
        {
            if (!Capture())
            {
                return;
            }

            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            bool custom = settings != null && !settings.fontName.NullOrEmpty();

            for (int i = 0; i < Count; i++)
            {
                int size = vanillaSize[i] + (settings == null ? 0 : settings.FontSizeOffset(i));
                if (size < 6)
                {
                    size = 6;
                }

                Font font = vanillaFont[i];
                if (custom)
                {
                    font = Resolve(settings.fontName, size) ?? vanillaFont[i];
                }

                bool untouched = !custom && (settings == null || settings.FontSizeOffset(i) == 0);
                int styleSize = untouched ? 0 : size;

                ApplyTo(Text.fontStyles, i, font, styleSize);
                ApplyTo(Text.textFieldStyles, i, font, styleSize);
                ApplyTo(Text.textAreaStyles, i, font, styleSize);
                ApplyTo(Text.textAreaReadOnlyStyles, i, font, styleSize);
            }

            RecomputeLineMetrics();
        }

        private static void ApplyTo(GUIStyle[] styles, int index, Font font, int size)
        {
            if (styles == null || index >= styles.Length || styles[index] == null)
            {
                return;
            }

            styles[index].font = font;
            styles[index].fontSize = size;
        }

        private static bool Capture()
        {
            if (captured)
            {
                return true;
            }

            if (Text.fontStyles == null || Text.fontStyles.Length < Count)
            {
                return false;
            }

            for (int i = 0; i < Count; i++)
            {
                if (Text.fontStyles[i] == null || Text.fontStyles[i].font == null)
                {
                    return false;
                }

                vanillaFont[i] = Text.fontStyles[i].font;
                vanillaSize[i] = Text.fontStyles[i].font.fontSize;
            }

            captured = true;
            return true;
        }

        internal static Font Preview(string name, int size)
        {
            return Resolve(name, size);
        }

        private static Font Resolve(string name, int size)
        {
            Font bundled = FontBundle.Get(name);
            if (bundled != null)
            {
                return bundled;
            }

            string key = name + "|" + size;
            if (cache.TryGetValue(key, out Font cached) && cached != null)
            {
                return cached;
            }

            Font font = Font.CreateDynamicFontFromOSFont(name, size);
            if (font == null)
            {
                Log.WarningOnce("[LizarbInterface] could not create font: " + name, name.GetHashCode());
                return null;
            }

            font.hideFlags = HideFlags.HideAndDontSave;
            cache[key] = font;
            return font;
        }

        private static void RecomputeLineMetrics()
        {
            var lineHeights = HarmonyLib.AccessTools.StaticFieldRefAccess<float[]>(typeof(Text), "lineHeights");
            var spaceBetween = HarmonyLib.AccessTools.StaticFieldRefAccess<float[]>(typeof(Text), "spaceBetweenLines");
            if (lineHeights == null || spaceBetween == null)
            {
                return;
            }

            GameFont previous = Text.Font;
            for (int i = 0; i < Count; i++)
            {
                Text.Font = (GameFont)i;
                float single = Text.CalcHeight("W", 999f);
                lineHeights[i] = single;
                spaceBetween[i] = Text.CalcHeight("W\nW", 999f) - single * 2f;
            }

            Text.Font = previous;
        }
    }
}

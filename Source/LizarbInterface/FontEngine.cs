using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Swaps the font behind every piece of text in the game, with no Harmony at all.
    ///
    /// Verse.Text keeps four GUIStyle arrays that every draw path reads. They are
    /// `static readonly`, but that only protects the reference. The GUIStyle objects
    /// inside are ordinary mutable objects, so setting .font and .fontSize on them
    /// changes the whole UI at once.
    ///
    /// Fonts resolve from the shipped AssetBundle first (see FontBundle), then from
    /// whatever is installed on the machine.
    /// </summary>
    internal static class FontEngine
    {
        private const int Count = 3;   // Tiny, Small, Medium

        private static readonly Font[] vanillaFont = new Font[Count];
        private static readonly int[] vanillaSize = new int[Count];
        private static bool captured;

        /// <summary>Cache keyed by "name|size" so switching back and forth is free.</summary>
        private static readonly Dictionary<string, Font> cache = new Dictionary<string, Font>();

        /// <summary>
        /// Shortlist in preference order: serif and humanist faces that stay readable at
        /// the 12-16px RimWorld actually draws at. Blackletter is deliberately absent -
        /// the most medieval thing on a Windows box and unreadable in a colonist list.
        /// </summary>
        private static readonly string[] Shortlist =
        {
            // Shipped in AssetBundles/, all SIL Open Font Licence, so these are
            // available to every player with nothing to install. A font here that is
            // NOT in the bundle still works if the machine happens to have it.

            // Sans-serif first. Worth saying plainly: sans-serif is a 19th century
            // invention, so nothing here is medieval in the literal sense. These are
            // humanist faces with calligraphic detail, which is the closest a sans gets
            // to the period while staying readable in a colonist list.
            "Philosopher",         // sans with calligraphic joints; best of the three
            "Quattrocento Sans",   // humanist, classical proportions, very clean
            "Amaranth",            // sans with a slight flare where serifs would be

            // Display faces with real period character. Legible enough for the UI, but
            // heavier going than the sans above in dense screens.
            "Metamorphous",        // the fantasy/medieval one people picture
            "Uncial Antiqua",      // actual uncial, the medieval book hand
            "Grenze Gotisch",      // blackletter modernised enough to still read
            "MedievalSharp",

            // Serif, for whoever wants the book look.
            "EB Garamond",         // best body text of the set; 16th century source
            "Marcellus",           // elegant roman, quiet
            "Cinzel",              // Roman capitals; strong for headings
            "Cormorant Garamond",
            "Alegreya",            // humanist, drawn for long-form reading
            "Vollkorn",
            "Spectral",
            "IM FELL English",     // 17th century, characterful and still legible

            // Faces already on most Windows machines.
            "Trajan Pro",
            "Book Antiqua",        // present wherever Office is
            "Palatino Linotype",   // presente em todo Windows
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

        /// <summary>Availability answers, so the probe below runs once per name.</summary>
        private static readonly Dictionary<string, bool> available = new Dictionary<string, bool>();

        private static bool reportedAvailability;

        /// <summary>
        /// A name that cannot exist. Whatever Unity hands back for it IS the fallback,
        /// so it doubles as the control sample for the comparison below.
        /// </summary>
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

        /// <summary>
        /// Does asking for this family actually give us that family?
        ///
        /// fontNames[0] does NOT answer it - Unity echoes back the requested name
        /// whether or not it found anything. So the font is compared against a
        /// deliberately impossible name: whatever Unity returns for that IS the
        /// fallback, and a real font differs in line height or advance width. Metrics
        /// only exist once glyphs are in the atlas, hence RequestCharactersInTexture.
        /// </summary>
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
                // No control to compare against; assume the name is good rather than
                // hiding every font.
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

        /// <summary>The shortlist filtered to what is actually usable.</summary>
        internal static List<string> CuratedFonts()
        {
            var result = new List<string>();
            var missing = new List<string>();

            // Bundled fonts first, and NOT through Available(): that probe asks the
            // OS, and a font from the AssetBundle was never installed. Without this
            // the bundle loads, logs its fonts, and none of them reach the picker -
            // which looks exactly like the bundle having failed.
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

        /// <summary>Evidence line: what Unity really hands back for a given name.</summary>
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
            // Bundled fonts belong here too: "show every installed font" must widen
            // the list, never drop something the curated one offered.
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

        /// <summary>
        /// Rebuilds the styles from current settings. Safe to call repeatedly, since it is
        /// also the repair path, since a dynamic Font is a runtime object that the
        /// engine can collect.
        /// </summary>
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

                // fontSize 0 tells Unity to use the font's own design size. Restoring
                // that on the way back keeps vanilla pixel-identical rather than
                // merely close.
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
                // A dynamic font can be destroyed by the engine; Unity's == catches
                // that. Re-resolving happens through the cache below.
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

        /// <summary>
        /// Font for a name, for drawing a preview. Same resolution order as the real
        /// thing (bundle first, then the OS), so what the picker shows is what
        /// selecting it will produce.
        /// </summary>
        internal static Font Preview(string name, int size)
        {
            return Resolve(name, size);
        }

        private static Font Resolve(string name, int size)
        {
            // A bundled font is a plain Unity asset: nothing installed, no OS lookup,
            // and it carries its own size so the size argument does not apply.
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
                Log.Warning("[LizarbInterface] could not create font: " + name);
                return null;
            }

            // Runtime-created like our textures, so the same protection applies.
            font.hideFlags = HideFlags.HideAndDontSave;
            cache[key] = font;
            return font;
        }

        /// <summary>
        /// Text.lineHeights and Text.spaceBetweenLines are measured from the fonts in
        /// Text's static constructor and never recomputed. Changing the font without
        /// updating them leaves every multi-line block mis-spaced, so they are measured
        /// again exactly the way vanilla does it.
        /// </summary>
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

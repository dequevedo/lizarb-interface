using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Lets any DesignationCategoryDef declare how it is coloured, including
    /// categories added by other mods. See Patches/ArchitectPalette.xml.
    /// </summary>
    public class ArchitectColorExtension : DefModExtension
    {
        /// <summary>Family name from CategoryPalette.Families. The normal way to do this.</summary>
        public string family;

        // ParseHelper.ParseColor reads "(230,180,60)" as 0-255 and "(0.9,0.7,0.2)" as
        // 0-1: if any component exceeds 1 the whole string is treated as 0-255.
        /// <summary>Exact colour, overriding the family's. For one-off tuning.</summary>
        public Color? color;

        /// <summary>Icon name, overriding the family's.</summary>
        public string icon;
    }

    /// <summary>A group of categories that share a colour, and the icon that fits them.</summary>
    internal sealed class CategoryFamily
    {
        public readonly string Name;
        public readonly Color Color;
        public readonly string Icon;
        public readonly string[] Keywords;

        public CategoryFamily(string name, int r, int g, int b, string icon, params string[] keywords)
        {
            Name = name;
            Color = new Color(r / 255f, g / 255f, b / 255f);
            Icon = icon;
            Keywords = keywords;
        }
    }

    /// <summary>
    /// Colour and icon for an architect category.
    ///
    /// The point of the family table is restraint. Sampling the hue circle gives
    /// every modded category an unrelated colour, and thirty unrelated colours read
    /// as noise however pretty each one is. Everything lands in one of twelve
    /// entries at a matched saturation instead, so the menu groups by meaning.
    /// </summary>
    public static class CategoryPalette
    {
        // Categories that do related jobs deliberately share a hue: that
        // similarity IS the grouping.
        private static readonly CategoryFamily[] Families =
        {
            new CategoryFamily("Designate", 219, 178, 99, "Orders",
                "order", "designat", "command", "plan", "blueprint", "marker", "sign", "label", "misc"),

            new CategoryFamily("Zone", 150, 186, 118, "Zone",
                "zone", "area", "farm", "grow", "field", "garden"),

            new CategoryFamily("Build", 147, 163, 187, "Structure",
                "structure", "wall", "build", "construct", "roof", "door", "bridge", "fence"),

            new CategoryFamily("Floor", 174, 150, 116, "Floors",
                "floor", "terrain", "path", "road", "carpet", "tile"),

            new CategoryFamily("Storage", 178, 164, 116, "Storage",
                "storage", "stockpile", "shelf", "container", "crate", "warehouse"),

            new CategoryFamily("Comfort", 191, 149, 110, "Furniture",
                "furniture", "decor", "joy", "recreation", "tavern", "bed", "comfort",
                "education", "school", "art", "music", "book"),

            new CategoryFamily("Industry", 205, 137, 95, "Production",
                "production", "craft", "work", "bench", "forge", "kitchen", "industr",
                "power", "energy", "electr", "pipe", "network", "conduit", "machine",
                "vehicle", "rail", "transport", "logistic", "mech", "robot", "tech"),

            new CategoryFamily("Defence", 198, 106, 100, "Security",
                "security", "defen", "weapon", "turret", "trap", "military", "war"),

            new CategoryFamily("Life", 118, 178, 158, "Medical",
                "medical", "medicine", "hospital", "health", "biotech", "genetic",
                "gene", "bio", "insect", "animal", "creature", "flesh"),

            new CategoryFamily("Climate", 120, 172, 194, "Temperature",
                "temperature", "climate", "heat", "cool", "vent", "water", "hydro", "atmos"),

            new CategoryFamily("Beyond", 168, 134, 196, "Ideology",
                "ideology", "ritual", "faith", "anomaly", "void", "arcane", "magic",
                "mythic", "ship", "odyssey", "space", "gravship", "star"),

            new CategoryFamily("Utility", 158, 162, 168, "Misc",
                "dev", "debug", "test", "tool", "utility", "other"),
        };

        // A family carries one icon, so everything in it would otherwise share a
        // picture: Orders and Blueprints both landed on the flag. These run first
        // and only decide the icon; the colour still comes from the family, which
        // is what keeps the grouping intact.
        //
        // Matched on a word boundary, not anywhere: "Designations" contains "sign",
        // while "Technical vehicles" genuinely is about vehicles.
        private static readonly string[][] IconHints =
        {
            new[] { "Blueprint", "blueprint" },
            new[] { "Sign",      "sign", "label" },
            new[] { "Vehicle",   "vehicle", "rail", "transport" },
            new[] { "Industry",  "industr", "machine", "mech" },
            new[] { "Joy",       "tavern", "music" },
            new[] { "Arcane",    "mythic", "arcane", "magic" },
            new[] { "Water",     "water", "hydro" },
            new[] { "Ship",      "gravship", "ship", "rocket" },
            new[] { "Nature",    "insect", "animal", "plant", "tree" },
            new[] { "Storage",   "storage", "stockpile", "shelf" },
            new[] { "Medical",   "genetic", "gene", "medical", "health" },
            new[] { "Power",     "power", "energy", "electr" },
            new[] { "Ideology",  "ideology", "ritual", "faith" },
            new[] { "Anomaly",   "anomaly", "void" },
            new[] { "Odyssey",   "odyssey", "orbital" },
            new[] { "Biotech",   "biotech" },
            new[] { "Joy",       "recreation", "joy" },
            new[] { "Misc",      "misc" },
        };

        // Keyed on defName, which is the category identity and does not change.
        // The XML extension is deliberately NOT cached, so "Hot reload Defs" still
        // picks up palette edits; only the keyword scan is memoised, and that scan
        // is the expensive half. It would otherwise run for every button of every
        // repaint, allocating a lowercased string each time.
        private static readonly Dictionary<string, CategoryFamily> guessed =
            new Dictionary<string, CategoryFamily>();

        public static Color HueFor(DesignationCategoryDef def)
        {
            if (!Active(def))
            {
                return Color.white;
            }

            ArchitectColorExtension ext = def.GetModExtension<ArchitectColorExtension>();
            if (ext?.color != null)
            {
                return ext.color.Value;
            }

            return Resolve(def, ext)?.Color ?? Color.white;
        }

        /// <summary>Icon name for a category, or null when it should not get one.</summary>
        public static string IconFor(DesignationCategoryDef def)
        {
            if (def == null)
            {
                return null;
            }

            ArchitectColorExtension ext = def.GetModExtension<ArchitectColorExtension>();
            if (!string.IsNullOrEmpty(ext?.icon))
            {
                return ext.icon;
            }

            return Hint(def.defName) ?? Hint(def.label) ?? Resolve(def, ext)?.Icon;
        }

        private static bool Active(DesignationCategoryDef def)
        {
            LizarbInterfaceSettings settings = LizarbInterfaceMod.Settings;
            return def != null && settings != null && settings.enabled && settings.architectColors;
        }

        /// <summary>
        /// Explicit family, else a keyword in the defName or label, else a stable
        /// pick from the table.
        /// </summary>
        private static CategoryFamily Resolve(DesignationCategoryDef def, ArchitectColorExtension ext)
        {
            if (!string.IsNullOrEmpty(ext?.family))
            {
                CategoryFamily named = ByName(ext.family);
                if (named != null)
                {
                    return named;
                }

                Log.WarningOnce(
                    "[LizarbInterface] unknown architect family " + ext.family + " on " + def.defName,
                    def.defName.GetHashCode());
            }

            if (!guessed.TryGetValue(def.defName, out CategoryFamily guess))
            {
                guess = Match(def.defName) ?? Match(def.label);
                guessed[def.defName] = guess;
            }

            if (guess != null)
            {
                return guess;
            }

            if (LizarbInterfaceMod.Settings?.architectAutoColor != true)
            {
                return null;
            }

            // Nothing matched: a stable pick keeps the category constant across
            // sessions, which matters more than which entry it lands on.
            int hash = Mathf.Abs(GenText.StableStringHash(def.defName));
            return Families[hash % Families.Length];
        }

        private static CategoryFamily ByName(string name)
        {
            foreach (CategoryFamily family in Families)
            {
                if (string.Equals(family.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return family;
                }
            }

            return null;
        }

        private static CategoryFamily Match(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            string lower = text.ToLowerInvariant();
            foreach (CategoryFamily family in Families)
            {
                foreach (string keyword in family.Keywords)
                {
                    if (StartsWord(lower, keyword))
                    {
                        return family;
                    }
                }
            }

            return null;
        }

        private static string Hint(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            string lower = text.ToLowerInvariant();
            foreach (string[] hint in IconHints)
            {
                for (int i = 1; i < hint.Length; i++)
                {
                    if (StartsWord(lower, hint[i]))
                    {
                        return hint[0];
                    }
                }
            }

            return null;
        }

        /// <summary>True when the keyword begins a word in the text.</summary>
        private static bool StartsWord(string text, string keyword)
        {
            int at = 0;
            while ((at = text.IndexOf(keyword, at, StringComparison.Ordinal)) >= 0)
            {
                if (at == 0 || !char.IsLetter(text[at - 1]))
                {
                    return true;
                }

                at++;
            }

            return false;
        }
    }
}

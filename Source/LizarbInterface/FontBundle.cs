using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    internal static class FontBundle
    {
        private static Dictionary<string, Font> byName;

        internal static List<string> Names()
        {
            Load();
            return new List<string>(byName.Keys);
        }

        internal static Font Get(string family)
        {
            Load();
            if (!byName.TryGetValue(family, out Font font))
            {
                return null;
            }

            if (font != null)
            {
                return font;
            }

            byName = null;
            Load();
            return byName.TryGetValue(family, out font) && font != null ? font : null;
        }

        private static readonly string[] Families =
        {
            "Alegreya", "Amaranth", "Audiowide", "Black Ops One", "Bungee", "Chakra Petch", "Cinzel",
            "Cormorant Garamond", "EB Garamond", "Electrolize", "Exo 2", "Grenze Gotisch",
            "IM FELL English", "Jacquard 24", "Jersey 10", "Marcellus", "MedievalSharp", "Metamorphous",
            "Michroma", "Orbitron", "Philosopher", "Pixelify Sans", "Quattrocento Sans", "Rajdhani",
            "Russo One", "Saira Stencil One", "Spectral", "Teko", "Uncial Antiqua", "Vollkorn", "Zen Dots",
        };

        private static string Normalize(string text)
        {
            var sb = new System.Text.StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }

            return sb.ToString();
        }

        private static readonly System.Reflection.PropertyInfo FontNames =
            HarmonyLib.AccessTools.Property(typeof(Font), "fontNames");

        private static string FamilyOf(Font font)
        {
            if (FontNames != null)
            {
                try
                {
                    if (FontNames.GetValue(font, null) is string[] names && names.Length > 0)
                    {
                        return names[0];
                    }
                }
                catch (System.Exception)
                {
                }
            }

            string raw = font.name ?? "";
            int dash = raw.LastIndexOf('-');
            string stem = dash > 0 ? raw.Substring(0, dash) : raw;
            string key = Normalize(stem);

            foreach (string family in Families)
            {
                if (Normalize(family) == key)
                {
                    return family;
                }
            }

            return stem;
        }

        private static void Load()
        {
            if (byName != null)
            {
                return;
            }

            byName = new Dictionary<string, Font>();

            ModContentPack content = LizarbInterfaceMod.Pack;
            if (content?.assetBundles?.loadedAssetBundles == null)
            {
                return;
            }

            foreach (AssetBundle bundle in content.assetBundles.loadedAssetBundles)
            {
                if (bundle == null)
                {
                    continue;
                }

                foreach (Font font in bundle.LoadAllAssets<Font>())
                {
                    if (font == null)
                    {
                        continue;
                    }

                    string family = FamilyOf(font);

                    byName[family] = font;
                }
            }

            if (byName.Count == 0 || !Prefs.DevMode)
            {
                return;
            }

            var names = new List<string>(byName.Keys);
            names.Sort(System.StringComparer.OrdinalIgnoreCase);
            Log.Message(
                "[LizarbInterface] " + byName.Count + " font(s) loaded from AssetBundle: " +
                string.Join(", ", names.ToArray()));
        }
    }
}

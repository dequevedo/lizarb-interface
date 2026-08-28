using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LizarbInterface
{
    /// <summary>
    /// Fonts loaded from an AssetBundle shipped with the mod. This is the only way to
    /// give players a font without asking them to install one.
    ///
    /// Building the bundle needs the Unity Editor at the exact version RimWorld runs
    /// (2022.3.35f1 for 1.6). Until one is present this finds nothing and the mod falls
    /// back to fonts installed on the machine; adding the bundle later needs no code
    /// change. Naming: no extension, optional "_win"/"_mac"/"_linux" suffix.
    /// </summary>
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
            return byName.TryGetValue(family, out Font font) ? font : null;
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

                    // fontNames[0] is the family Unity reports; prefer it over the asset
                    // name so the picker shows the same string as an OS-installed font.
                    string family = font.fontNames != null && font.fontNames.Length > 0
                        ? font.fontNames[0]
                        : font.name;

                    byName[family] = font;
                }
            }

            if (byName.Count == 0)
            {
                return;
            }

            // The family names matter, not just the count: they are the keys the
            // picker and the saved fontName are matched against, and Unity decides
            // them - "Amaranth" and "Amaranth Regular" would behave very differently.
            var names = new List<string>(byName.Keys);
            names.Sort(System.StringComparer.OrdinalIgnoreCase);
            Log.Message(
                "[LizarbInterface] " + byName.Count + " font(s) loaded from AssetBundle: " +
                string.Join(", ", names.ToArray()));
        }
    }
}

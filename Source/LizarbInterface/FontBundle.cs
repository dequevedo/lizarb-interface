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

                    string family = font.fontNames != null && font.fontNames.Length > 0
                        ? font.fontNames[0]
                        : font.name;

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

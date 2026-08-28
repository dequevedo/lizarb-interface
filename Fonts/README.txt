Source .ttf files for the font AssetBundle. NOTHING READS THIS FOLDER AT RUNTIME.

HOW THE MOD FINDS FONTS

  1. AssetBundles/lizarbinterface_fonts_win - built from this folder. The only
     route that needs nothing from the player, and the one every shipped font
     travels.
  2. Fonts installed on the machine, by family name. A fallback for faces the
     bundle does not carry.
  3. The game's own font, if neither has the name.

ADDING A FONT

  1. drop the .ttf here, with its licence file
  2. run tools\Make-FontBundle.ps1
  3. add the family name to Shortlist in Source/LizarbInterface/FontEngine.cs
     if it should appear in the curated list

  The family name is decided by Unity, not by the file name, and it is what the
  picker and the saved setting are matched on. The startup log prints the exact
  names the bundle yielded:

    [LizarbInterface] 15 font(s) loaded from AssetBundle: Alegreya, Amaranth, ...

  Read them from there rather than guessing from the file name.

LICENCE: these fonts are from Google Fonts under the SIL Open Font License, NOT
under this mod's GPL-3.0. Each one has its OFL-*.txt alongside it. They are
redistributed both here and inside the AssetBundle, so both must keep shipping
with the mod.

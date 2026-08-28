RimWorld loads any AssetBundle it finds in this folder
(Verse.ModAssetBundlesHandler + GenFilePaths.ContentPath<AssetBundle>).

FILE RULES
  - NO extension
  - optional platform suffix: _win, _mac, _linux
  - no suffix = loaded on every platform

WHAT IS HERE
  lizarbinterface_fonts_win   the 15 fonts from Fonts/, so no player has to
                              install anything

  The _win suffix is deliberate. AssetBundles are platform-specific, and this
  one is built for StandaloneWindows64; without the suffix RimWorld would offer
  it to macOS and Linux players, where it would fail to load.

REBUILDING IT

  .\tools\Make-FontBundle.ps1

  That script stages the fonts, writes the Editor build script, runs Unity
  headless and copies the result here. Re-run it whenever Fonts/ changes.

  It needs the Unity Editor at EXACTLY the version RimWorld was built with
  (2022.3.35f1 for 1.6). A bundle from any other version fails to load, and it
  fails SILENTLY - the mod just reports no bundled fonts. The script reads the
  required version back out of the game's globalgamemanagers rather than
  hardcoding it, so a RimWorld update becomes a loud error instead of a quiet
  one.

VERIFYING
  Start the game and look for this in Player.log:
      [LizarbInterface] 15 font(s) loaded from AssetBundle.
  The fonts then appear in Settings > Text > Font with no install step.

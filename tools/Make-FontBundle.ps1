<#
  Builds the AssetBundle that ships the mod's fonts, so no player has to install
  anything. Everything here is scripted rather than done through the Editor GUI:
  the bundle has to be rebuilt whenever the font set changes, and a documented
  click-path rots in a way a script does not.

  REQUIRES the Unity Editor at EXACTLY the version RimWorld was built with. A
  bundle from any other version fails to load, and it fails SILENTLY - the mod
  simply reports no bundled fonts. The version is read back from the game below
  rather than hardcoded, so a RimWorld update turns this into a loud error
  instead of a silent one.

  Usage:
    .\tools\Make-FontBundle.ps1
    .\tools\Make-FontBundle.ps1 -Clean     # discard the scratch project first
#>
[CmdletBinding()]
param(
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'

$Repo     = Split-Path $PSScriptRoot -Parent
$FontDir  = Join-Path $Repo 'Fonts'
$OutDir   = Join-Path $Repo 'AssetBundles'
$Proj     = Join-Path $Repo 'dev\unity-fonts'
$GameDir  = if ($env:RIMWORLD_DIR) { $env:RIMWORLD_DIR } else { 'D:\Steam\steamapps\common\RimWorld' }

# Windows-only bundle, hence the _win suffix RimWorld understands. AssetBundles
# are platform-specific; a no-suffix name would be offered to macOS and Linux
# players too and fail there.
$BundleName = 'lizarbinterface_fonts_win'

# ---------------------------------------------------------------------------
# The Unity version must match the game's, exactly.
# ---------------------------------------------------------------------------
$ggm = Join-Path $GameDir 'RimWorldWin64_Data\globalgamemanagers'
if (-not (Test-Path $ggm)) { throw "RimWorld not found at $GameDir (set `$env:RIMWORLD_DIR)" }

$bytes = [IO.File]::ReadAllBytes($ggm)[0..2047]
$text = -join ($bytes | ForEach-Object { if ($_ -ge 32 -and $_ -lt 127) { [char]$_ } else { ' ' } })
if ($text -notmatch '(\d{4}\.\d+\.\d+[a-z]\d+)') { throw 'could not read the Unity version from globalgamemanagers' }
$Version = $Matches[1]
Write-Host "RimWorld was built with Unity $Version" -ForegroundColor Cyan

$Editor = "C:\Program Files\Unity\Hub\Editor\$Version\Editor\Unity.exe"
if (-not (Test-Path $Editor)) {
    throw "Unity $Version is not installed at $Editor. Install exactly that version from Unity Hub."
}

# ---------------------------------------------------------------------------
# Scratch project. Regenerated from this script, so it is gitignored and safe
# to delete; only the Library folder is expensive to rebuild.
# ---------------------------------------------------------------------------
if ($Clean -and (Test-Path $Proj)) {
    Write-Host "removing $Proj" -ForegroundColor DarkGray
    Remove-Item -Recurse -Force $Proj
}

$assetFonts = Join-Path $Proj 'Assets\Fonts'
$assetEditor = Join-Path $Proj 'Assets\Editor'
foreach ($d in @($assetFonts, $assetEditor, $OutDir)) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}

$ttf = @(Get-ChildItem $FontDir -Filter *.ttf)
if ($ttf.Count -eq 0) { throw "no .ttf in $FontDir" }
foreach ($f in $ttf) { Copy-Item $f.FullName (Join-Path $assetFonts $f.Name) -Force }
Write-Host "$($ttf.Count) font(s) staged" -ForegroundColor DarkGray

# The build script lives in the scratch project, so it is written fresh here
# rather than kept as a second source of truth.
$builder = @'
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Invoked by Unity in batch mode. Any exception must set a non-zero exit code,
// or the calling script would treat a failed build as a success.
public static class BuildFontBundle
{
    public static void Build()
    {
        try
        {
            string name = System.Environment.GetEnvironmentVariable("LZ_BUNDLE_NAME");
            string outDir = System.Environment.GetEnvironmentVariable("LZ_BUNDLE_OUT");
            Directory.CreateDirectory(outDir);

            string[] assets = AssetDatabase.FindAssets("t:Font", new[] { "Assets/Fonts" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .OrderBy(p => p)
                .ToArray();

            if (assets.Length == 0)
            {
                Debug.LogError("LZ: no Font assets found under Assets/Fonts");
                EditorApplication.Exit(2);
                return;
            }

            foreach (string a in assets) { Debug.Log("LZ: packing " + a); }

            var build = new AssetBundleBuild { assetBundleName = name, assetNames = assets };

            // LZ4: RimWorld loads these at startup and chunk compression decodes
            // far faster than LZMA for a marginal size increase.
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                outDir, new[] { build },
                BuildAssetBundleOptions.ChunkBasedCompression,
                BuildTarget.StandaloneWindows64);

            if (manifest == null)
            {
                Debug.LogError("LZ: BuildAssetBundles returned null");
                EditorApplication.Exit(3);
                return;
            }

            Debug.Log("LZ: built " + name + " with " + assets.Length + " font(s)");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError("LZ: " + e);
            EditorApplication.Exit(4);
        }
    }
}
'@
[IO.File]::WriteAllText((Join-Path $assetEditor 'BuildFontBundle.cs'), $builder, (New-Object Text.UTF8Encoding($false)))

# ---------------------------------------------------------------------------
# Run the Editor headless.
# ---------------------------------------------------------------------------
$stage = Join-Path $Proj 'BundleOut'
$log   = Join-Path $Proj 'build.log'
$env:LZ_BUNDLE_NAME = $BundleName
$env:LZ_BUNDLE_OUT  = $stage

Write-Host "running Unity (first run imports the fonts and can take a few minutes)..." -ForegroundColor Yellow
$args = @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', $Proj,
    '-executeMethod', 'BuildFontBundle.Build',
    '-logFile', $log
)
$p = Start-Process $Editor -ArgumentList $args -PassThru -Wait -NoNewWindow

if ($p.ExitCode -ne 0) {
    Write-Host ""
    Write-Host "Unity exited $($p.ExitCode). Last lines of $log :" -ForegroundColor Red
    if (Test-Path $log) { Get-Content $log -Tail 30 | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray } }
    Write-Host ""
    Write-Host "A licensing error here means Unity Hub has never signed in on this machine." -ForegroundColor Yellow
    throw "bundle build failed"
}

# ---------------------------------------------------------------------------
# Ship it. Only the extensionless bundle goes to the mod: Unity also writes
# .manifest files and a bundle named after the output folder, and RimWorld
# would ignore the first and load the second for nothing.
# ---------------------------------------------------------------------------
$built = Join-Path $stage $BundleName
if (-not (Test-Path $built)) { throw "Unity reported success but $built is missing" }

Copy-Item $built (Join-Path $OutDir $BundleName) -Force
$size = [Math]::Round((Get-Item $built).Length / 1MB, 2)

Write-Host ""
Write-Host "OK - AssetBundles\$BundleName  ($size MB, $($ttf.Count) fonts)" -ForegroundColor Green
Write-Host "Restart RimWorld; the log should say 'font(s) loaded from AssetBundle'." -ForegroundColor DarkGray

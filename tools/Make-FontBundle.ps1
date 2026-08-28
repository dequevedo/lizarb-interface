[CmdletBinding()]
param(
    [ValidateSet('all', 'win', 'mac', 'linux')]
    [string[]]$Platform = @('all'),
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'

$Repo     = Split-Path $PSScriptRoot -Parent
$FontDir  = Join-Path $Repo 'Fonts'
$OutDir   = Join-Path $Repo 'AssetBundles'
$Proj     = Join-Path $Repo 'dev\unity-fonts'
$GameDir  = if ($env:RIMWORLD_DIR) { $env:RIMWORLD_DIR } else { 'D:\Steam\steamapps\common\RimWorld' }

$BaseName = 'lizarbinterface_fonts'

$Targets = [ordered]@{
    win   = @{ Target = 'StandaloneWindows64'; Suffix = '_win';   Engine = 'windowsstandalonesupport'; Module = 'Windows Build Support' }
    mac   = @{ Target = 'StandaloneOSX';       Suffix = '_mac';   Engine = 'MacStandaloneSupport';     Module = 'Mac Build Support (Mono)' }
    linux = @{ Target = 'StandaloneLinux64';   Suffix = '_linux'; Engine = 'LinuxStandaloneSupport';   Module = 'Linux Build Support (Mono)' }
}

$wanted = if ($Platform -contains 'all') { @($Targets.Keys) } else { @($Platform | Select-Object -Unique) }

$ggm = Join-Path $GameDir 'RimWorldWin64_Data\globalgamemanagers'
if (-not (Test-Path $ggm)) { throw "RimWorld not found at $GameDir (set `$env:RIMWORLD_DIR)" }

$bytes = [IO.File]::ReadAllBytes($ggm)[0..2047]
$text = -join ($bytes | ForEach-Object { if ($_ -ge 32 -and $_ -lt 127) { [char]$_ } else { ' ' } })
if ($text -notmatch '(\d{4}\.\d+\.\d+[a-z]\d+)') { throw 'could not read the Unity version from globalgamemanagers' }
$Version = $Matches[1]
Write-Host "RimWorld was built with Unity $Version" -ForegroundColor Cyan

$EditorDir = "C:\Program Files\Unity\Hub\Editor\$Version\Editor"
$Editor = Join-Path $EditorDir 'Unity.exe'
if (-not (Test-Path $Editor)) {
    throw "Unity $Version is not installed at $Editor. Install exactly that version from Unity Hub."
}

$build = @()
$skipped = @()
foreach ($key in $wanted) {
    $t = $Targets[$key]
    $engine = Join-Path $EditorDir "Data\PlaybackEngines\$($t.Engine)"
    if (Test-Path $engine) {
        $build += , @($key, $t)
    } else {
        $skipped += "$key ($($t.Module))"
    }
}

if ($skipped.Count -gt 0) {
    Write-Host ""
    Write-Host "SKIPPING $($skipped -join ', ')" -ForegroundColor Yellow
    Write-Host "Unity Hub > Installs > $Version > Add modules, then run again." -ForegroundColor DarkGray
    Write-Host ""
}

if ($build.Count -eq 0) { throw 'no build target available' }

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

$builder = @'
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildFontBundle
{
    public static void Build()
    {
        try
        {
            string outRoot = System.Environment.GetEnvironmentVariable("LZ_BUNDLE_OUT");
            string spec = System.Environment.GetEnvironmentVariable("LZ_BUNDLE_TARGETS");

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

            foreach (string entry in spec.Split(';'))
            {
                if (entry.Length == 0) { continue; }

                string[] parts = entry.Split('=');
                var target = (BuildTarget)System.Enum.Parse(typeof(BuildTarget), parts[0]);
                string name = parts[1];
                string outDir = Path.Combine(outRoot, parts[0]);
                Directory.CreateDirectory(outDir);

                var bundle = new AssetBundleBuild { assetBundleName = name, assetNames = assets };

                AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                    outDir, new[] { bundle },
                    BuildAssetBundleOptions.ChunkBasedCompression,
                    target);

                if (manifest == null)
                {
                    Debug.LogError("LZ: BuildAssetBundles returned null for " + target);
                    EditorApplication.Exit(3);
                    return;
                }

                Debug.Log("LZ: built " + name + " for " + target + " with " + assets.Length + " font(s)");
            }

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

$stage = Join-Path $Proj 'BundleOut'
$log   = Join-Path $Proj 'build.log'
$env:LZ_BUNDLE_OUT = $stage
$env:LZ_BUNDLE_TARGETS = (($build | ForEach-Object { "$($_[1].Target)=$BaseName$($_[1].Suffix)" }) -join ';')

Write-Host "building for: $(($build | ForEach-Object { $_[0] }) -join ', ')" -ForegroundColor Cyan
Write-Host "running Unity (first run imports the fonts and can take a few minutes)..." -ForegroundColor Yellow
$unityArgs = @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', $Proj,
    '-executeMethod', 'BuildFontBundle.Build',
    '-logFile', $log
)
$p = Start-Process $Editor -ArgumentList $unityArgs -PassThru -Wait -NoNewWindow

if ($p.ExitCode -ne 0) {
    Write-Host ""
    Write-Host "Unity exited $($p.ExitCode). Last lines of $log :" -ForegroundColor Red
    if (Test-Path $log) { Get-Content $log -Tail 30 | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray } }
    Write-Host ""
    Write-Host "A licensing error here means Unity Hub has never signed in on this machine." -ForegroundColor Yellow
    throw "bundle build failed"
}

Write-Host ""
foreach ($b in $build) {
    $name = "$BaseName$($b[1].Suffix)"
    $built = Join-Path (Join-Path $stage $b[1].Target) $name
    if (-not (Test-Path $built)) { throw "Unity reported success but $built is missing" }

    Copy-Item $built (Join-Path $OutDir $name) -Force
    $size = [Math]::Round((Get-Item $built).Length / 1MB, 2)
    Write-Host ("OK: AssetBundles\{0}  ({1} MB, {2} fonts)" -f $name, $size, $ttf.Count) -ForegroundColor Green
}

if ($skipped.Count -gt 0) {
    Write-Host ""
    Write-Host "NOT built: $($skipped -join ', ')" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Restart RimWorld; the log should say 'font(s) loaded from AssetBundle'." -ForegroundColor DarkGray

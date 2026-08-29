[CmdletBinding()]
param(
    [string]$Out,
    [switch]$SkipBuild,
    [switch]$Link,
    [switch]$Dev,
    [switch]$NoBump
)

$ErrorActionPreference = 'Stop'

$Repo    = Split-Path $PSScriptRoot -Parent
$ModName = 'LizarbInterface'
$Dist    = if ($Out) { $Out } else { Join-Path $Repo "dist\$ModName" }
$GameDir = if ($env:RIMWORLD_DIR) { $env:RIMWORLD_DIR } else { 'D:\Steam\steamapps\common\RimWorld' }

$Dirs = @('About', 'Assemblies', 'Patches', 'Languages', 'Skins')
$Files = @('LICENSE', 'README.md')
$SourceExclude = @('bin', 'obj')

function Set-ModLink {
    param([string]$Target)

    $link = Join-Path $GameDir "Mods\$ModName"
    if (Test-Path $link) {
        $item = Get-Item $link -Force
        if (-not $item.LinkType) {
            throw "$link exists and is a real directory, not a link. remove it yourself"
        }
        [IO.Directory]::Delete($link, $false)
    }

    New-Item -ItemType Junction -Path $link -Target $Target | Out-Null
    Write-Host "$link -> $Target" -ForegroundColor Green
    Write-Host "RimWorld must be closed for the junction to be picked up." -ForegroundColor DarkGray
}

if ($Dev) {
    Set-ModLink -Target $Repo
    Write-Host "back on the working tree; edits show up in game again." -ForegroundColor DarkGray
    return
}

if (-not $SkipBuild) {
    $csproj = Join-Path $Repo "Source\$ModName\$ModName.csproj"
    Write-Host "building $ModName..." -ForegroundColor Cyan
    & dotnet build -c Release $csproj -v q --nologo
    if ($LASTEXITCODE -ne 0) { throw 'build failed' }
}

$dll = Join-Path $Repo "Assemblies\$ModName.dll"
if (-not (Test-Path $dll)) { throw "missing $dll" }

$aboutPath = Join-Path $Repo 'About\About.xml'
$about = Get-Content $aboutPath -Raw
if ($about -notmatch '<modVersion>(\d+)\.(\d+)\.(\d+)</modVersion>') {
    throw "no <modVersion>major.minor.patch</modVersion> in $aboutPath"
}

$was = "$($Matches[1]).$($Matches[2]).$($Matches[3])"
$version = $was

if ($NoBump) {
    Write-Host "version $version, kept" -ForegroundColor DarkGray
} else {
    $version = "$($Matches[1]).$([int]$Matches[2] + 1).0"
    $bumped = $about -replace '<modVersion>[^<]*</modVersion>', "<modVersion>$version</modVersion>"
    [IO.File]::WriteAllText($aboutPath, $bumped, (New-Object Text.UTF8Encoding $false))
    Write-Host "version $was -> $version. run with -NoBump to keep it" -ForegroundColor Cyan
}

$keepId = $null
$idPath = Join-Path $Dist 'About\PublishedFileId.txt'
if (Test-Path $idPath) {
    $keepId = Get-Content $idPath -Raw
    Write-Host "carrying over PublishedFileId $($keepId.Trim())" -ForegroundColor DarkGray
}

if (Test-Path $Dist) { Remove-Item -Recurse -Force $Dist }
New-Item -ItemType Directory -Path $Dist -Force | Out-Null

foreach ($d in $Dirs) {
    $src = Join-Path $Repo $d
    if (-not (Test-Path $src)) { throw "missing $d" }
    Copy-Item $src (Join-Path $Dist $d) -Recurse -Force
}

foreach ($f in $Files) {
    $src = Join-Path $Repo $f
    if (Test-Path $src) { Copy-Item $src (Join-Path $Dist $f) -Force }
}

$srcOut = Join-Path $Dist 'Source'
Copy-Item (Join-Path $Repo 'Source') $srcOut -Recurse -Force
foreach ($junk in $SourceExclude) {
    Get-ChildItem $srcOut -Recurse -Directory -Filter $junk -ErrorAction SilentlyContinue |
        ForEach-Object { Remove-Item $_.FullName -Recurse -Force }
}

$licenceDir = Join-Path $Dist 'Fonts'
New-Item -ItemType Directory -Path $licenceDir -Force | Out-Null
$ofl = @(Get-ChildItem (Join-Path $Repo 'Fonts') -Filter 'OFL-*.txt')
if ($ofl.Count -eq 0) { throw 'no OFL-*.txt found; the bundle redistributes the fonts and the licences must ship' }
foreach ($f in $ofl) { Copy-Item $f.FullName (Join-Path $licenceDir $f.Name) -Force }

$ttfCount = @(Get-ChildItem (Join-Path $Repo 'Fonts') -Filter *.ttf).Count
if ($ofl.Count -ne $ttfCount) {
    Write-Host "WARNING: $ttfCount .ttf but $($ofl.Count) OFL-*.txt. A font may be shipping unlicensed" -ForegroundColor Red
}

$rootId = Join-Path $Repo 'About\PublishedFileId.txt'
if ($keepId) {
    Set-Content -Path $idPath -Value $keepId.Trim() -Encoding ascii -NoNewline
} elseif (Test-Path $rootId) {
    Copy-Item $rootId $idPath -Force
    Write-Host "PublishedFileId taken from the repo" -ForegroundColor DarkGray
}

$bundleOut = Join-Path $Dist 'AssetBundles'
New-Item -ItemType Directory -Path $bundleOut -Force | Out-Null
$bundles = @(Get-ChildItem (Join-Path $Repo 'AssetBundles') -File |
             Where-Object { -not $_.Extension })
if ($bundles.Count -eq 0) { throw 'no AssetBundle found; run tools\Make-FontBundle.ps1 first' }
foreach ($b in $bundles) { Copy-Item $b.FullName (Join-Path $bundleOut $b.Name) -Force }

$absent = @('_win', '_mac', '_linux') |
          Where-Object { $p = $_; -not @($bundles | Where-Object { $_.Name.EndsWith($p) }) }
if ($absent.Count -gt 0) {
    $names = ($absent | ForEach-Object { $_.TrimStart('_') }) -join ', '
    Write-Host "WARNING: no font bundle for $names. Those players fall back to OS fonts" -ForegroundColor Yellow
}

$size = [Math]::Round((Get-ChildItem $Dist -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
$count = @(Get-ChildItem $Dist -Recurse -File).Count

Write-Host ""
Write-Host "OK: $Dist" -ForegroundColor Green
Write-Host "     $count files, $size MB" -ForegroundColor DarkGray
Write-Host ""

foreach ($d in (@($Dirs) + 'AssetBundles' + 'Source' + 'Fonts')) {
    $n = @(Get-ChildItem (Join-Path $Dist $d) -Recurse -File).Count
    Write-Host ("  {0,-14} {1} file(s)" -f $d, $n) -ForegroundColor DarkGray
}

if ($Link) {
    Write-Host ""
    Set-ModLink -Target $Dist
    Write-Host "run this script with -Dev to point it back at the working tree." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "To publish: tools\Make-Release.ps1 -Link, then the in-game Mods screen." -ForegroundColor DarkGray
Write-Host "The first upload writes About\PublishedFileId.txt inside dist. Copy it back" -ForegroundColor DarkGray
Write-Host "into the repo, or the next release creates a second Workshop item." -ForegroundColor DarkGray

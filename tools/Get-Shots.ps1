[CmdletBinding()]
param(
    [switch]$Keep
)

$ErrorActionPreference = 'Stop'

$Repo = Split-Path $PSScriptRoot -Parent
$Mod  = Join-Path $Repo 'Source\LizarbInterface\LizarbInterfaceMod.cs'
$Dest = Join-Path $Repo 'docs\shots'

$src = Get-Content $Mod -Raw
$themes = @([regex]::Matches($src, '\("([A-Za-z]+)",\s*"([A-Za-z]+)",\s*new Color') |
            ForEach-Object { $_.Groups[1].Value })
if ($themes.Count -eq 0) { throw "no theme tuples found in $Mod" }

$roots = @(
    (Join-Path $Repo 'dev\profile\Screenshots\LizarbThemes'),
    (Join-Path $Repo 'dev\profile-conflict\Screenshots\LizarbThemes'),
    (Join-Path $env:USERPROFILE 'AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Screenshots\LizarbThemes')
)

$found = @($roots | Where-Object { Test-Path $_ } |
           ForEach-Object { [pscustomobject]@{
               Path = $_
               Shots = @(Get-ChildItem $_ -Filter *.png -File)
           } } | Where-Object { $_.Shots.Count -gt 0 } |
           Sort-Object { ($_.Shots | Measure-Object LastWriteTime -Maximum).Maximum } -Descending)

if ($found.Count -eq 0) {
    Write-Host "no shots found. run the debug action 'Lizarb Interface > Shoot every theme' first." -ForegroundColor Yellow
    Write-Host "looked in:" -ForegroundColor DarkGray
    foreach ($r in $roots) { Write-Host "  $r" -ForegroundColor DarkGray }
    return
}

$from = $found[0]

if (-not $Keep -and (Test-Path $Dest)) { Remove-Item (Join-Path $Dest '*.png') -Force }
New-Item -ItemType Directory -Path $Dest -Force | Out-Null

foreach ($s in $from.Shots) { Copy-Item $s.FullName (Join-Path $Dest $s.Name) -Force }

$copied = @(Get-ChildItem $Dest -Filter *.png -File)
$size = [Math]::Round(($copied | Measure-Object -Property Length -Sum).Sum / 1MB, 1)

Write-Host ""
Write-Host "OK: $Dest" -ForegroundColor Green
Write-Host "     $($copied.Count) shot(s), $size MB, from $($from.Path)" -ForegroundColor DarkGray
Write-Host ""

$missing = @($themes | Where-Object { $t = $_; -not @($copied | Where-Object { $_.BaseName -match "-$t`$" }) })
if ($missing.Count -gt 0) {
    Write-Host "WARNING: no shot for $($missing -join ', ')" -ForegroundColor Red
} else {
    Write-Host "every one of the $($themes.Count) themes has a shot." -ForegroundColor DarkGray
}

<#
  Launches RimWorld with an isolated DEVELOPMENT profile.

  Uses the game's -savedatafolder argument, so Config, Saves and Prefs come from
  dev\profile\ instead of AppData. The normal profile is never touched: nothing is
  swapped, copied or overwritten there.

  Usage:
    .\dev\rw-dev.ps1            # Core + Harmony + whatever is in $ExtraMods
    .\dev\rw-dev.ps1 -Dlcs      # the same, plus every DLC
    .\dev\rw-dev.ps1 -EditMods  # open the dev profile's ModsConfig in an editor

  Note: Player.log is NOT redirected. Unity writes it to persistentDataPath, which
  this argument does not change, so it stays under AppData\LocalLow.
#>
param(
    [switch]$Dlcs,
    [switch]$EditMods
)

$ErrorActionPreference = 'Stop'

# Override with $env:RIMWORLD_DIR if the game is not on this path.
$GameDir     = if ($env:RIMWORLD_DIR) { $env:RIMWORLD_DIR } else { 'D:\Steam\steamapps\common\RimWorld' }
$GameExe     = Join-Path $GameDir 'RimWorldWin64.exe'
$ProfileDir  = Join-Path $PSScriptRoot 'profile'
$ModsConfig  = Join-Path $ProfileDir 'Config\ModsConfig.xml'

# The mod under development plus any debug tooling. Order is load order.
$ExtraMods = @(
    'com.bymarcin.architecticons'   # exercise the compatibility path as well
    'lizarb.interface'              # last: wins the UI texture contest
)

$Dlc = @(
    'ludeon.rimworld.royalty'
    'ludeon.rimworld.ideology'
    'ludeon.rimworld.biotech'
    'ludeon.rimworld.anomaly'
    'ludeon.rimworld.odyssey'
)

if ($EditMods) { Start-Process notepad.exe $ModsConfig; return }

# Rewrite the dev profile's active mod list.
$active = @('ludeon.rimworld')
if ($Dlcs) { $active += $Dlc }
$active += 'brrainz.harmony'
$active += $ExtraMods

$xml = [xml](Get-Content $ModsConfig -Raw)
$node = $xml.ModsConfigData.activeMods
$node.RemoveAll()
foreach ($id in $active) {
    $li = $xml.CreateElement('li')
    $li.InnerText = $id
    [void]$node.AppendChild($li)
}
$xml.Save($ModsConfig)

Write-Host "Active mods ($($active.Count)): $($active -join ', ')" -ForegroundColor Cyan
Write-Host "Profile: $ProfileDir" -ForegroundColor DarkGray

$p = Start-Process $GameExe -ArgumentList "-savedatafolder=$ProfileDir" -PassThru
Write-Host "RimWorld started (PID $($p.Id))." -ForegroundColor Green

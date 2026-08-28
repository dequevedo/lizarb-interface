param(
    [switch]$Dlcs,
    [switch]$EditMods
)

$ErrorActionPreference = 'Stop'

$GameDir     = if ($env:RIMWORLD_DIR) { $env:RIMWORLD_DIR } else { 'D:\Steam\steamapps\common\RimWorld' }
$GameExe     = Join-Path $GameDir 'RimWorldWin64.exe'
$ProfileDir  = Join-Path $PSScriptRoot 'profile'
$ModsConfig  = Join-Path $ProfileDir 'Config\ModsConfig.xml'

$ExtraMods = @(
    'com.bymarcin.architecticons'
    'lizarb.interface'
)

$Dlc = @(
    'ludeon.rimworld.royalty'
    'ludeon.rimworld.ideology'
    'ludeon.rimworld.biotech'
    'ludeon.rimworld.anomaly'
    'ludeon.rimworld.odyssey'
)

if ($EditMods) { Start-Process notepad.exe $ModsConfig; return }

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

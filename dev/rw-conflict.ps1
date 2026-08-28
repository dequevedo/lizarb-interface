[CmdletBinding()]
param(
    [switch]$ListOnly,
    [switch]$NoDlcs,
    [switch]$Quick
)

$ErrorActionPreference = 'Stop'

$GameDir    = if ($env:RIMWORLD_DIR) { $env:RIMWORLD_DIR } else { 'D:\Steam\steamapps\common\RimWorld' }
$GameExe    = Join-Path $GameDir 'RimWorldWin64.exe'
$Workshop   = if ($env:RIMWORLD_WORKSHOP) { $env:RIMWORLD_WORKSHOP } else { 'D:\Steam\steamapps\workshop\content\294100' }
$ProfileDir = Join-Path $PSScriptRoot 'profile-conflict'
$Seed       = Join-Path $PSScriptRoot 'profile'

$Suspects = @(
    'com.bymarcin.ArchitectIcons'
    'ODs.IsekaiRetexture'
    'JellyCreative.IsekaiLeveling'
    'Poupun.GuildFactionAddon'

    'Andromeda.NiceHealthTab'
    'Andromeda.NiceBillTab'
    'Andromeda.NiceInventoryTab'
    'Andromeda.NiceResearchTab'
    'Owlchemist.PowerTab.forked.RunningBugs'
    'PeteTimesSix.ResearchReinvented'

    'Jaxe.RimHUD'
    'zeracronius.dynamictradeinterface'
    'shunter.bettertradersguild'
    'dubwise.dubsmintminimap'
    'm00nl1ght.MapPreview'
    'bodlosh.WeaponStats'
    'vanillaexpanded.achievements'

    'DawnsGlow.qualcolor'
    'Telefonmast.GraphicsSettings'
    'ferny.PerspectiveShift'
    'Krafs.LevelUp'
    'Deadmano.MoodAlerts'

    'PeteTimesSix.SimpleSidearms'
    'MemeGoddess.RunAndGun'
    'trigger.universalblueprints'
    'Owlchemist.ToggleableOverlays'

    'OskarPotocki.VanillaFactionsExpanded.Core'
    'UnlimitedHugs.HugsLib'
    'VanillaExpanded.VPsycastsE'
    'Dubwise.DubsPerformanceAnalyzer.steam'
)

$SuspectsWide = @(
    'SmashPhil.VehicleFramework'
    'JAHV.SpacerVehiclesHAL.CONTINUED'
    'Aoba.Exosuit.Framework'
    'OELS.VehicleMapFramework'
    'OskarPotocki.VanillaVehiclesExpanded'

    'AOBA.Framework'
    'EBSG.Framework'
    'RedMattis.BetterPrerequisites'
    'adaptive.storage.framework'
    'imranfish.xmlextensions'
    'erdelf.HumanoidAlienRaces'
    'NozoMe.MapModeFramework'
    'RimThunder.Core'
    'Aoba.DeadManSwitch.Core'

    '6224Y.OneWithDeath'
    'VanillaStorytellersExpanded.WinstonWave'
    'Orion.Hospitality'
    'CP.Rimdeed'
    'DerekBickley.LTOColonyGroupsFinal'
    'Andromeda.UsefulMarks'
    'Andromeda.MilkyWay'
    'jaeger972.factionterritories'
    'ilyvion.LoadingProgress'
    'VanillaExpanded.VanillaTradingExpanded'
    'Orion.CashRegister'

    'vanillaexpanded.gravship'
    'spdskatr.projectrimfactory'
    'VanillaExpanded.VFEPropsandDecor'

    'CrashM.ColorCodedMoodBar.11'
    'Owlchemist.ToggleableReadouts'
    'Dark.Signs'
    'ferny.ResourceDeliveryHelper'
    'DawnsGlow.Numbers'
    'falconne.LabelsOnFloor'
    'telardo.DragSelect'
    'ferny.ProgressionEducation'

    'OskarPotocki.VFE.Deserters'
    'OskarPotocki.VFE.Empire'
    'OskarPotocki.VFE.Medieval2'
    'OskarPotocki.VFE.Insectoid2'
    'VanillaExpanded.VFECore'
    'VanillaExpanded.VAPPE'
    'VanillaExpanded.VHE'
    'VanillaExpanded.VanillaAspirationsExpanded'
    'vanillaracesexpanded.android'
    'vanillaexpanded.skills'
    'wuhuansuiyue.defensivenetworkexpanded'
    'm00nl1ght.GeologicalLandforms'
)

if (-not $Quick) { $Suspects = $Suspects + $SuspectsWide }

$Ours = @('lizarb.interface')

$Core = 'ludeon.rimworld'
$Dlc  = @(
    'ludeon.rimworld.royalty'
    'ludeon.rimworld.ideology'
    'ludeon.rimworld.biotech'
    'ludeon.rimworld.anomaly'
    'ludeon.rimworld.odyssey'
)

function Read-Mods([string[]]$Roots) {
    $map = @{}
    foreach ($root in $Roots) {
        if (-not (Test-Path $root)) { continue }
        foreach ($dir in Get-ChildItem $root -Directory -ErrorAction SilentlyContinue) {
            $about = Join-Path $dir.FullName 'About\About.xml'
            if (-not (Test-Path $about)) {
                $hit = Get-ChildItem $dir.FullName -Recurse -Filter About.xml -ErrorAction SilentlyContinue |
                       Where-Object { $_.Directory.Name -eq 'About' } | Select-Object -First 1
                if (-not $hit) { continue }
                $about = $hit.FullName
            }
            try { [xml]$x = Get-Content -Raw -LiteralPath $about } catch { continue }
            $m = $x.ModMetaData
            if (-not $m -or -not $m.packageId) { continue }

            $id = ([string]$m.packageId).Trim()
            $key = $id.ToLowerInvariant()
            if ($map.ContainsKey($key)) { continue }

            $deps = @()
            if ($m.modDependencies) {
                foreach ($li in $m.modDependencies.li) {
                    if ($li.packageId) { $deps += ([string]$li.packageId).Trim().ToLowerInvariant() }
                }
            }
            $after = @()
            foreach ($field in 'loadAfter', 'forceLoadAfter') {
                if ($m.$field) { foreach ($li in $m.$field.li) { $after += ([string]$li).Trim().ToLowerInvariant() } }
            }
            $before = @()
            foreach ($field in 'loadBefore', 'forceLoadBefore') {
                if ($m.$field) { foreach ($li in $m.$field.li) { $before += ([string]$li).Trim().ToLowerInvariant() } }
            }

            $nameNode = $m.SelectSingleNode('name')
            $name = if ($nameNode) { $nameNode.InnerText.Trim() } else { $id }

            $map[$key] = [pscustomobject]@{
                Id = $id; Name = $name
                Deps = $deps; After = $after; Before = $before
            }
        }
    }
    return $map
}

$mods = Read-Mods @(
    (Join-Path $GameDir 'Data'),
    (Join-Path $GameDir 'Mods'),
    $Workshop
)

Write-Host "$($mods.Count) mods indexed." -ForegroundColor DarkGray

$wanted = New-Object System.Collections.Generic.HashSet[string]
$seedKeys = New-Object System.Collections.Generic.HashSet[string]
foreach ($id in (@($Core) + $Dlc + $Suspects + $Ours)) { [void]$seedKeys.Add($id.ToLowerInvariant()) }
$queue  = New-Object System.Collections.Generic.Queue[string]
$missing = @()

foreach ($id in (@($Core) + $(if ($NoDlcs) { @() } else { $Dlc }) + $Suspects + $Ours)) {
    [void]$queue.Enqueue($id.ToLowerInvariant())
}

while ($queue.Count -gt 0) {
    $key = $queue.Dequeue()
    if ($wanted.Contains($key)) { continue }
    if (-not $mods.ContainsKey($key)) { $missing += $key; continue }
    [void]$wanted.Add($key)
    foreach ($d in $mods[$key].Deps) { [void]$queue.Enqueue($d) }
}

if ($missing.Count -gt 0) {
    Write-Host "NOT INSTALLED (skipped): $($missing -join ', ')" -ForegroundColor Yellow
}

$baseOrder = @()
foreach ($id in (@($Core) + $(if ($NoDlcs) { @() } else { $Dlc }) + $Suspects + $Ours)) {
    $k = $id.ToLowerInvariant()
    if ($wanted.Contains($k) -and $baseOrder -notcontains $k) { $baseOrder += $k }
}
foreach ($k in $wanted) { if ($baseOrder -notcontains $k) { $baseOrder = @($baseOrder[0]) + $k + $baseOrder[1..($baseOrder.Count - 1)] } }

$rank = @{}
for ($i = 0; $i -lt $baseOrder.Count; $i++) { $rank[$baseOrder[$i]] = $i }

$edges = @{}
$indeg = @{}
foreach ($k in $baseOrder) { $edges[$k] = New-Object System.Collections.Generic.HashSet[string]; $indeg[$k] = 0 }

function Add-Edge($from, $to) {
    if (-not $edges.ContainsKey($from) -or -not $edges.ContainsKey($to)) { return }
    if ($from -eq $to) { return }
    if ($edges[$from].Add($to)) { $indeg[$to] = $indeg[$to] + 1 }
}

foreach ($k in $baseOrder) {
    $m = $mods[$k]
    foreach ($d in $m.Deps)   { Add-Edge $d $k }
    foreach ($a in $m.After)  { Add-Edge $a $k }
    foreach ($b in $m.Before) { Add-Edge $k $b }
}

$coreKey = $Core.ToLowerInvariant()
foreach ($k in $baseOrder) {
    if ($k -eq $coreKey) { continue }
    if ($mods[$k].Before -contains $coreKey) { continue }
    Add-Edge $coreKey $k
}
foreach ($o in $Ours) {
    $ok = $o.ToLowerInvariant()
    foreach ($k in $baseOrder) { if ($k -ne $ok -and $Ours -notcontains $k) { Add-Edge $k $ok } }
}
for ($i = 0; $i -lt $Ours.Count - 1; $i++) {
    Add-Edge ($Ours[$i].ToLowerInvariant()) ($Ours[$i + 1].ToLowerInvariant())
}

$ready = [System.Collections.ArrayList]@($baseOrder | Where-Object { $indeg[$_] -eq 0 })
$order = @()
while ($ready.Count -gt 0) {
    $pick = @($ready | Sort-Object { $rank[$_] })[0]
    [void]$ready.Remove($pick)
    $order += $pick
    foreach ($n in $edges[$pick]) {
        $indeg[$n] = $indeg[$n] - 1
        if ($indeg[$n] -eq 0) { [void]$ready.Add($n) }
    }
}

if ($order.Count -ne $baseOrder.Count) {
    Write-Host "cycle in loadAfter/loadBefore; falling back to the base order." -ForegroundColor Yellow
    $order = $baseOrder
}

Write-Host ""
Write-Host "Resolved order ($($order.Count) mods):" -ForegroundColor Cyan
for ($i = 0; $i -lt $order.Count; $i++) {
    $m = $mods[$order[$i]]
    if ($Ours -contains $order[$i]) { $tag = 'Green'; $mark = '*' }
    elseif ($seedKeys.Contains($order[$i])) { $tag = 'Gray'; $mark = ' ' }
    else { $tag = 'DarkGray'; $mark = '+' }
    Write-Host ("  {0,2}.{1} {2,-46} {3}" -f ($i + 1), $mark, $m.Id, $m.Name) -ForegroundColor $tag
}
Write-Host ""

if ($ListOnly) { return }

$cfgDir = Join-Path $ProfileDir 'Config'
if (-not (Test-Path $cfgDir)) { New-Item -ItemType Directory -Path $cfgDir -Force | Out-Null }

$prefs = Join-Path $cfgDir 'Prefs.xml'
if (-not (Test-Path $prefs)) {
    $src = Join-Path $Seed 'Config\Prefs.xml'
    if (Test-Path $src) { Copy-Item $src $prefs }
}

$version = (Get-Content (Join-Path $GameDir 'Version.txt') -Raw).Trim()
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<?xml version="1.0" ?>')
[void]$sb.AppendLine('<ModsConfigData>')
[void]$sb.AppendLine("  <version>$version</version>")
[void]$sb.AppendLine('  <activeMods>')
foreach ($k in $order) { [void]$sb.AppendLine("    <li>$($mods[$k].Id)</li>") }
[void]$sb.AppendLine('  </activeMods>')
[void]$sb.AppendLine('</ModsConfigData>')
[IO.File]::WriteAllText((Join-Path $cfgDir 'ModsConfig.xml'), $sb.ToString(), (New-Object Text.UTF8Encoding($false)))

Write-Host "Profile: $ProfileDir" -ForegroundColor DarkGray
$p = Start-Process $GameExe -ArgumentList "-savedatafolder=$ProfileDir" -PassThru
Write-Host "RimWorld started (PID $($p.Id))." -ForegroundColor Green
Write-Host ""
Write-Host "Player.log does NOT go to the profile - Unity always writes to persistentDataPath." -ForegroundColor DarkGray
Write-Host "After closing:  .\dev\watch-log.ps1" -ForegroundColor Yellow

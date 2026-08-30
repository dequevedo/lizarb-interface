[CmdletBinding()]
param(
    [switch]$ListOnly,
    [switch]$NoDlcs,
    [switch]$Quick,
    [switch]$Visuals,
    [switch]$Without,
    [ValidateSet("all","architect","astryl","reskins","tabs","text","solo")]
    [string]$Scenario
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

if (-not $Quick -and -not $Visuals -and -not $Scenario) { $Suspects = $Suspects + $SuspectsWide }

$Visual = [ordered]@{
    '1668983184' = 'RimThemes'
    '2916014613' = 'Basic RimThemes Recolours'
    '3338215884' = 'RimThemes: Sci-Fi Dark Mode'
    '3551233435' = 'Medieval Era RimTheme'
    '3661802609' = 'PrettyUI'
    '2588873455' = 'UI Not Included'
    '2978831421' = 'UI Retexture'
    '3141849282' = "Tang's Retexture UI"
    '3649094165' = 'Declutter UI'

    '3563882422' = 'Better Architect Menu'

    '2882372932' = 'CustomUIScales'
    '3231727915' = 'Custom Fonts - Forked'
    '3762441264' = 'Readable Numbers'

    '3786107692' = 'Radius UI Framework'
    '3786131805' = 'Radius UI - Inspector'
    '3786129210' = 'Radius UI - Health Tab'
    '3787806055' = 'Radius UI - Needs Tab'
    '3788278207' = 'Radius UI - Colonist Bar'
    '3786840944' = 'Radius UI - Quest Menu'
    '3786945713' = 'Radius UI - Faction Menu'
    '3788256848' = 'Radius UI - Mission Control'
    '3789670069' = 'Radius UI - Xenotypes'
    '3789690957' = 'Radius UI - Ideology'

    '3740575493' = 'Modern Pawn Tabs'
    '3740688691' = 'Modern Bio Tab'
    '3743615382' = 'Modern Needs Tab'
    '3742906203' = 'Modern Quest Menu'
    '3752932665' = 'Modern Notifications'
    '3762126187' = 'Modern Character Creator'
    '3775737458' = 'Modern Expand Menu'
    '3744201621' = 'True RPG Inventory'

    '3742689571' = 'Sleek Work Schedule Tab'
    '3764537806' = 'Sleek Work Priorities'
    '3752835712' = 'Sleek Research Tab'

    '3328729902' = 'Nice Health Tab'
    '3520130671' = 'Nice Bill Tab'
    '3609897594' = 'Nice Inventory Tab'
    '3773496046' = 'Nice Research Tab'
    '3685058533' = 'Nice Plants Menu'
    '3506573327' = 'Useful Marks'

    '1508850027' = 'RimHUD'
    '1446523594' = 'Dubs Mint Menus'
    '3020706506' = 'Dynamic Trade Interface'
    '3745434121' = 'Yet Another Research Tree'
    '3776660810' = 'Info Card Plus'
    '3769342092' = "EPrime's Readouts"
    '3714704223' = 'RPG Health Bar'
    '3761551912' = 'Better Health Bar'
    '3547971440' = 'RPG Dialog'
    '3749216803' = 'Colonist Bar Dividers'
    '3535481557' = 'Loading Progress'
    '2466336893' = 'Optional Name Display'
    '1516158345' = 'Interaction Bubbles'
}

$Scenarios = [ordered]@{
    'all' = @()

    'architect' = @(
        '1195427067'
        '3563882422'
        '1446523594'
        '2588873455'
    )

    'astryl' = @(
        '3786107692', '3786131805', '3786129210', '3787806055', '3788278207'
        '3786840944', '3786945713', '3788256848', '3789670069', '3789690957'
        '3740575493', '3740688691', '3743615382', '3742906203', '3752932665'
        '3762126187', '3744201621', '3776015553'
    )

    'reskins' = @(
        '1668983184', '2916014613', '3338215884', '3551233435'
        '3661802609', '2978831421', '3141849282', '3649094165'
        '3775737458'
    )

    'tabs' = @(
        '3775737458', '3740575493', '3740688691', '3743615382'
        '3742689571', '3764537806', '3752835712'
        '3328729902', '3520130671', '3609897594', '3773496046'
        '3745434121'
    )

    'text' = @(
        '3231727915', '3762441264', '2882372932', '2466336893', '1516158345'
    )

    'solo' = @()
}

$ScenarioNotes = @{
    'all'       = 'every UI mod in the collection'
    'architect' = 'only what draws the Architect menu'
    'astryl'    = 'only Astryl, the Modern and Radius UI families'
    'reskins'   = 'only the mods that reskin the same surfaces we do'
    'tabs'      = 'only mods that replace a whole tab or window'
    'text'      = 'only fonts, UI scale and label text'
    'solo'      = 'nothing but this mod'
}

function Resolve-Workshop([hashtable]$Wanted) {
    $found = @()
    $absent = @()

    foreach ($wid in $Wanted.Keys) {
        $dir = Join-Path $Workshop $wid
        $about = Join-Path $dir 'About\About.xml'

        if (-not (Test-Path $about)) {
            $absent += "$($Wanted[$wid])  https://steamcommunity.com/sharedfiles/filedetails/?id=$wid"
            continue
        }

        try { [xml]$x = Get-Content -Raw -LiteralPath $about } catch { continue }
        $node = $x.ModMetaData.SelectSingleNode('packageId')
        if (-not $node) { continue }
        $found += $node.InnerText.Trim().ToLowerInvariant()
    }

    if ($absent.Count -gt 0) {
        Write-Host ""
        Write-Host "$($absent.Count) of the visual collection are not subscribed:" -ForegroundColor Yellow
        foreach ($a in ($absent | Sort-Object)) { Write-Host "  $a" -ForegroundColor DarkGray }
        Write-Host ""
    }

    return $found
}

$Ours = if ($Without) { @() } else { @('lizarb.interface') }

if ($Without) {
    Write-Host ""
    Write-Host "CONTROL RUN: this mod is left out. Everything else is the same." -ForegroundColor Magenta
    Write-Host "Anything that still misbehaves is not ours." -ForegroundColor Magenta
    Write-Host ""
}

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

if ($Scenario) {
    $pick = $Scenarios[$Scenario]

    if ($Scenario -eq "solo") {
        $Suspects = @()
    }
    elseif ($pick.Count -eq 0) {
        $Suspects = @(Resolve-Workshop $Visual)
    }
    else {
        $subset = [ordered]@{}
        foreach ($id in $pick) {
            $subset[$id] = if ($Visual.Contains($id)) { $Visual[$id] } else { $id }
        }
        $Suspects = @(Resolve-Workshop $subset)
    }

    Write-Host ""
    Write-Host "SCENARIO $Scenario : $($ScenarioNotes[$Scenario])" -ForegroundColor Cyan
    Write-Host "$($Suspects.Count) mod(s) picked, before dependencies." -ForegroundColor DarkGray
}
elseif ($Visuals) {
    $Suspects = @(Resolve-Workshop $Visual)
    Write-Host "$($Suspects.Count) of the visual collection are installed." -ForegroundColor DarkGray
}

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
$broken = @()

while ($order.Count -lt $baseOrder.Count) {
    if ($ready.Count -eq 0) {
        $stuck = @($baseOrder | Where-Object { $order -notcontains $_ })
        $pick = @($stuck | Sort-Object { $rank[$_] })[0]

        foreach ($k in $stuck) {
            if ($edges[$k].Contains($pick)) {
                [void]$edges[$k].Remove($pick)
                $indeg[$pick] = $indeg[$pick] - 1
                $broken += "$($mods[$k].Name) -> $($mods[$pick].Name)"
            }
        }

        [void]$ready.Add($pick)
        continue
    }

    $pick = @($ready | Sort-Object { $rank[$_] })[0]
    [void]$ready.Remove($pick)
    $order += $pick

    foreach ($n in $edges[$pick]) {
        $indeg[$n] = $indeg[$n] - 1
        if ($indeg[$n] -eq 0) { [void]$ready.Add($n) }
    }
}

if ($broken.Count -gt 0) {
    Write-Host ""
    Write-Host "$($broken.Count) contradictory load rule(s) ignored:" -ForegroundColor Yellow
    foreach ($b in $broken) { Write-Host "  $b" -ForegroundColor DarkGray }
    Write-Host ""
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
Write-Host "Player.log does NOT go to the profile. Unity always writes to persistentDataPath." -ForegroundColor DarkGray
Write-Host "After closing:  .\dev\watch-log.ps1" -ForegroundColor Yellow

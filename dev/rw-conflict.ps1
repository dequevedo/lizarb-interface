<#
  CONFLICT profile: ~80 mods picked for their odds of clashing with this one.

  Not a list of favourite mods - a list of suspects. Each one is here because it
  draws its own UI, replaces a game panel, or contests a texture this mod swaps.
  Eighty mods load in well under a minute; the full list of 374 takes over four,
  which is the whole reason this script exists.

  Two tiers. Tier 1 was chosen by hand from known interactions; tier 2 came out
  of scanning every installed mod's assemblies for the methods this mod patches.
  -Quick drops tier 2 when a fast loop matters more than coverage.

  Isolated in dev\profile-conflict via -savedatafolder. The production profile in
  AppData is never touched.

  Usage:
    .\dev\rw-conflict.ps1              # build the list and launch
    .\dev\rw-conflict.ps1 -ListOnly    # print the resolved order and stop
    .\dev\rw-conflict.ps1 -Quick       # tier 1 only, the original ~30
    .\dev\rw-conflict.ps1 -NoDlcs      # skip the DLCs (faster still)
#>
[CmdletBinding()]
param(
    [switch]$ListOnly,
    [switch]$NoDlcs,
    [switch]$Quick
)

$ErrorActionPreference = 'Stop'

# Override with $env:RIMWORLD_DIR / $env:RIMWORLD_WORKSHOP if these paths differ.
$GameDir    = if ($env:RIMWORLD_DIR) { $env:RIMWORLD_DIR } else { 'D:\Steam\steamapps\common\RimWorld' }
$GameExe    = Join-Path $GameDir 'RimWorldWin64.exe'
$Workshop   = if ($env:RIMWORLD_WORKSHOP) { $env:RIMWORLD_WORKSHOP } else { 'D:\Steam\steamapps\workshop\content\294100' }
$ProfileDir = Join-Path $PSScriptRoot 'profile-conflict'
$Seed       = Join-Path $PSScriptRoot 'profile'

# ---------------------------------------------------------------------------
# Tier 1: the suspects picked by hand. Each comment says WHY one is here -
# without that the list turns into folklore in two weeks and nobody knows what
# is safe to drop.
# ---------------------------------------------------------------------------
$Suspects = @(
    # -- known, documented interaction --
    'com.bymarcin.ArchitectIcons'            # transpiles DoCategoryButton and ButtonTextSubtle
    'ODs.IsekaiRetexture'                    # Textures/-only mod - direct texture competitor
    'JellyCreative.IsekaiLeveling'           # WindowOnGUI, DrawWindowBackground, DrawMenuSection
    'Poupun.GuildFactionAddon'               # draws its own window with its own art

    # -- these draw TABS: the biggest risk, our TabRecord.Draw returns false --
    'Andromeda.NiceHealthTab'
    'Andromeda.NiceBillTab'
    'Andromeda.NiceInventoryTab'
    'Andromeda.NiceResearchTab'
    'Owlchemist.PowerTab.forked.RunningBugs'
    'PeteTimesSix.ResearchReinvented'        # replaces the whole research tab

    # -- replace game panels or windows --
    'Jaxe.RimHUD'                            # rewrites the inspect pane
    'zeracronius.dynamictradeinterface'      # replaces the trade window
    'shunter.bettertradersguild'
    'dubwise.dubsmintminimap'                # own window, drawing constantly
    'm00nl1ght.MapPreview'                   # own window on the world screen
    'bodlosh.WeaponStats'                    # own window
    'vanillaexpanded.achievements'           # window with its own tabs

    # -- touch text, colour or scale: they fight the outline and GUI.matrix --
    'DawnsGlow.qualcolor'                    # colours item labels - exercises the text outline
    'Telefonmast.GraphicsSettings'           # touches rendering and UI scale
    'ferny.PerspectiveShift'                 # shows up in EVERY method we patch
    'Krafs.LevelUp'                          # overlay over the world
    'Deadmano.MoodAlerts'

    # -- gizmos and designators: our gizmo plate and the colonist bar --
    'PeteTimesSix.SimpleSidearms'
    'MemeGoddess.RunAndGun'
    'trigger.universalblueprints'
    'Owlchemist.ToggleableOverlays'

    # -- frameworks half the list depends on, which draw their own UI --
    'OskarPotocki.VanillaFactionsExpanded.Core'
    'UnlimitedHugs.HugsLib'
    'VanillaExpanded.VPsycastsE'
    'Dubwise.DubsPerformanceAnalyzer.steam'  # also measures what our prefixes cost
)

# ---------------------------------------------------------------------------
# Tier 2. Picked by EVIDENCE, not by reputation: every installed mod's
# Assemblies\*.dll was scanned for references to the methods this mod patches
# (DrawAtlas, DrawWindowBackground, DrawMenuSection, ButtonTextSubtle,
# TabRecord, WindowOnGUI, FillableBar, DrawTextureWithMaterial, fontStyles,
# CurFontStyle, verticalScrollbar, DoCategoryButton). The comment on each line
# is what the scan actually found.
#
# Two traps in that scan, both of which faked results the first time:
#   - Mods ship a copy of the game's Assembly-CSharp.dll under Source\. Grepping
#     it reports every marker and says nothing about the mod. Only *\Assemblies\*
#     is loaded by the game, so only that counts.
#   - The first <packageId> in an About.xml is often a <modDependencies> entry,
#     not the mod's own. It made 58 different mods look like Harmony. Parse the
#     XML and read the direct child.
#
# A reference is not a patch - see PatchAudit for that. It is the right filter
# for a SUSPECT list: it means that mod touches the same surface at all.
# ---------------------------------------------------------------------------
$SuspectsWide = @(
    # -- widest UI footprint measured: panels, windows AND scrollbars --
    'SmashPhil.VehicleFramework'             # 9 markers; already shares WindowStack.TryRemove with us
    'JAHV.SpacerVehiclesHAL.CONTINUED'       # 9 markers, same family
    'Aoba.Exosuit.Framework'                 # 7 markers incl. WindowOnGUI + CurFontStyle
    'OELS.VehicleMapFramework'               # DrawAtlas + MainTabWindow_Architect + TabRecord
    'OskarPotocki.VanillaVehiclesExpanded'   # DrawTextureWithMaterial + TabRecord

    # -- frameworks: loaded by many others, so their UI reaches everywhere --
    'AOBA.Framework'                         # MainTabWindow_Architect + DrawWindowBackground
    'EBSG.Framework'                         # DrawTextureWithMaterial + TabRecord
    'RedMattis.BetterPrerequisites'          # DrawMenuSection + TabRecord + WindowOnGUI
    'adaptive.storage.framework'             # fontStyles + verticalScrollbar - our two mutations
    'imranfish.xmlextensions'                # ButtonTextSubtle + DrawAtlas + TabRecord
    'erdelf.HumanoidAlienRaces'              # DrawMenuSection + TabRecord
    'NozoMe.MapModeFramework'                # DrawWindowBackground + WindowOnGUI
    'RimThunder.Core'                        # DrawWindowBackground + FillableBar
    'Aoba.DeadManSwitch.Core'                # DrawTextureWithMaterial + DrawWindowBackground

    # -- draw their own windows or panels --
    '6224Y.OneWithDeath'                     # 5 markers
    'VanillaStorytellersExpanded.WinstonWave' # 5 markers incl. WindowOnGUI
    'Orion.Hospitality'                      # DrawAtlas + DrawWindowBackground
    'CP.Rimdeed'                             # DrawAtlas + DrawMenuSection + WindowOnGUI
    'DerekBickley.LTOColonyGroupsFinal'      # DrawAtlas, and ships 569 UI textures
    'Andromeda.UsefulMarks'                  # DrawAtlas + DrawWindowBackground
    'Andromeda.MilkyWay'                     # DrawTextureWithMaterial + verticalScrollbar
    'jaeger972.factionterritories'           # CurFontStyle + TabRecord
    'ilyvion.LoadingProgress'                # DrawWindowBackground during startup
    'VanillaExpanded.VanillaTradingExpanded' # DrawWindowBackground
    'Orion.CashRegister'                     # DrawWindowBackground

    # -- touch the Architect menu itself --
    'vanillaexpanded.gravship'               # MainTabWindow_Architect
    'spdskatr.projectrimfactory'             # MainTabWindow_Architect
    'VanillaExpanded.VFEPropsandDecor'       # MainTabWindow_Architect

    # -- text, font and label colour: they fight the outline and the font swap --
    'CrashM.ColorCodedMoodBar.11'            # CurFontStyle
    'Owlchemist.ToggleableReadouts'          # fontStyles
    'Dark.Signs'                             # CurFontStyle
    'ferny.ResourceDeliveryHelper'           # CurFontStyle
    'DawnsGlow.Numbers'                      # FillableBar, and a large table UI
    'falconne.LabelsOnFloor'                 # DrawAtlas; also throws in the real 380-mod log
    'telardo.DragSelect'                     # DrawAtlas
    'ferny.ProgressionEducation'             # DrawTextureWithMaterial + FillableBar

    # -- content mods that still draw panels and bars; a sample, not all of VE --
    'OskarPotocki.VFE.Deserters'
    'OskarPotocki.VFE.Empire'
    'OskarPotocki.VFE.Medieval2'             # ButtonTextSubtle
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

# Tier 1 alone with -Quick; both by default.
if (-not $Quick) { $Suspects = $Suspects + $SuspectsWide }

# Always last: loading last means registering last, which means yielding on a
# prefix that returns false.
$Ours = @('lizarb.interface')

$Core = 'ludeon.rimworld'
$Dlc  = @(
    'ludeon.rimworld.royalty'
    'ludeon.rimworld.ideology'
    'ludeon.rimworld.biotech'
    'ludeon.rimworld.anomaly'
    'ludeon.rimworld.odyssey'
)

# ---------------------------------------------------------------------------
# Indexes every installed mod from its About.xml.
#
# Keyed by LOWERCASE packageId: RimWorld treats packageIds case-insensitively and
# real lists mix the two spellings, so a case-sensitive compare would make an
# installed mod vanish with no explanation.
# ---------------------------------------------------------------------------
function Read-Mods([string[]]$Roots) {
    $map = @{}
    foreach ($root in $Roots) {
        if (-not (Test-Path $root)) { continue }
        foreach ($dir in Get-ChildItem $root -Directory -ErrorAction SilentlyContinue) {
            $about = Join-Path $dir.FullName 'About\About.xml'
            if (-not (Test-Path $about)) {
                # Some Workshop mods keep everything under a version subfolder.
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

            # SelectSingleNode, not $m.name: with no <name> child - the case for Core
            # and the DLCs - PowerShell falls back to XmlElement.Name and returns
            # "ModMetaData".
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

# ---------------------------------------------------------------------------
# Transitive dependency closure.
#
# Without it the list boots with red missing-dependency errors - and the error log
# is exactly what this profile exists to read, so polluting it defeats the point.
# ---------------------------------------------------------------------------
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

# ---------------------------------------------------------------------------
# Topological sort over loadAfter / loadBefore.
#
# Kahn with a STABLE tie-break: among ready candidates take the lowest index in the
# base order. Without it the order changes between runs and an intermittent bug
# becomes impossible to reproduce.
# ---------------------------------------------------------------------------
$baseOrder = @()
foreach ($id in (@($Core) + $(if ($NoDlcs) { @() } else { $Dlc }) + $Suspects + $Ours)) {
    $k = $id.ToLowerInvariant()
    if ($wanted.Contains($k) -and $baseOrder -notcontains $k) { $baseOrder += $k }
}
# dependencies pulled in that were not seeded go right after Core
foreach ($k in $wanted) { if ($baseOrder -notcontains $k) { $baseOrder = @($baseOrder[0]) + $k + $baseOrder[1..($baseOrder.Count - 1)] } }

$rank = @{}
for ($i = 0; $i -lt $baseOrder.Count; $i++) { $rank[$baseOrder[$i]] = $i }

# edge A -> B means "A loads before B"
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
    foreach ($d in $m.Deps)   { Add-Edge $d $k }   # a dependency loads first
    foreach ($a in $m.After)  { Add-Edge $a $k }
    foreach ($b in $m.Before) { Add-Edge $k $b }
}

# Core first and ours last, whatever anyone else declares.
#
# With ONE exception: Harmony declares <loadBefore>Ludeon.RimWorld</loadBefore> and
# the game honours it. Forcing Core ahead of everything created Core->Harmony next
# to Harmony->Core, a cycle, and the whole sort fell back silently. Anything that
# declares itself before Core is exempt.
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
    # @(...) is required. With ONE item in $ready the pipeline returns a scalar and
    # [0] on a string is its first CHARACTER, so Remove finds nothing, $ready never
    # shrinks and the loop spins forever - only when the queue reaches one item,
    # so not on every run.
    $pick = @($ready | Sort-Object { $rank[$_] })[0]
    [void]$ready.Remove($pick)
    $order += $pick
    foreach ($n in $edges[$pick]) {
        $indeg[$n] = $indeg[$n] - 1
        if ($indeg[$n] -eq 0) { [void]$ready.Add($n) }
    }
}

if ($order.Count -ne $baseOrder.Count) {
    # Cycle in the ordering rules. Fall back to the base order, which is known to
    # work - an imperfect order beats a silently truncated list.
    Write-Host "cycle in loadAfter/loadBefore; falling back to the base order." -ForegroundColor Yellow
    $order = $baseOrder
}

Write-Host ""
Write-Host "Resolved order ($($order.Count) mods):" -ForegroundColor Cyan
for ($i = 0; $i -lt $order.Count; $i++) {
    $m = $mods[$order[$i]]
    if ($Ours -contains $order[$i]) { $tag = 'Green'; $mark = '*' }
    elseif ($seedKeys.Contains($order[$i])) { $tag = 'Gray'; $mark = ' ' }
    else { $tag = 'DarkGray'; $mark = '+' }   # + = pulled in as a dependency
    Write-Host ("  {0,2}.{1} {2,-46} {3}" -f ($i + 1), $mark, $m.Id, $m.Name) -ForegroundColor $tag
}
Write-Host ""

if ($ListOnly) { return }

# ---------------------------------------------------------------------------
# Write the profile and launch
# ---------------------------------------------------------------------------
$cfgDir = Join-Path $ProfileDir 'Config'
if (-not (Test-Path $cfgDir)) { New-Item -ItemType Directory -Path $cfgDir -Force | Out-Null }

# Seed Prefs from the dev profile (devMode already on) the first time.
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

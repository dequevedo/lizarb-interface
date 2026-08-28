<#
.SYNOPSIS
  Reads RimWorld's Player.log looking for problems caused by this mod.

.DESCRIPTION
  Three modes:

    .\dev\watch-log.ps1            analyse the session that just ended
    .\dev\watch-log.ps1 -Prev      analyse the previous one (Player-prev.log)
    .\dev\watch-log.ps1 -Follow    tail it live, with the game open

  Player.log is NOT redirected by the dev profile's -savedatafolder: Unity always
  writes to persistentDataPath. So this is the right file whether the session came
  from the dev profile or from a full mod list.
#>
[CmdletBinding()]
param(
    [switch]$Follow,
    [switch]$Prev,
    [switch]$All      # show every exception, not only ours
)

$ErrorActionPreference = 'Stop'

$dir = Join-Path $env:USERPROFILE 'AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios'
$log = Join-Path $dir ($(if ($Prev) { 'Player-prev.log' } else { 'Player.log' }))

if (-not (Test-Path $log)) { throw "log not found: $log" }

$Ours = @('LizarbInterface')

function Test-Ours([string]$text) {
    foreach ($o in $Ours) { if ($text -match [regex]::Escape($o)) { return $true } }
    return $false
}

function Write-Line([string]$line) {
    if (Test-Ours $line) { Write-Host $line -ForegroundColor Cyan; return }
    if ($line -match 'Exception|ERROR|Could not|failed|Harmony') { Write-Host $line -ForegroundColor Red; return }
    Write-Host $line -ForegroundColor DarkGray
}

if ($Follow) {
    Write-Host "following $log  (Ctrl+C to stop)" -ForegroundColor Yellow
    Get-Content -LiteralPath $log -Tail 0 -Wait | ForEach-Object { Write-Line $_ }
    return
}

$lines = Get-Content -LiteralPath $log
Write-Host "$log  ($($lines.Count) lines)" -ForegroundColor Yellow
Write-Host ""

# The patch audit the mod prints at startup.
# @(...) matters: a pipeline with one match returns a scalar, and [0] on a string
# is its first CHARACTER, so IndexOf would then fail to find it.
$audit = @($lines | Where-Object { $_ -match 'patch audit' })
if ($audit.Count -gt 0) {
    Write-Host "== patch audit ==" -ForegroundColor Green
    $i = [array]::IndexOf($lines, $audit[0])
    for ($j = $i; $j -lt [Math]::Min($i + 40, $lines.Count); $j++) {
        if ($j -gt $i -and $lines[$j] -notmatch '^\s+\S') { break }
        Write-Host "  $($lines[$j])" -ForegroundColor Cyan
    }
    Write-Host ""
}

# A RimWorld exception starts on a header line and continues while the following
# lines are stack frames.
$blocks = @()
$cur = $null
foreach ($line in $lines) {
    $isFrame = $line -match '^\s' -or $line -match '^\s*at\s'
    if ($isFrame -and $null -ne $cur) { $cur.Body += $line; continue }

    if ($null -ne $cur) { $blocks += ,$cur; $cur = $null }

    if ($line -match 'Exception|Root level exception|Could not|HarmonyException|failed to load|Error while') {
        $cur = [pscustomobject]@{ Head = $line; Body = New-Object System.Collections.ArrayList }
    }
}
if ($null -ne $cur) { $blocks += ,$cur }

if ($blocks.Count -eq 0) {
    Write-Host "no exceptions in the log." -ForegroundColor Green
    return
}

$mine = @($blocks | Where-Object { Test-Ours ($_.Head + ($_.Body -join "`n")) })

Write-Host "== $($blocks.Count) error block(s), $($mine.Count) mentioning this mod ==" -ForegroundColor Yellow
Write-Host ""

if ($mine.Count -gt 0) {
    Write-Host "-- OURS --" -ForegroundColor Red
    foreach ($b in $mine) {
        Write-Host $b.Head -ForegroundColor Cyan
        foreach ($l in $b.Body) { Write-Host "  $l" -ForegroundColor DarkGray }
        Write-Host ""
    }
}

# Grouped by message: one error repeating every frame would otherwise fill the screen.
$others = @($blocks | Where-Object { $_ -notin $mine })
if ($others.Count -gt 0) {
    Write-Host "-- OTHERS (grouped by message) --" -ForegroundColor DarkYellow
    $others | Group-Object { $_.Head -replace '0x[0-9a-fA-F]+','' } |
        Sort-Object Count -Descending | ForEach-Object {
            Write-Host ("  {0,4}x  {1}" -f $_.Count, $_.Name)
            if ($All) { foreach ($l in $_.Group[0].Body) { Write-Host "        $l" -ForegroundColor DarkGray } }
        }
    Write-Host ""
    if (-not $All) { Write-Host "  (-All shows each stack trace)" -ForegroundColor DarkGray }
}

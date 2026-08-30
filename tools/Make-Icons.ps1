param(
    [switch]$Extract,
    [string]$PackDir,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$Repo = Split-Path -Parent $PSScriptRoot
$PackArt = Join-Path $Repo 'art\icons\pack'
$CustomArt = Join-Path $Repo 'art\icons\custom'
$OutDir = Join-Path $Repo 'Skins\Shared'

$Size = 64
$Fill = 52
$Outline = 2

foreach ($d in @($PackArt, $CustomArt, $OutDir)) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}

$Map = [ordered]@{
    'Orders'      = '6-buildings/flag'
    'Production'  = '2-items/hammer-02'
    'Furniture'   = '6-buildings/chair'
    'Power'       = '4-nature/lightning'
    'Security'    = '3-gear/shield'
    'Misc'        = '12-misc/shapes'
    'Floors'      = '8-ui/grid'
    'Joy'         = '1-game/dice-05'
    'Ship'        = '7-vehicles/rocket'
    'Temperature' = '4-nature/temperature'
    'Ideology'    = '12-misc/six-pointed-star'
    'Anomaly'     = '1-game/demon'
    'Odyssey'     = '9-media/earth'
    'Zone'        = '10-editing/select'
    'Storage'     = '2-items/chest'
    'Industry'    = '6-buildings/anvil'
    'Nature'      = '4-nature/tree'
    'Water'       = '4-nature/water'
    'Medical'     = '2-items/medical-kit'
    'Vehicle'     = '7-vehicles/sedan-car'
    'Blueprint'   = '9-media/document'
    'Sign'        = '9-media/tag'
    'Arcane'      = '3-gear/wand'

    'ShowZones'                    = '10-editing/frame'
    'ShowBeauty'                   = '4-nature/flower'
    'CategorizedResourceReadout'   = '8-ui/menu'
    'ShowColonistBar'              = '8-ui/user-group'
    'ShowRoofOverlay'              = '6-buildings/house-03'
    'ShowTemperatureOverlay'       = '4-nature/temperature-up'
    'ShowFertilityOverlay'         = '4-nature/grass'
    'ShowTerrainAffordanceOverlay' = '4-nature/stone'
    'ShowPollutionOverlay'         = '12-misc/radiation'
    'ShowLearningHelper'           = '8-ui/question-mark'
    'AutoHomeArea'                 = '6-buildings/house'
    'AutoRebuild'                  = '8-ui/refresh'
    'LockNorthUp'                  = '8-ui/lock'
    'ShowWorldFeatures'            = '4-nature/mountain'
    'UsePlanetDayNightSystem'      = '4-nature/night'
    'ShowImportantLocations'       = '9-media/location'
    'ShowLandmarkIcons'            = '6-buildings/tower-03'
    'ShowOtherFactionBases'        = '6-buildings/village'
    'CodexButton'                  = '2-items/book'
    'SearchButton'                 = '8-ui/search'
    'CloseX'                       = '8-ui/cross'

    'SpeedPause'  = '9-media/pause'
    'SpeedNormal' = '9-media/play'
    'SpeedFast'   = '9-media/fast-forward'
}

$Missing = [ordered]@{
    'Structure'         = '6-buildings/door'
    'Biotech'           = '9-media/microchip'
    'ShowRoomStats'     = '12-misc/rectangle'
    'ShowVacuumOverlay' = '8-ui/circle-ring'
    'SpeedSuper'        = '9-media/fast-forward'
    'SpeedUltra'        = '9-media/fast-forward'
}

function Copy-Source {
    param([string]$Root, [string]$Rel, [string]$Dest)

    $src = (Join-Path $Root ($Rel -replace '/', '\')) + '.png'
    if (-not (Test-Path $src)) { throw "pack icon not found: $Rel" }
    Copy-Item $src $Dest -Force
}

if ($Extract) {
    if (-not $PackDir) { throw 'pass -PackDir pointing at the extracted Game Icon Pack' }

    $root = Join-Path $PackDir 'no-padding\256px\white'
    if (-not (Test-Path $root)) { throw "expected $root" }

    foreach ($name in $Map.Keys) {
        Copy-Source $root $Map[$name] (Join-Path $PackArt "Icon$name.png")
    }

    foreach ($name in $Missing.Keys) {
        $dest = Join-Path $CustomArt "Icon$name.png"
        if ((Test-Path $dest) -and -not $Force) {
            Write-Host "  kept $name" -ForegroundColor Yellow
            continue
        }
        Copy-Source $root $Missing[$name] $dest
    }

    Write-Host "extracted $($Map.Count) pack icons and $($Missing.Count) drawing bases" -ForegroundColor Green
}

function New-Composed {
    param([string]$Src, [string]$Dest)

    $raw = [System.Drawing.Bitmap]::FromFile($Src)

    $scale = $Fill / [Math]::Max($raw.Width, $raw.Height)
    $w = [Math]::Max(1, [int][Math]::Round($raw.Width * $scale))
    $h = [Math]::Max(1, [int][Math]::Round($raw.Height * $scale))

    $flat = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($flat)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($raw, [int](($Size - $w) / 2), [int](($Size - $h) / 2), $w, $h)
    $g.Dispose()
    $raw.Dispose()

    $rect = New-Object System.Drawing.Rectangle(0, 0, $Size, $Size)
    $data = $flat.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly,
                           [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $stride = $data.Stride
    $bytes = New-Object byte[] ($stride * $Size)
    [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $bytes, 0, $bytes.Length)
    $flat.UnlockBits($data)
    $flat.Dispose()

    $alpha = New-Object 'single[]' ($Size * $Size)
    for ($y = 0; $y -lt $Size; $y++) {
        for ($x = 0; $x -lt $Size; $x++) {
            $alpha[$y * $Size + $x] = $bytes[$y * $stride + $x * 4 + 3] / 255.0
        }
    }

    $out = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $odata = $out.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::WriteOnly,
                           [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $ostride = $odata.Stride
    $obytes = New-Object byte[] ($ostride * $Size)

    $r2 = $Outline * $Outline
    for ($y = 0; $y -lt $Size; $y++) {
        for ($x = 0; $x -lt $Size; $x++) {
            $core = $alpha[$y * $Size + $x]

            $ring = 0.0
            for ($dy = -$Outline; $dy -le $Outline; $dy++) {
                $ny = $y + $dy
                if ($ny -lt 0 -or $ny -ge $Size) { continue }
                for ($dx = -$Outline; $dx -le $Outline; $dx++) {
                    if ($dx * $dx + $dy * $dy -gt $r2) { continue }
                    $nx = $x + $dx
                    if ($nx -lt 0 -or $nx -ge $Size) { continue }
                    $v = $alpha[$ny * $Size + $nx]
                    if ($v -gt $ring) { $ring = $v }
                }
            }

            $a = [Math]::Max($core, $ring)
            if ($a -le 0.0) { continue }

            $lum = [int][Math]::Round(255.0 * $core / $a)
            $i = $y * $ostride + $x * 4
            $obytes[$i] = $lum
            $obytes[$i + 1] = $lum
            $obytes[$i + 2] = $lum
            $obytes[$i + 3] = [int][Math]::Round(255.0 * $a)
        }
    }

    [System.Runtime.InteropServices.Marshal]::Copy($obytes, 0, $odata.Scan0, $obytes.Length)
    $out.UnlockBits($odata)
    $out.Save($Dest, [System.Drawing.Imaging.ImageFormat]::Png)
    $out.Dispose()
}

$all = @($Map.Keys) + @($Missing.Keys)
$built = 0

foreach ($name in $all) {
    $custom = Join-Path $CustomArt "Icon$name.png"
    $src = if (Test-Path $custom) { $custom } else { Join-Path $PackArt "Icon$name.png" }
    if (-not (Test-Path $src)) { throw "no source art for Icon$name, run -Extract first" }

    New-Composed $src (Join-Path $OutDir "Icon$name.png")
    $built++
}

Write-Host "OK: $built icons -> Skins\Shared" -ForegroundColor Green

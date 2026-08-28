[CmdletBinding()]
param(
    [switch]$SheetOnly,
    [switch]$PreviewOnly,
    [switch]$ArchitectOnly
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$Repo  = Split-Path $PSScriptRoot -Parent
$Skins = Join-Path $Repo 'Skins'
$Mod   = Join-Path $Repo 'Source\LizarbInterface\LizarbInterfaceMod.cs'
$Cats  = Join-Path $Repo 'Source\LizarbInterface\Architect\CategoryPalette.cs'

$Featured = @('Foundry', 'Royal', 'Verdant', 'Crimson', 'Wood', 'Aero')

$src = Get-Content $Mod -Raw
$pairs = @([regex]::Matches($src, '\("([A-Za-z]+)",\s*"([A-Za-z]+)",\s*new Color') |
           ForEach-Object { , @($_.Groups[1].Value, $_.Groups[2].Value) })
if ($pairs.Count -eq 0) { throw "no theme tuples found in $Mod" }

foreach ($p in $pairs) {
    if (-not (Test-Path (Join-Path $Skins $p[0]))) {
        throw "theme $($p[0]) is in the C# but has no Skins folder"
    }
}

$known = @($pairs | ForEach-Object { $_[0] })
foreach ($f in $Featured) {
    if ($known -notcontains $f) { throw "featured theme $f is not in LizarbInterfaceMod.Themes" }
}

$scale = [double](Get-Content (Join-Path $Skins 'atlas-scale.txt') -Raw).Trim()

function Draw9 {
    param($g, $img, [int]$X, [int]$Y, [int]$W, [int]$H)

    $c = [int][Math]::Min($img.Width * 0.25 / $scale, [Math]::Min($H / 2, $W / 2))
    $sc = [int]($img.Width * 0.25)
    $sy0 = [int]($img.Height * 0.25)
    $sx = @(0, $sc, ($img.Width - $sc));  $sw = @($sc, ($img.Width - 2 * $sc), $sc)
    $dx = @($X, ($X + $c), ($X + $W - $c)); $dw = @($c, ($W - 2 * $c), $c)
    $sy = @(0, $sy0, ($img.Height - $sy0)); $sh = @($sy0, ($img.Height - 2 * $sy0), $sy0)
    $dy = @($Y, ($Y + $c), ($Y + $H - $c)); $dh = @($c, ($H - 2 * $c), $c)

    for ($i = 0; $i -lt 3; $i++) {
        for ($j = 0; $j -lt 3; $j++) {
            if ($dw[$i] -le 0 -or $dh[$j] -le 0) { continue }
            $g.DrawImage($img,
                (New-Object System.Drawing.Rectangle($dx[$i], $dy[$j], $dw[$i], $dh[$j])),
                (New-Object System.Drawing.Rectangle($sx[$i], $sy[$j], $sw[$i], $sh[$j])),
                [System.Drawing.GraphicsUnit]::Pixel)
        }
    }
}

function New-Canvas {
    param([int]$W, [int]$H)

    $bmp = New-Object System.Drawing.Bitmap($W, $H)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBilinear
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
    $g.Clear([System.Drawing.Color]::FromArgb(24, 22, 21))
    , @($bmp, $g)
}

function Draw-Card {
    param($g, [string]$Theme, [string]$Pattern, [int]$X, [int]$Y, [int]$W, [int]$H,
          $NameFont, $SmallFont, [switch]$WithTab)

    $dir = Join-Path $Skins $Theme

    $win = [System.Drawing.Image]::FromFile("$dir\WindowAtlas.png")
    Draw9 $g $win $X $Y $W $H
    $win.Dispose()

    $pad = [int]($W * 0.09)
    $bw = [int](($W - $pad * 3) / 2)
    $bh = [int]($H * 0.20)
    $by = $Y + [int]($H * 0.16)

    $btn = [System.Drawing.Image]::FromFile("$dir\ButtonBG.png")
    Draw9 $g $btn ($X + $pad) $by $bw $bh
    Draw9 $g $btn ($X + $pad * 2 + $bw) $by $bw $bh
    $btn.Dispose()

    if ($WithTab) {
        $tab = [System.Drawing.Image]::FromFile("$dir\TabAtlas.png")
        Draw9 $g $tab ($X + $pad) ($Y + $H - $bh - [int]($H * 0.12)) ([int]($bw * 1.15)) $bh
        $tab.Dispose()
    }

    $ty = $by + $bh + [int]($H * 0.10)
    $g.DrawString($Theme, $NameFont, [System.Drawing.Brushes]::White, ($X + $pad), $ty)
    if ($SmallFont) {
        $g.DrawString($Pattern, $SmallFont, [System.Drawing.Brushes]::Gainsboro,
                      ($X + $pad * 2 + $bw), ($ty + 3))
    }
}

function Write-Sheet {
    $CW = 248; $CH = 190; $COLS = 4; $GAP = 6
    $rows = [Math]::Ceiling($pairs.Count / $COLS)
    $c = New-Canvas ($COLS * $CW + $GAP) ($rows * $CH + $GAP)
    $bmp = $c[0]; $g = $c[1]

    $name = New-Object System.Drawing.Font('Segoe UI', 11, [System.Drawing.FontStyle]::Bold)
    $small = New-Object System.Drawing.Font('Segoe UI', 8)

    for ($i = 0; $i -lt $pairs.Count; $i++) {
        $x = $GAP + ($i % $COLS) * $CW
        $y = $GAP + [Math]::Floor($i / $COLS) * $CH
        Draw-Card $g $pairs[$i][0] $pairs[$i][1] $x $y ($CW - $GAP * 2) ($CH - $GAP * 2) $name $small -WithTab
    }

    $out = Join-Path $Repo 'docs\themes.png'
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Host "docs\themes.png  ($($pairs.Count) themes, $($COLS * $CW + $GAP)x$($rows * $CH + $GAP))" -ForegroundColor Green
}

function Write-Preview {
    $W = 640; $H = 360; $COLS = 3; $Band = 52
    $CW = [int]($W / $COLS)
    $CH = [int](($H - $Band) / 2)

    $c = New-Canvas $W $H
    $bmp = $c[0]; $g = $c[1]

    $title = New-Object System.Drawing.Font('Georgia', 21, [System.Drawing.FontStyle]::Bold)
    $sub = New-Object System.Drawing.Font('Segoe UI', 10)
    $name = New-Object System.Drawing.Font('Segoe UI', 10, [System.Drawing.FontStyle]::Bold)
    $ink = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 16, 12, 8))
    $gold = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 240, 214, 158))
    $dim = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 150, 142, 132))

    foreach ($dx in -2..2) {
        foreach ($dy in -2..2) {
            $g.DrawString('Lizarb Interface', $title, $ink, (16 + $dx), (6 + $dy))
        }
    }
    $g.DrawString('Lizarb Interface', $title, $gold, 16, 6)

    $count = $pairs.Count
    $titleWidth = $g.MeasureString('Lizarb Interface', $title).Width
    $g.DrawString("$count themes for RimWorld 1.6", $sub, $dim, (16 + $titleWidth), 20)

    for ($i = 0; $i -lt $Featured.Count; $i++) {
        $theme = $Featured[$i]
        $pattern = ($pairs | Where-Object { $_[0] -eq $theme })[0][1]
        $x = ($i % $COLS) * $CW
        $y = $Band + [Math]::Floor($i / $COLS) * $CH
        Draw-Card $g $theme $pattern ($x + 6) ($y + 4) ($CW - 12) ($CH - 10) $name $null
    }

    $out = Join-Path $Repo 'About\Preview.png'
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Host "About\Preview.png  ($($Featured.Count) of $count themes, ${W}x${H})" -ForegroundColor Green
}

$Categories = @(
    'Orders', 'Blueprints(UB)', 'Zone', 'Structure', 'Production', 'Storage',
    'Tavern', 'Furniture', 'Genetics', 'Power', 'Pipe networks', 'Security',
    'Misc', 'Industrial', 'Labeling', 'Floors', 'Education', 'Railroad',
    'Recreation', 'Mythic', 'Temperature', 'Technical vehicles', 'Ideology',
    'Biotech', 'Gravship', 'Insectoids', 'Signs', 'Anomaly', 'Odyssey', 'Ship'
)

function Read-Palette {
    $s = Get-Content $Cats -Raw
    $single = [System.Text.RegularExpressions.RegexOptions]::Singleline

    $fam = New-Object System.Collections.Generic.List[object]
    foreach ($m in [regex]::Matches($s,
        'new CategoryFamily\(\s*"(\w+)",\s*(\d+),\s*(\d+),\s*(\d+),\s*"(\w+)",(.*?)\),\r?\n', $single)) {
        $keys = @([regex]::Matches($m.Groups[6].Value, '"([a-z]+)"') |
                  ForEach-Object { $_.Groups[1].Value })
        $fam.Add(@($m.Groups[1].Value, [int]$m.Groups[2].Value, [int]$m.Groups[3].Value,
                   [int]$m.Groups[4].Value, $m.Groups[5].Value, $keys))
    }
    if ($fam.Count -eq 0) { throw "no CategoryFamily entries found in $Cats" }

    $hint = New-Object System.Collections.Generic.List[object]
    $block = [regex]::Match($s, 'IconHints\s*=\s*\{(.*?)\r?\n\s*\};', $single)
    foreach ($m in [regex]::Matches($block.Groups[1].Value, 'new\[\]\s*\{\s*"(\w+)",(.*?)\},')) {
        $keys = @([regex]::Matches($m.Groups[2].Value, '"([a-z]+)"') |
                  ForEach-Object { $_.Groups[1].Value })
        $hint.Add(@($m.Groups[1].Value, $keys))
    }
    if ($hint.Count -eq 0) { throw "no IconHints entries found in $Cats" }

    , @($fam.ToArray(), $hint.ToArray())
}

function Test-WordStart {
    param([string]$Text, [string]$Key)

    $at = 0
    while (($at = $Text.IndexOf($Key, $at)) -ge 0) {
        if ($at -eq 0 -or -not [char]::IsLetter($Text[$at - 1])) { return $true }
        $at++
    }
    $false
}

function New-Tint {
    param([int]$R, [int]$G, [int]$B)

    $m = New-Object System.Drawing.Imaging.ColorMatrix
    $m.Matrix00 = 0; $m.Matrix11 = 0; $m.Matrix22 = 0
    $m.Matrix40 = $R / 255.0; $m.Matrix41 = $G / 255.0; $m.Matrix42 = $B / 255.0
    $attr = New-Object System.Drawing.Imaging.ImageAttributes
    $attr.SetColorMatrix($m)
    $attr
}

function Draw9Tinted {
    param($g, $img, [int]$X, [int]$Y, [int]$W, [int]$H, $Attr)

    $c = [int][Math]::Min($img.Width * 0.25 / $scale, [Math]::Min($H / 2, $W / 2))
    $sc = [int]($img.Width * 0.25)
    $sy0 = [int]($img.Height * 0.25)
    $sx = @(0, $sc, ($img.Width - $sc));  $sw = @($sc, ($img.Width - 2 * $sc), $sc)
    $dx = @($X, ($X + $c), ($X + $W - $c)); $dw = @($c, ($W - 2 * $c), $c)
    $sy = @(0, $sy0, ($img.Height - $sy0)); $sh = @($sy0, ($img.Height - 2 * $sy0), $sy0)
    $dy = @($Y, ($Y + $c), ($Y + $H - $c)); $dh = @($c, ($H - 2 * $c), $c)

    for ($i = 0; $i -lt 3; $i++) {
        for ($j = 0; $j -lt 3; $j++) {
            if ($dw[$i] -le 0 -or $dh[$j] -le 0) { continue }
            $g.DrawImage($img,
                (New-Object System.Drawing.Rectangle($dx[$i], $dy[$j], $dw[$i], $dh[$j])),
                $sx[$i], $sy[$j], $sw[$i], $sh[$j],
                [System.Drawing.GraphicsUnit]::Pixel, $Attr)
        }
    }
}

function Write-Architect {
    param([string]$Theme = 'Foundry')

    $p = Read-Palette
    $families = $p[0]; $hints = $p[1]

    $dir = Join-Path $Skins $Theme
    $shared = Join-Path $Skins 'Shared'

    $BW = 200; $BH = 36; $GAP = 4; $PAD = 10; $COLS = 2
    $rows = [Math]::Ceiling($Categories.Count / $COLS)
    $c = New-Canvas ($COLS * ($BW + 14) + $PAD) ($PAD * 2 + $rows * ($BH + $GAP))
    $bmp = $c[0]; $g = $c[1]

    $subtle = [System.Drawing.Image]::FromFile("$dir\ButtonSubtleAtlas.png")
    $plate = [System.Drawing.Image]::FromFile("$dir\Plate.png")
    $font = New-Object System.Drawing.Font('Segoe UI', 9.5)
    $tints = @{}

    for ($i = 0; $i -lt $Categories.Count; $i++) {
        $name = $Categories[$i]
        $lower = $name.ToLower()

        $fam = $null
        foreach ($f in $families) {
            foreach ($k in $f[5]) { if (Test-WordStart $lower $k) { $fam = $f; break } }
            if ($fam) { break }
        }
        if (-not $fam) {
            $h = 0
            foreach ($ch in $name.ToCharArray()) { $h = (($h * 31 + [int]$ch) -band 0x7FFFFFFF) }
            $fam = $families[$h % $families.Count]
        }

        $icon = $fam[4]
        foreach ($hint in $hints) {
            $hit = $false
            foreach ($k in $hint[1]) { if (Test-WordStart $lower $k) { $icon = $hint[0]; $hit = $true; break } }
            if ($hit) { break }
        }

        if (-not $tints.ContainsKey($fam[0])) { $tints[$fam[0]] = New-Tint $fam[1] $fam[2] $fam[3] }

        $x = $PAD + ($i % $COLS) * ($BW + 14)
        $y = $PAD + [Math]::Floor($i / $COLS) * ($BH + $GAP)

        Draw9 $g $subtle $x $y $BW $BH
        Draw9Tinted $g $plate ($x + 3) ($y + 3) ($BW - 6) ($BH - 6) $tints[$fam[0]]

        $file = Join-Path $shared "Icon$icon.png"
        if (-not (Test-Path $file)) { throw "category $name resolved to a missing icon: $file" }
        $img = [System.Drawing.Image]::FromFile($file)
        $g.DrawImage($img, (New-Object System.Drawing.Rectangle(($x + 5), ($y + 5), 26, 26)))
        $img.Dispose()

        $g.DrawString($name, $font, [System.Drawing.Brushes]::White, ($x + 38), ($y + 9))
    }

    $subtle.Dispose(); $plate.Dispose()

    $out = Join-Path $Repo 'docs\architect.png'
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Host "docs\architect.png  ($($Categories.Count) categories, $($families.Count) families, $Theme)" -ForegroundColor Green
}

if (-not $PreviewOnly -and -not $ArchitectOnly) { Write-Sheet }
if (-not $SheetOnly -and -not $ArchitectOnly) { Write-Preview }
if (-not $SheetOnly -and -not $PreviewOnly) { Write-Architect }

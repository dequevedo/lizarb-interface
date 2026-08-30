[CmdletBinding()]
param(
    [switch]$SheetOnly,
    [switch]$PreviewOnly,
    [switch]$ArchitectOnly,
    [switch]$FontsOnly,
    [switch]$ShapesOnly,
    [switch]$GuidesOnly,
    [switch]$TitlesOnly,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$Manifest = Join-Path $PSScriptRoot 'generated-docs.txt'
$ManifestExisted = Test-Path $Manifest

$Recorded = @{}
if ($ManifestExisted) {
    foreach ($line in (Get-Content $Manifest)) {
        $parts = $line -split ' ', 2
        if ($parts.Count -eq 2) { $Recorded[$parts[0]] = $parts[1] }
    }
}

$Generated = @{}
$KeptByHand = New-Object System.Collections.Generic.List[string]

function Get-Sha {
    param([string]$Path)
    (Get-FileHash -Path $Path -Algorithm SHA256).Hash
}

function Save-Doc {
    param([System.Drawing.Bitmap]$Bmp, [string]$Path, [string]$Key)

    if (-not $Force -and $ManifestExisted -and (Test-Path $Path)) {
        $onDisk = Get-Sha $Path
        if (-not $Recorded.ContainsKey($Key) -or $Recorded[$Key] -ne $onDisk) {
            $KeptByHand.Add($Key)
            return
        }
    }

    $dir = Split-Path $Path -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

    $Bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $Generated[$Key] = Get-Sha $Path
}

$Repo  = Split-Path $PSScriptRoot -Parent
$Skins = Join-Path $Repo 'Skins'
$Mod   = Join-Path $Repo 'Source\LizarbInterface\LizarbInterfaceMod.cs'
$Cats  = Join-Path $Repo 'Source\LizarbInterface\Architect\CategoryPalette.cs'

$Featured = @('Foundry', 'Royal', 'Verdant', 'Crimson', 'Wood', 'Aero')

$src = Get-Content $Mod -Raw
$pairs = @([regex]::Matches($src, '\("([A-Za-z]+)",\s*"([A-Za-z]+)",\s*new Color\([^)]*\),\s*"(\w+)"') |
           ForEach-Object { , @($_.Groups[1].Value, $_.Groups[2].Value, $_.Groups[3].Value) } |
           Where-Object { $_[2] -ne 'Development' })
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

function Get-SkinKey {
    param([string]$Skin, [string]$Key, [string]$Fallback)

    $file = Join-Path $Skins "$Skin\theme.txt"
    if (-not (Test-Path $file)) { return $Fallback }

    foreach ($line in (Get-Content $file)) {
        $parts = $line -split '=', 2
        if ($parts.Count -eq 2 -and $parts[0].Trim().ToLower() -eq $Key) {
            return $parts[1].Trim()
        }
    }

    return $Fallback
}

function Get-SkinScale {
    param([string]$Skin)
    [double](Get-SkinKey $Skin 'scale' "$scale")
}

function Get-SkinTile {
    param([string]$Skin)
    @('true', 'on', 'yes', '1') -contains (Get-SkinKey $Skin 'tile' 'false').ToLower()
}

function Skin-File {
    param([string]$Skin, [string]$Name, [string]$Fallback)

    $path = Join-Path $Skins "$Skin\$Name.png"
    if (Test-Path $path) { return $path }
    return (Join-Path $Skins "$Skin\$Fallback.png")
}

function Draw-Card {
    param($g, [string]$Theme, [string]$Pattern, [int]$X, [int]$Y, [int]$W, [int]$H,
          $NameFont, $SmallFont, [switch]$WithTab)

    $density = Get-SkinScale $Theme
    $tile = Get-SkinTile $Theme

    $win = [System.Drawing.Image]::FromFile((Skin-File $Theme 'WindowAtlas' 'ButtonBG'))
    Draw9Scaled $g $win $X $Y $W $H $density $tile
    $win.Dispose()

    $pad = [int]($W * 0.09)
    $bw = [int](($W - $pad * 3) / 2)
    $bh = [int]($H * 0.20)
    $by = $Y + [int]($H * 0.16)

    $btn = [System.Drawing.Image]::FromFile((Join-Path $Skins "$Theme\ButtonBG.png"))
    Draw9Scaled $g $btn ($X + $pad) $by $bw $bh $density $tile
    Draw9Scaled $g $btn ($X + $pad * 2 + $bw) $by $bw $bh $density $tile
    $btn.Dispose()

    if ($WithTab) {
        $tab = [System.Drawing.Image]::FromFile((Skin-File $Theme 'TabAtlas' 'ButtonBG'))
        Draw9Scaled $g $tab ($X + $pad) ($Y + $H - $bh - [int]($H * 0.12)) ([int]($bw * 1.15)) $bh $density $tile
        $tab.Dispose()
    }

    $ty = $by + $bh + [int]($H * 0.10)
    $g.DrawString($Theme, $NameFont, [System.Drawing.Brushes]::White, ($X + $pad), $ty)

    if ($SmallFont) {
        $nameWidth = $g.MeasureString($Theme, $NameFont).Width
        $patternX = $X + $pad * 2 + $bw
        if (($X + $pad + $nameWidth) -lt $patternX) {
            $g.DrawString($Pattern, $SmallFont, [System.Drawing.Brushes]::Gainsboro,
                          $patternX, ($ty + 3))
        }
    }
}

function Write-Sheet {
    $CW = 248; $CH = 190; $COLS = 4; $GAP = 6; $HEAD = 30

    $builtIn = @($pairs | ForEach-Object { $_[0] })
    $hand = @()
    foreach ($d in (Get-ChildItem $Skins -Directory | Sort-Object Name)) {
        if ($d.Name -eq 'Shared' -or $d.Name -like 'Debug*') { continue }
        if ($builtIn -contains $d.Name) { continue }
        if (-not (Test-Path (Join-Path $d.FullName 'ButtonBG.png'))) { continue }
        $hand += , @($d.Name, (Get-SkinKey $d.Name 'pattern' 'Hatch'), 'Handpainted')
    }

    $groups = @(
        , @('Hand painted', $hand)
        , @('Generated, squared', @($pairs | Where-Object { $_[2] -eq 'Squared' }))
        , @('Generated, rounded (experimental)', @($pairs | Where-Object { $_[2] -eq 'Rounded' }))
    )

    $H = $GAP
    foreach ($grp in $groups) {
        $H += $HEAD + [Math]::Ceiling($grp[1].Count / $COLS) * $CH
    }

    $c = New-Canvas ($COLS * $CW + $GAP) $H
    $bmp = $c[0]; $g = $c[1]

    $name = New-Object System.Drawing.Font('Segoe UI', 11, [System.Drawing.FontStyle]::Bold)
    $small = New-Object System.Drawing.Font('Segoe UI', 8)
    $groupFont = New-Object System.Drawing.Font('Segoe UI', 13, [System.Drawing.FontStyle]::Bold)

    $top = $GAP
    foreach ($grp in $groups) {
        $g.DrawString($grp[0], $groupFont, [System.Drawing.Brushes]::White, $GAP, $top)
        $top += $HEAD

        $list = $grp[1]
        for ($i = 0; $i -lt $list.Count; $i++) {
            $x = $GAP + ($i % $COLS) * $CW
            $y = $top + [Math]::Floor($i / $COLS) * $CH
            Draw-Card $g $list[$i][0] $list[$i][1] $x $y ($CW - $GAP * 2) ($CH - $GAP * 2) $name $small -WithTab
        }

        $top += [Math]::Ceiling($list.Count / $COLS) * $CH
    }

    $out = Join-Path $Repo 'docs\themes.png'
    Save-Doc $bmp $out 'docs/themes.png'
    $g.Dispose(); $bmp.Dispose()
    Write-Host "docs\themes.png  ($($pairs.Count + $hand.Count) skins, $($COLS * $CW + $GAP)x$H)" -ForegroundColor Green
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
    Save-Doc $bmp $out 'About/Preview.png'
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
    Save-Doc $bmp $out 'docs/architect.png'
    $g.Dispose(); $bmp.Dispose()
    Write-Host "docs\architect.png  ($($Categories.Count) categories, $($families.Count) families, $Theme)" -ForegroundColor Green
}

function Write-Fonts {
    $dir = Join-Path $Repo 'Fonts'
    $files = @(Get-ChildItem $dir -Filter *.ttf | Sort-Object Name)
    if ($files.Count -eq 0) { throw "no .ttf in $dir" }

    $held = New-Object System.Collections.Generic.List[object]
    $families = New-Object System.Collections.Generic.List[object]

    foreach ($f in $files) {
        $pfc = New-Object System.Drawing.Text.PrivateFontCollection
        $pfc.AddFontFile($f.FullName)
        $held.Add($pfc)
        $families.Add($pfc.Families[0])
    }

    $families = @($families | Sort-Object Name)
    $cols = 2
    $rowH = 46
    $rows = [Math]::Ceiling($families.Count / $cols)
    $cw = 460
    $W = $cols * $cw
    $H = $rows * $rowH + 24

    $c = New-Canvas $W $H
    $bmp = $c[0]; $g = $c[1]

    $ink = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 232, 226, 216))

    for ($i = 0; $i -lt $families.Count; $i++) {
        $fam = $families[$i]
        $style = [System.Drawing.FontStyle]::Regular
        if (-not $fam.IsStyleAvailable($style)) { $style = [System.Drawing.FontStyle]::Bold }
        if (-not $fam.IsStyleAvailable($style)) { continue }

        $font = New-Object System.Drawing.Font($fam, 21, $style)
        $x = 24 + ($i % $cols) * $cw
        $y = 14 + [Math]::Floor($i / $cols) * $rowH
        $g.DrawString($fam.Name, $font, $ink, $x, $y)
        $font.Dispose()
    }

    $out = Join-Path $Repo 'docs\fonts.png'
    Save-Doc $bmp $out 'docs/fonts.png'
    $g.Dispose(); $bmp.Dispose()
    foreach ($p in $held) { $p.Dispose() }
    Write-Host "docs\fonts.png  ($($families.Count) faces, ${W}x${H})" -ForegroundColor Green
}

function Write-Shapes {
    param([string]$Theme = 'Foundry')

    $plate = Join-Path $Repo 'Source\LizarbInterface\Architect\Patch_ArchitectPlate.cs'
    $block = [regex]::Match((Get-Content $plate -Raw), 'string\[\] Styles\s*=\s*\{(.*?)\};',
             [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $styles = @([regex]::Matches($block.Groups[1].Value, '"(\w+)"') |
                ForEach-Object { $_.Groups[1].Value } |
                Where-Object { $_ -ne 'None' })
    if ($styles.Count -eq 0) { throw "no plate styles found in $plate" }

    $dir = Join-Path $Skins $Theme
    $shared = Join-Path $Skins 'Shared'
    $tint = New-Tint 205 137 95

    $BW = 210; $BH = 34; $GAP = 6; $PAD = 10; $COLS = 3
    $rows = [Math]::Ceiling($styles.Count / $COLS)
    $c = New-Canvas ($COLS * ($BW + $GAP) + $PAD * 2) ($PAD * 2 + $rows * ($BH + $GAP))
    $bmp = $c[0]; $g = $c[1]

    $subtle = [System.Drawing.Image]::FromFile("$dir\ButtonSubtleAtlas.png")
    $icon = [System.Drawing.Image]::FromFile("$shared\IconProduction.png")
    $font = New-Object System.Drawing.Font('Segoe UI', 9.5)

    for ($i = 0; $i -lt $styles.Count; $i++) {
        $style = $styles[$i]
        $x = $PAD + ($i % $COLS) * ($BW + $GAP)
        $y = $PAD + [Math]::Floor($i / $COLS) * ($BH + $GAP)

        Draw9 $g $subtle $x $y $BW $BH

        $px = $x + 3; $py = $y + 3; $pw = $BW - 6; $ph = $BH - 6
        $side = $ph

        switch ($style) {
            'Flat' {
                $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 205, 137, 95))
                $g.FillRectangle($brush, $px, $py, $pw, $ph)
                $brush.Dispose()
            }
            { $_ -in @('Plate', 'Frame', 'Gradient') } {
                $file = if ($_ -eq 'Plate') { 'Plate' } else { "Plate$_" }
                $img = [System.Drawing.Image]::FromFile("$dir\$file.png")
                Draw9Tinted $g $img $px $py $pw $ph $tint
                $img.Dispose()
            }
            default {
                $file = Join-Path $shared "Shape$style.png"
                if (-not (Test-Path $file)) { throw "plate style $style has no Shape$style.png" }
                $img = [System.Drawing.Image]::FromFile($file)
                $g.DrawImage($img, (New-Object System.Drawing.Rectangle($px, $py, $side, $side)),
                    0, 0, $img.Width, $img.Height, [System.Drawing.GraphicsUnit]::Pixel, $tint)
                $img.Dispose()
            }
        }

        $g.DrawImage($icon, (New-Object System.Drawing.Rectangle(($px + 5), ($py + 4), 20, 20)))
        $g.DrawString($style, $font, [System.Drawing.Brushes]::White, ($px + 34), ($y + 9))
    }

    $subtle.Dispose(); $icon.Dispose()

    $out = Join-Path $Repo 'docs\shapes.png'
    Save-Doc $bmp $out 'docs/shapes.png'
    $g.Dispose(); $bmp.Dispose()
    Write-Host "docs\shapes.png  ($($styles.Count) shapes, $Theme)" -ForegroundColor Green
}

function Write-Overlay {
    param([int]$W, [int]$H, [string]$Dir)

    $bmp = New-Object System.Drawing.Bitmap($W, $H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $cx = [int]($W / 4)
    $cy = [int]($H / 4)

    $line = [System.Drawing.Color]::FromArgb(200, 255, 0, 200)
    $corner = [System.Drawing.Color]::FromArgb(28, 255, 0, 200)
    $edge = [System.Drawing.Color]::FromArgb(28, 0, 200, 255)

    for ($y = 0; $y -lt $H; $y++) {
        $inTopBottom = ($y -lt $cy) -or ($y -ge $H - $cy)
        for ($x = 0; $x -lt $W; $x++) {
            $inLeftRight = ($x -lt $cx) -or ($x -ge $W - $cx)

            $onLine = ($x -eq $cx) -or ($x -eq $W - $cx - 1) -or ($y -eq $cy) -or ($y -eq $H - $cy - 1)

            if ($onLine) {
                $bmp.SetPixel($x, $y, $line)
            } elseif ($inLeftRight -and $inTopBottom) {
                $bmp.SetPixel($x, $y, $corner)
            } elseif ($inLeftRight -or $inTopBottom) {
                $bmp.SetPixel($x, $y, $edge)
            }
        }
    }

    $out = Join-Path $Dir "slices-${W}x${H}.png"
    Save-Doc $bmp $out ("docs/guides/slices-${W}x${H}.png")
    $bmp.Dispose()
    Write-Host "  slices-${W}x${H}.png  (corner $cx x $cy texels)" -ForegroundColor DarkGray
}

function Strip {
    param($g, $img, [int]$X, [int]$Y, [int]$W, [int]$H,
          [int]$SX, [int]$SY, [int]$SW, [int]$SH, [int]$UnitW, [int]$UnitH)

    if ($W -le 0 -or $H -le 0) { return }

    $uw = $UnitW; if ($uw -le 0 -or $uw -gt $W) { $uw = $W }
    $uh = $UnitH; if ($uh -le 0 -or $uh -gt $H) { $uh = $H }

    $cols = [int][Math]::Ceiling($W / [double]$uw); if ($cols -gt 256) { $cols = 256 }
    $rows = [int][Math]::Ceiling($H / [double]$uh); if ($rows -gt 256) { $rows = 256 }

    for ($r = 0; $r -lt $rows; $r++) {
        $y = $Y + $r * $uh
        $h = [Math]::Min($uh, $Y + $H - $y)
        if ($h -le 0) { break }
        $sh2 = [int]($SH * $h / $uh); if ($sh2 -lt 1) { $sh2 = 1 }

        for ($c = 0; $c -lt $cols; $c++) {
            $x = $X + $c * $uw
            $w = [Math]::Min($uw, $X + $W - $x)
            if ($w -le 0) { break }
            $sw2 = [int]($SW * $w / $uw); if ($sw2 -lt 1) { $sw2 = 1 }

            $g.DrawImage($img,
                (New-Object System.Drawing.Rectangle($x, $y, $w, $h)),
                (New-Object System.Drawing.Rectangle($SX, $SY, $sw2, $sh2)),
                [System.Drawing.GraphicsUnit]::Pixel)
        }
    }
}

function Draw9Scaled {
    param($g, $img, [int]$X, [int]$Y, [int]$W, [int]$H, [double]$Density, [bool]$Tile = $false, [bool]$Flat = $false)

    $c = [int][Math]::Min($img.Width * 0.25 / $Density, [Math]::Min($H / 2, $W / 2))
    $sc = [int]($img.Width * 0.25)
    $sy0 = [int]($img.Height * 0.25)
    $sx = @(0, $sc, ($img.Width - $sc));  $sw = @($sc, ($img.Width - 2 * $sc), $sc)
    $dx = @($X, ($X + $c), ($X + $W - $c)); $dw = @($c, ($W - 2 * $c), $c)
    $sy = @(0, $sy0, ($img.Height - $sy0)); $sh = @($sy0, ($img.Height - 2 * $sy0), $sy0)
    $dy = @($Y, ($Y + $c), ($Y + $H - $c)); $dh = @($c, ($H - 2 * $c), $c)

    $band = 0
    if ($Tile -and $c -gt 0) { $band = [int]($c * 2) }

    for ($i = 0; $i -lt 3; $i++) {
        for ($j = 0; $j -lt 3; $j++) {
            if ($dw[$i] -le 0 -or $dh[$j] -le 0) { continue }

            $uw = 0
            $uh = 0
            if ($i -eq 1 -and $j -ne 1) { $uw = $band }
            if ($j -eq 1 -and $i -ne 1) { $uh = $band }

            $ssx = $sx[$i]; $ssw = $sw[$i]
            $ssy = $sy[$j]; $ssh = $sh[$j]

            if ($Flat -and $i -eq 1 -and $ssw -gt 1) {
                $ssx = $ssx + [int]($ssw / 2); $ssw = 1
            }
            if ($Flat -and $j -eq 1 -and $ssh -gt 1) {
                $ssy = $ssy + [int]($ssh / 2); $ssh = 1
            }

            Strip $g $img $dx[$i] $dy[$j] $dw[$i] $dh[$j] $ssx $ssy $ssw $ssh $uw $uh
        }
    }
}

function Write-Titles {
    $Skin = 'EnhancedPixelStone'
    $Face = 'Rajdhani'
    $W = 630
    $H = 64
    $Size = 28

    $dir = Join-Path $Repo 'docs\titles'
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

    $headings = @(
        'Presets', 'Fonts', 'Settings', 'The Architect menu',
        'Compatibility', 'Built with AI', 'Source and licence', 'Dependencies'
    )

    $bbcode = Join-Path $Repo 'docs\steam-description.bbcode'
    $text = Get-Content $bbcode -Raw

    $linked = @([regex]::Matches($text, 'docs/titles/([A-Za-z0-9]+)\.png') |
                ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
    $made = @($headings | ForEach-Object { $_ -replace '[^A-Za-z0-9]', '' } | Sort-Object -Unique)

    foreach ($s in $made) {
        if ($linked -notcontains $s) { throw "banner $s is generated but the description never links it" }
    }

    foreach ($s in $linked) {
        if ($made -notcontains $s) { throw "the description links banner $s, which no heading produces" }
    }

    $skinDir = Join-Path $Skins $Skin
    if (-not (Test-Path $skinDir)) { throw "skin $Skin has no folder" }

    $density = Get-SkinScale $Skin
    $tile = Get-SkinTile $Skin
    $wanted = $Face

    $pfc = New-Object System.Drawing.Text.PrivateFontCollection
    $found = $null
    foreach ($f in (Get-ChildItem (Join-Path $Repo 'Fonts') -Filter *.ttf)) {
        $probe = New-Object System.Drawing.Text.PrivateFontCollection
        $probe.AddFontFile($f.FullName)
        if ($probe.Families[0].Name -eq $wanted) {
            $pfc.AddFontFile($f.FullName)
            $found = $pfc.Families[0]
            break
        }
    }
    if (-not $found) { throw "font '$wanted' not found in Fonts" }

    $font = New-Object System.Drawing.Font($found, $Size, [System.Drawing.FontStyle]::Regular,
                                           [System.Drawing.GraphicsUnit]::Pixel)

    $plate = [System.Drawing.Image]::FromFile((Join-Path $skinDir 'WindowAtlas.png'))

    foreach ($text in $headings) {
        $bmp = New-Object System.Drawing.Bitmap($W, $H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
        $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::SingleBitPerPixelGridFit

        Draw9Scaled $g $plate 0 0 $W $H $density $false $true

        $sf = New-Object System.Drawing.StringFormat
        $sf.Alignment = [System.Drawing.StringAlignment]::Near
        $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
        $box = New-Object System.Drawing.RectangleF(24, 0, ($W - 48), $H)

        $ink = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, 0, 0, 0))
        foreach ($dx in -1..1) {
            foreach ($dy in -1..1) {
                if ($dx -eq 0 -and $dy -eq 0) { continue }
                $shifted = New-Object System.Drawing.RectangleF(($box.X + $dx), ($box.Y + $dy), $box.Width, $box.Height)
                $g.DrawString($text, $font, $ink, $shifted, $sf)
            }
        }
        $g.DrawString($text, $font, [System.Drawing.Brushes]::White, $box, $sf)

        $g.Dispose()

        $slug = ($text -replace '[^A-Za-z0-9]', '')
        Save-Doc $bmp (Join-Path $dir "$slug.png") "docs/titles/$slug.png"
        $bmp.Dispose()
    }

    $plate.Dispose()
    Write-Host "docs\titles\  ($($headings.Count) banners, ${W}x${H}, $Skin, $wanted)" -ForegroundColor Green
}

function Write-Guides {
    $dir = Join-Path $Repo 'docs\guides'
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

    $sizes = @(
        , @(64, 64)
        , @(128, 64)
        , @(128, 128)
        , @(256, 256)
    )
    foreach ($s in $sizes) { Write-Overlay $s[0] $s[1] $dir }

    $W = 900; $H = 560
    $c = New-Canvas $W $H
    $bmp = $c[0]; $g = $c[1]

    $head = New-Object System.Drawing.Font('Segoe UI', 15, [System.Drawing.FontStyle]::Bold)
    $body = New-Object System.Drawing.Font('Segoe UI', 10)
    $mono = New-Object System.Drawing.Font('Consolas', 9)
    $white = [System.Drawing.Brushes]::White
    $dim = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 168, 160, 150))

    $g.DrawString('Nine slice: what survives where', $head, $white, 24, 18)

    $bx = 24; $by = 62; $bw = 320; $bh = 320
    $cx = [int]($bw / 4); $cy = [int]($bh / 4)

    $cornerBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(90, 255, 0, 200))
    $edgeBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(70, 0, 200, 255))
    $midBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(60, 255, 200, 0))

    $g.FillRectangle($midBrush, ($bx + $cx), ($by + $cy), ($bw - 2 * $cx), ($bh - 2 * $cy))
    $g.FillRectangle($edgeBrush, ($bx + $cx), $by, ($bw - 2 * $cx), $cy)
    $g.FillRectangle($edgeBrush, ($bx + $cx), ($by + $bh - $cy), ($bw - 2 * $cx), $cy)
    $g.FillRectangle($edgeBrush, $bx, ($by + $cy), $cx, ($bh - 2 * $cy))
    $g.FillRectangle($edgeBrush, ($bx + $bw - $cx), ($by + $cy), $cx, ($bh - 2 * $cy))
    foreach ($p in @(@(0, 0), @(1, 0), @(0, 1), @(1, 1))) {
        $g.FillRectangle($cornerBrush, ($bx + $p[0] * ($bw - $cx)), ($by + $p[1] * ($bh - $cy)), $cx, $cy)
    }

    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(220, 255, 0, 200), 1)
    $g.DrawRectangle($pen, $bx, $by, ($bw - 1), ($bh - 1))
    $g.DrawLine($pen, ($bx + $cx), $by, ($bx + $cx), ($by + $bh))
    $g.DrawLine($pen, ($bx + $bw - $cx), $by, ($bx + $bw - $cx), ($by + $bh))
    $g.DrawLine($pen, $bx, ($by + $cy), ($bx + $bw), ($by + $cy))
    $g.DrawLine($pen, $bx, ($by + $bh - $cy), ($bx + $bw), ($by + $bh - $cy))

    $g.DrawString('corner', $mono, $white, ($bx + 12), ($by + 30))
    $g.DrawString('edge', $mono, $white, ($bx + $bw / 2 - 14), ($by + 30))
    $g.DrawString('middle', $mono, $white, ($bx + $bw / 2 - 20), ($by + $bh / 2 - 8))

    $tx = 380
    $lines = @(
        @('Corner, magenta', 'Never stretched. Both axes are preserved, so this is the only'),
        @('', 'region where any drawing can go. Its size is the texture width'),
        @('', 'divided by four, clamped to half the height and half the width.'),
        @('', ''),
        @('Edge, blue', 'Stretched along the side, its cross section preserved. A profile'),
        @('', 'reads; a pattern along the side smears. The top band stretches'),
        @('', 'horizontally, so vertical detail in it survives.'),
        @('', ''),
        @('Middle, amber', 'Stretched on both axes. Flat colour, or a gradient, which just'),
        @('', 'scales. Anything with detail turns to mush.'),
        @('', ''),
        @('When it is short', 'The corner is clamped to half the height, so on an element'),
        @('', 'thinner than two corners the middle band has no room and the'),
        @('', 'top quarter is drawn straight against the bottom quarter.'),
        @('', 'Keep every vertical change inside the top and bottom quarters,'),
        @('', 'or a seam appears where they meet.')
    )

    $y = 62
    foreach ($l in $lines) {
        if ($l[0]) { $g.DrawString($l[0], $body, $white, $tx, $y) }
        if ($l[1]) { $g.DrawString($l[1], $body, $dim, ($tx + 120), $y) }
        $y += 19
    }

    $g.DrawString('Overlay files in this folder match a texture pixel for pixel. Open one as a layer on top of your art.',
                  $mono, $dim, 24, ($H - 34))

    $out = Join-Path $dir 'slices.png'
    Save-Doc $bmp $out 'docs/guides/slices.png'
    $g.Dispose(); $bmp.Dispose()
    Write-Host "docs\guides\  ($($sizes.Count) overlays + slices.png)" -ForegroundColor Green
}

$only = $SheetOnly -or $PreviewOnly -or $ArchitectOnly -or $FontsOnly -or $ShapesOnly -or $GuidesOnly -or $TitlesOnly

if ($SheetOnly -or -not $only) { Write-Sheet }
if ($PreviewOnly -or -not $only) { Write-Preview }
if ($ArchitectOnly -or -not $only) { Write-Architect }
if ($FontsOnly -or -not $only) { Write-Fonts }
if ($ShapesOnly -or -not $only) { Write-Shapes }
if ($GuidesOnly -or -not $only) { Write-Guides }
if ($TitlesOnly -or -not $only) { Write-Titles }

foreach ($key in $Recorded.Keys) {
    if (-not $Generated.ContainsKey($key)) { $Generated[$key] = $Recorded[$key] }
}

$lines = $Generated.Keys | Sort-Object | ForEach-Object { "$_ $($Generated[$_])" }
Set-Content -Path $Manifest -Value $lines -Encoding ascii

if ($KeptByHand.Count -gt 0) {
    Write-Host ""
    Write-Host "$($KeptByHand.Count) edited by hand, left alone:" -ForegroundColor Yellow
    foreach ($k in ($KeptByHand | Sort-Object)) { Write-Host "  $k" -ForegroundColor Yellow }
    Write-Host "run with -Force to overwrite them" -ForegroundColor DarkGray
}

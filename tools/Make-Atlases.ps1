param(
    [switch]$IconsOnly,
    [switch]$Force,
    [switch]$DefineOnly,
    [int]$Scale = 2,

    [string[]]$Only = @()
)

Add-Type -AssemblyName System.Drawing

$Black = @(8, 6, 5)

function Blend {
    param([int[]]$A, [int[]]$B, [double]$T)
    if ($T -lt 0) { $T = 0 }
    if ($T -gt 1) { $T = 1 }
    @(
        [int][Math]::Round($A[0] * (1 - $T) + $B[0] * $T)
        [int][Math]::Round($A[1] * (1 - $T) + $B[1] * $T)
        [int][Math]::Round($A[2] * (1 - $T) + $B[2] * $T)
    )
}

function New-RoundAtlas {
    param(
        [string]$Name, [int]$Size, [int]$Radius, [double]$Thin, [double]$Fat,
        [int[]]$Light, [int[]]$Dark, [int[]]$Fill,

        [string]$Ornament = 'Fillet',

        [int]$FillAlpha = 255,

        [double]$Gloss = 0.0,

        [bool]$Outline = $true,

        [string]$Edge = 'Plain',

        [double]$Recess = 0.0,

        [bool]$Flat = $false
    )

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $clear = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)
    $max = $Size - 1
    $zone = $Size / 4
    $scale = $Size / 64.0

    $profileDepth = 0.0
    if ($Edge -ne 'Plain') { $profileDepth = 5.0 * $scale }
    $recessDepth = 1.0
    if ($Recess -gt 0) { $recessDepth = $Recess * $scale }
    $r1 = 1.6 * $scale
    $r2 = $r1 + [Math]::Max(1.0, 1.4 * $scale)

    for ($y = 0; $y -le $max; $y++) {
        for ($x = 0; $x -le $max; $x++) {

            $cx = [Math]::Min($x, $max - $x)
            $cy = [Math]::Min($y, $max - $y)

            if ($Ornament -eq 'Chamfer') {
                $diag = $cx + $cy - $Radius
                $dist = [Math]::Min([Math]::Min($cx, $cy), $diag)
            }
            elseif ($cx -lt $Radius -and $cy -lt $Radius) {
                $dx = $Radius - $cx
                $dy = $Radius - $cy
                $dist = $Radius - [Math]::Sqrt($dx * $dx + $dy * $dy)
            }
            else {
                $dist = [Math]::Min($cx, $cy)
            }

            if ($dist -lt 0) {
                $bmp.SetPixel($x, $y, $clear)
                continue
            }

            $cornerness = 0.0
            if ($cx -lt $zone -and $cy -lt $zone) {
                $cornerness = 1.0 - ([Math]::Max($cx, $cy) / $zone)
            }

            $thick = $Thin
            $lobe = 0.0
            if ($Ornament -eq 'Fillet') {
                $thick = $Thin + ($Fat - $Thin) * $cornerness
            }

            $nearTopLeft = ($x + $y) -lt $max
            if ($nearTopLeft) { $metal = $Light } else { $metal = $Dark }

            if ($Ornament -eq 'Fillet') {
                $metal = Blend $metal @(255, 236, 194) ($cornerness * 0.55)
            }
            elseif ($Ornament -eq 'Bone') {
                $metal = Blend (Blend $metal $Black 0.18) @(255, 250, 224) ($lobe * 1.3)
            }

            $ornamentHit = $false
            $plate = 0.0
            $bcU = 0.0
            $ow = [Math]::Max(1.0, $scale * 0.8)

            if ($Ornament -eq 'Bracket') {
                $arm = [int](13 * $scale)
                $bar = [int](3 * $scale)
                $ornamentHit = ($cx -lt $arm -and $cy -lt $arm) -and
                               (($cx -le $bar) -or ($cy -le $bar)) -and ($dist -ge 1)
            }
            elseif ($Ornament -eq 'BookCorner') {
                $reach = 13.5 * $scale
                $diag = $cx + $cy

                $toInner = ($reach - $diag) / 1.4142
                $toOuter = $dist - 1.0

                $plate = [Math]::Min($toOuter, $toInner)
                $ornamentHit = ($plate -gt 0) -and ($dist -ge 1)

                if ($ornamentHit) {
                    $bcU = $toInner / [Math]::Max(0.001, $toOuter + $toInner)
                }
            }

            elseif ($Ornament -eq 'Studs') {
                $sc = [int](7 * $scale)
                $sr = [double](3.2 * $scale)
                $ddx = $cx - $sc
                $ddy = $cy - $sc
                $ornamentHit = (($ddx * $ddx) + ($ddy * $ddy)) -le ($sr * $sr)
            }
            elseif ($Ornament -eq 'Double') {
                $gap = 3 * $scale
                $ornamentHit = ($dist -ge (1 + $thick + $gap)) -and
                               ($dist -lt (1 + $thick + $gap + [Math]::Max(1, $scale)))
            }
            elseif ($Ornament -eq 'Bone' -and $cx -lt $Radius -and $cy -lt $Radius) {
                $ang1 = 12.0 * [Math]::PI / 180.0
                $ang2 = 78.0 * [Math]::PI / 180.0
                $kd = 3.2 * $scale
                $kr = 2.8 * $scale
                $rr = $Radius - $kd

                $k1x = $Radius - $rr * [Math]::Cos($ang1)
                $k1y = $Radius - $rr * [Math]::Sin($ang1)
                $k2x = $Radius - $rr * [Math]::Cos($ang2)
                $k2y = $Radius - $rr * [Math]::Sin($ang2)

                $e1x = $cx - $k1x; $e1y = $cy - $k1y
                $e2x = $cx - $k2x; $e2y = $cy - $k2y
                $in1 = (($e1x * $e1x) + ($e1y * $e1y)) -le ($kr * $kr)
                $in2 = (($e2x * $e2x) + ($e2y * $e2y)) -le ($kr * $kr)

                $ornamentHit = ($in1 -or $in2) -and ($dist -ge 1)
                if ($in1) { $lobe = 1.0 - ([Math]::Sqrt(($e1x * $e1x) + ($e1y * $e1y)) / $kr) }
                if ($in2) {
                    $l2 = 1.0 - ([Math]::Sqrt(($e2x * $e2x) + ($e2y * $e2y)) / $kr)
                    if ($l2 -gt $lobe) { $lobe = $l2 }
                }
            }
            elseif ($Ornament -eq 'Gothic') {
                $gap = 3 * $scale
                $ornamentHit = ($dist -ge (1 + $thick + $gap)) -and
                               ($dist -lt (1 + $thick + $gap + [Math]::Max(1, $scale)))

                if (-not $ornamentHit -and $cx -lt $zone -and $cy -lt $zone) {
                    $tc = 9.0 * $scale
                    $ld = 3.1 * $scale
                    $lr = 3.1 * $scale
                    $w  = [Math]::Max(1.0, 0.9 * $scale)

                    $best = 9999.0
                    $inside = $false
                    foreach ($deg in @(45.0, 165.0, 285.0)) {
                        $rad = $deg * [Math]::PI / 180.0
                        $lx = $tc + $ld * [Math]::Cos($rad)
                        $ly = $tc + $ld * [Math]::Sin($rad)
                        $ex = $cx - $lx
                        $ey = $cy - $ly
                        $dd = [Math]::Sqrt(($ex * $ex) + ($ey * $ey))
                        if ($dd -lt ($lr - $w)) { $inside = $true }
                        $err = [Math]::Abs($dd - $lr)
                        if ($err -lt $best) { $best = $err }
                    }

                    $ornamentHit = (-not $inside) -and ($best -le ($w * 0.5)) -and
                                   ($dist -ge (1 + $thick))
                }
            }

            $body = $Fill
            if ($Gloss -gt 0) {
                $vt = $y / [double]$max
                if ($vt -lt 0.5) {
                    $body = Blend $Fill @(255, 255, 255) ($Gloss * (0.5 - $vt) * 2)
                }
                else {
                    $body = Blend $Fill $Black ($Gloss * ($vt - 0.5) * 1.2)
                }
            }

            $alpha = 255

            if ($dist -lt 1) {
                if ($Outline) { $c = $Black } else { $c = $metal }
            }
            elseif ($ornamentHit -and ($Ornament -eq 'Studs' -or $Ornament -eq 'Rivets')) {
                $lit = ($ddx + $ddy) -lt 0
                $c = if ($lit) { Blend $Light @(255, 240, 210) 0.4 } else { Blend $Dark $Black 0.3 }
            }
            elseif ($ornamentHit -and $Ornament -eq 'BookCorner') {
                if ($toInner -lt $ow) {
                    $c = $Black
                }
                elseif ($Flat) {
                    if ($bcU -gt 0.45) { $c = $Light } else { $c = $Dark }
                }
                else {
                    $c = Blend (Blend $metal $Black 0.42) (Blend $metal @(255, 246, 212) 0.6) $bcU
                }
            }
            elseif ($ornamentHit) {
                $c = $metal
            }
            elseif ($dist -lt 1 + $thick) {
                if ($Flat) {
                    $c = $metal
                }
                else {
                    $t = ($dist - 1) / $thick
                    $c = Blend $metal (Blend $metal $Black 0.35) $t
                }
            }
            elseif ($Edge -ne 'Plain' -and $dist -lt (1 + $thick + $profileDepth)) {
                $d = $dist - 1 - $thick
                $u = $d / $profileDepth
                $alpha = $FillAlpha

                switch ($Edge) {
                    'Step' {
                        $tread = [Math]::Floor($u * 3.0)
                        $c = Blend (Blend $metal $Black 0.25) $body (($tread + 1) / 3.5)
                        if ($d -lt 1.2) { $c = Blend $metal @(255, 240, 210) 0.25; $alpha = 255 }
                    }
                    default { $c = $body }
                }
            }
            elseif ($dist -lt 1 + $thick + $profileDepth + $recessDepth) {
                $alpha = $FillAlpha
                if ($Recess -le 0) {
                    if ($Flat) { $c = Blend $body $Black 0.28 }
                    else { $c = Blend $body $Black 0.45 }
                }
                elseif ($Flat) {
                    if ($nearTopLeft) { $c = Blend $body $Black 0.45 }
                    else { $c = Blend $body @(255, 246, 220) 0.16 }
                }
                else {
                    $rt = ($dist - 1 - $thick - $profileDepth) / $recessDepth
                    $fade = 1.0 - $rt
                    if ($nearTopLeft) { $c = Blend $body $Black (0.62 * $fade) }
                    else { $c = Blend $body @(255, 246, 220) (0.30 * $fade) }
                }
            }
            else {
                $c = $body
                $alpha = $FillAlpha
            }

            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($alpha, $c[0], $c[1], $c[2]))
        }
    }

    $bmp.Save((Join-Path $OutDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  $Name.png  ($Size x $Size, radius $Radius, $Ornament/$Edge)"
}

function New-TabAtlas {
    param(
        [string]$Name, [int]$Radius, [double]$Thin, [double]$Fat,
        [int[]]$Light, [int[]]$Dark, [int[]]$Fill, [int]$Scale = 1
    )

    $w = 64 * $Scale; $h = 32 * $Scale
    $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $clear = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)

    for ($y = 0; $y -lt $h; $y++) {
        for ($x = 0; $x -lt $w; $x++) {

            $isEnd = ($x -lt (30 * $Scale)) -or ($x -ge (34 * $Scale))
            $ex = [Math]::Min($x, $w - 1 - $x)

            $db = $h - 1 - $y

            if ($isEnd -and $y -lt $Radius -and $ex -lt $Radius) {
                $dx = $Radius - $ex
                $dy = $Radius - $y
                $dist = $Radius - [Math]::Sqrt($dx * $dx + $dy * $dy)
            }
            elseif ($isEnd) {
                $dist = [Math]::Min([Math]::Min($ex, $y), $db)
            }
            else {
                $dist = $y
            }

            if ($dist -lt 0) {
                $bmp.SetPixel($x, $y, $clear)
                continue
            }

            $thick = $Thin
            $cornerness = 0.0
            if ($isEnd -and $ex -lt 10 -and $y -lt 10) {
                $cornerness = 1.0 - ([Math]::Max($ex, $y) / 10.0)
            }
            if ($isEnd -and $ex -lt 7 -and $db -lt 7) {
                $foot = 0.7 * (1.0 - ([Math]::Max($ex, $db) / 7.0))
                if ($foot -gt $cornerness) { $cornerness = $foot }
            }
            $thick = $Thin + ($Fat - $Thin) * $cornerness

            if ($dist -lt 1) {
                $c = $Black
            }
            elseif ($dist -lt 1 + $thick) {
                $t = ($dist - 1) / $thick
                if ($x -lt $w / 2) { $metal = $Light } else { $metal = $Dark }
                $metal = Blend $metal @(255, 236, 194) ($cornerness * 0.55)
                $c = Blend $metal (Blend $metal $Black 0.35) $t
            }
            elseif ($dist -lt 2 + $thick) {
                $c = Blend $Fill $Black 0.45
            }
            else {
                $c = $Fill
            }

            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c[0], $c[1], $c[2]))
        }
    }

    $bmp.Save((Join-Path $OutDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  $Name.png  ($w x $h, raio $Radius)"
}

function Hash01 {
    param([int]$x, [int]$y, [int]$seed)

    $n = ([int64]$x * 374761393) + ([int64]$y * 668265263) + ([int64]$seed * 362437)
    $n = $n -band 0x7FFFFFFF
    $n = ($n -bxor ($n -shr 13)) -band 0x7FFFFFFF
    $n = ($n * 60493) -band 0x7FFFFFFF
    $n = ($n -bxor ($n -shr 11)) -band 0x7FFFFFFF
    return ($n % 1000000) / 1000000.0
}

function ValueNoise {
    param([int]$x, [int]$y, [int]$period, [int]$size, [int]$seed)

    $cells = [int]($size / $period)
    $cx = [int][Math]::Floor($x / $period)
    $cy = [int][Math]::Floor($y / $period)
    $fx = ($x / [double]$period) - $cx
    $fy = ($y / [double]$period) - $cy

    $sx = $fx * $fx * (3 - 2 * $fx)
    $sy = $fy * $fy * (3 - 2 * $fy)

    $x0 = $cx % $cells; $x1 = ($cx + 1) % $cells
    $y0 = $cy % $cells; $y1 = ($cy + 1) % $cells

    $v00 = Hash01 $x0 $y0 $seed
    $v10 = Hash01 $x1 $y0 $seed
    $v01 = Hash01 $x0 $y1 $seed
    $v11 = Hash01 $x1 $y1 $seed

    $top = $v00 * (1 - $sx) + $v10 * $sx
    $bot = $v01 * (1 - $sx) + $v11 * $sx
    return $top * (1 - $sy) + $bot * $sy
}

$script:PatternMaps = @{}

function Get-PatternMap {
    param([string]$Kind, [int]$Size = 128)

    if ($script:PatternMaps.ContainsKey($Kind)) { return $script:PatternMaps[$Kind] }

    $n = $Size * $Size
    $cov = New-Object 'double[]' $n
    $mix = New-Object 'double[]' $n
    $wsh = New-Object 'double[]' $n

    $starCenters = @(@(32, 32), @(96, 32), @(32, 96), @(96, 96))

    for ($y = 0; $y -lt $Size; $y++) {
        for ($x = 0; $x -lt $Size; $x++) {

            $d = 999.0

            switch ($Kind) {

                'Hatch' {
                    $a = ($x + $y) % 14
                    $b = ($x - $y + 1024) % 14
                    $d = [Math]::Min([Math]::Min($a, 14 - $a), [Math]::Min($b, 14 - $b))
                }

                'Medieval' {
                    $a = ($x + $y) % 32
                    $b = ($x - $y + 1024) % 32
                    $lat = [Math]::Min([Math]::Min($a, 32 - $a), [Math]::Min($b, 32 - $b))
                    $da = $a - 16
                    $db = $b - 16
                    $dot = [Math]::Abs([Math]::Sqrt($da * $da + $db * $db) - 2.2)
                    $d = [Math]::Min($lat, $dot)
                }

                'Scales' {
                    $row = [int][Math]::Floor($y / 16)
                    $ox = 0
                    if ($row % 2 -ne 0) { $ox = 8 }
                    $px = (($x + $ox) % 16) - 8
                    $py = ($y % 16) - 16
                    $d = [Math]::Abs([Math]::Sqrt($px * $px + $py * $py) - 15)
                }

                'Bricks' {
                    $row = [int][Math]::Floor($y / 12)
                    $ox = 0
                    if ($row % 2 -ne 0) { $ox = 16 }
                    $h = $y % 12
                    $v = ($x + $ox) % 32
                    $d = [Math]::Min([Math]::Min($h, 12 - $h), [Math]::Min($v, 32 - $v))
                }

                'Dots' {
                    $dx = ($x % 16) - 8
                    $dy = ($y % 16) - 8
                    $d = [Math]::Max(0, [Math]::Sqrt($dx * $dx + $dy * $dy) - 1.6)
                }

                'Chevron' {
                    $band = [int][Math]::Floor($y / 16)
                    if ($band % 2 -eq 0) { $v = ($x + $y) % 16 } else { $v = ($x - $y + 1024) % 16 }
                    $d = [Math]::Min($v, 16 - $v)
                }

                'Woodgrain' {
                    $wobble = (ValueNoise $x $y 32 $Size 5) * 14
                    $g = ($y + $wobble) % 9
                    $d = [Math]::Min($g, 9 - $g)
                }

            }

            $c = 1.0 - ($d / 1.4)
            if ($c -lt 0) { $c = 0 }
            if ($c -gt 1) { $c = 1 }

            $i = $y * $Size + $x
            $cov[$i] = $c
            $mix[$i] = ValueNoise $x $y 42 $Size 71
            $wsh[$i] = ValueNoise $x $y 64 $Size 23
        }
    }

    $map = @{ Cov = $cov; Mix = $mix; Wash = $wsh }
    $script:PatternMaps[$Kind] = $map
    return $map
}

function New-Pattern {
    param(
        [string]$Kind, [int]$Size = 128,
        [int[]]$InkA, [int[]]$InkB, [int[]]$Wash
    )

    $map = Get-PatternMap -Kind $Kind -Size $Size
    $cov = $map.Cov; $mix = $map.Mix; $wsh = $map.Wash

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

    $lineAlpha = 150.0
    $washAlpha = 38.0
    $clear = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)

    for ($y = 0; $y -lt $Size; $y++) {
        for ($x = 0; $x -lt $Size; $x++) {
            $i = $y * $Size + $x

            $la = $lineAlpha * $cov[$i]
            $wa = $washAlpha * (0.35 + 0.65 * $wsh[$i])
            $total = $la + $wa

            if ($total -le 0.5) { $bmp.SetPixel($x, $y, $clear); continue }

            $ink = Blend $InkA $InkB $mix[$i]
            $c = Blend $Wash $ink ($la / $total)

            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb([int][Math]::Min(255, $total), $c[0], $c[1], $c[2]))
        }
    }

    $bmp.Save((Join-Path $OutDir "Pattern_$Kind.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  Pattern_$Kind.png"
}

function New-Checkbox {
    param(
        [string]$Name, [string]$State,
        [int[]]$Light, [int[]]$Dark, [int[]]$Fill, [int[]]$Mark
    )

    $s = 32
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $max = $s - 1

    for ($y = 0; $y -le $max; $y++) {
        for ($x = 0; $x -le $max; $x++) {
            $d = [Math]::Min([Math]::Min($x, $y), [Math]::Min($max - $x, $max - $y))
            $nearTopLeft = ($x + $y) -lt $max

            if ($d -lt 1)      { $c = $Black }
            elseif ($d -lt 4)  { if ($nearTopLeft) { $c = $Light } else { $c = $Dark } }
            else               { $c = $Fill }

            if ($State -eq 'On') {
                $a = [Math]::Abs(($x - 10) - ($y - 18))
                $b = [Math]::Abs(($x - 12) + ($y - 24))
                if (($a -le 2 -and $x -ge 8 -and $x -le 14) -or
                    ($b -le 2 -and $x -ge 12 -and $x -le 24)) { $c = $Mark }
            }
            elseif ($State -eq 'Partial') {
                if (($y -ge 14) -and ($y -le 17) -and ($x -ge 9) -and ($x -le 22)) { $c = $Mark }
            }

            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c[0], $c[1], $c[2]))
        }
    }

    $bmp.Save((Join-Path $OutDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  $Name.png"
}

function New-Radio {
    param([string]$Name, [bool]$On, [int[]]$Light, [int[]]$Dark, [int[]]$Fill, [int[]]$Mark)

    $s = 32
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $clear = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)
    $cen = 15.5

    for ($y = 0; $y -lt $s; $y++) {
        for ($x = 0; $x -lt $s; $x++) {
            $dx = $x - $cen
            $dy = $y - $cen
            $r = [Math]::Sqrt($dx * $dx + $dy * $dy)

            if ($r -gt 15.0) { $bmp.SetPixel($x, $y, $clear); continue }

            $nearTopLeft = ($dx + $dy) -lt 0
            if ($r -gt 14.0)     { $c = $Black }
            elseif ($r -gt 11.0) { if ($nearTopLeft) { $c = $Light } else { $c = $Dark } }
            elseif ($On -and $r -le 6.0) { $c = $Mark }
            else                 { $c = $Fill }

            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c[0], $c[1], $c[2]))
        }
    }

    $bmp.Save((Join-Path $OutDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  $Name.png"
}

function New-Knob {
    param([string]$Name, [int[]]$Light, [int[]]$Dark, [int[]]$Fill)

    $s = 16
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $clear = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)
    $cen = 7.5

    for ($y = 0; $y -lt $s; $y++) {
        for ($x = 0; $x -lt $s; $x++) {
            $dx = $x - $cen
            $dy = $y - $cen
            $r = [Math]::Sqrt($dx * $dx + $dy * $dy)

            if ($r -gt 7.5) { $bmp.SetPixel($x, $y, $clear); continue }

            if ($r -gt 6.5)     { $c = $Black }
            elseif ($dx + $dy -lt 0) { $c = $Light }
            else                { $c = Blend $Dark $Fill 0.4 }

            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c[0], $c[1], $c[2]))
        }
    }

    $bmp.Save((Join-Path $OutDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  $Name.png"
}

function New-Strip {
    param(
        [string]$Name, [int]$W, [int]$H,
        [int[]]$Top, [int[]]$Bottom, [int[]]$Edge, [bool]$Outline = $true
    )

    $bmp = New-Object System.Drawing.Bitmap($W, $H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $maxY = $H - 1
    $maxX = $W - 1

    for ($y = 0; $y -le $maxY; $y++) {
        $t = $y / [double]$maxY
        $body = Blend $Top $Bottom $t

        for ($x = 0; $x -le $maxX; $x++) {
            $d = [Math]::Min([Math]::Min($x, $y), [Math]::Min($maxX - $x, $maxY - $y))

            if ($Outline -and $d -lt 1)     { $c = $Black }
            elseif ($Outline -and $d -lt 2) { $c = $Edge }
            else                            { $c = $body }

            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c[0], $c[1], $c[2]))
        }
    }

    $bmp.Save((Join-Path $OutDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  $Name.png  ($W x $H)"
}

$Themes = @{

    'Grimoire' = @{
        Ornament = 'BookCorner'; Edge = 'Plain'; Pattern = 'Hatch'; Flat = $true; Recess = 3.5
        Radius = @{ Button = 3; Tab = 3; Window = 6; Section = 3 }
        Fillet = @{ Thin = 2; Fat = 2; WindowThin = 2; WindowFat = 2 }
        Button  = @{ Light = @(150, 142, 128); Dark = @(74, 68, 61);   Fill = @(58, 53, 48) }
        Hover   = @{ Light = @(186, 177, 161); Dark = @(98, 90, 81);   Fill = @(76, 69, 62) }
        Click   = @{ Light = @(84, 78, 70);    Dark = @(140, 132, 119); Fill = @(44, 40, 36) }
        Subtle  = @{ Light = @(140, 132, 119); Dark = @(68, 62, 56);   Fill = @(52, 47, 43) }
        Tab     = @{ Light = @(164, 155, 140); Dark = @(82, 75, 68);   Fill = @(64, 58, 53) }
        Window  = @{ Light = @(142, 134, 121); Dark = @(70, 64, 58);   Fill = @(38, 34, 31) }
        Section = @{ Light = @(104, 97, 88);   Dark = @(54, 49, 45);   Fill = @(28, 25, 23) }
    }
    'Foundry' = @{
        Ornament = 'None'; Edge = 'Step'; Pattern = 'Bricks'
        Radius = @{ Button = 5; Tab = 5; Window = 10; Section = 5 }
        Fillet = @{ Thin = 3; Fat = 5; WindowThin = 4; WindowFat = 7 }
        Button  = @{ Light = @(214, 132, 62);  Dark = @(70, 58, 54);   Fill = @(48, 40, 38) }
        Hover   = @{ Light = @(255, 174, 92);  Dark = @(102, 84, 76);  Fill = @(68, 56, 52) }
        Click   = @{ Light = @(96, 60, 32);    Dark = @(198, 122, 58); Fill = @(34, 28, 26) }
        Subtle  = @{ Light = @(190, 118, 58);  Dark = @(62, 52, 48);   Fill = @(42, 36, 34) }
        Tab     = @{ Light = @(236, 150, 74);  Dark = @(84, 70, 64);   Fill = @(56, 46, 43) }
        Window  = @{ Light = @(178, 112, 56);  Dark = @(58, 49, 46);   Fill = @(28, 24, 23) }
        Section = @{ Light = @(120, 78, 44);   Dark = @(44, 38, 36);   Fill = @(20, 17, 17) }
    }
    'Brass' = @{
        Ornament = 'Fillet'; Pattern = 'Hatch'
        Radius = @{ Button = 11; Tab = 10; Window = 22; Section = 10 }
        Fillet = @{ Thin = 2; Fat = 4; WindowThin = 3; WindowFat = 7 }
        Button  = @{ Light = @(178, 145, 89);  Dark = @(96, 77, 48);   Fill = @(64, 51, 38) }
        Hover   = @{ Light = @(238, 204, 138); Dark = @(140, 113, 70); Fill = @(90, 73, 52) }
        Click   = @{ Light = @(88, 70, 44);    Dark = @(170, 138, 85); Fill = @(46, 37, 27) }
        Subtle  = @{ Light = @(178, 145, 89);  Dark = @(96, 77, 48);   Fill = @(58, 48, 36) }
        Tab     = @{ Light = @(206, 170, 106); Dark = @(126, 101, 62); Fill = @(74, 60, 45) }
        Window  = @{ Light = @(168, 137, 84);  Dark = @(88, 71, 45);   Fill = @(34, 28, 22) }
        Section = @{ Light = @(112, 91, 57);   Dark = @(62, 50, 32);   Fill = @(26, 21, 17) }
    }

    'Iron' = @{
        Ornament = 'Chamfer'; Pattern = 'Bricks'
        Radius = @{ Button = 4; Tab = 4; Window = 8; Section = 4 }
        Fillet = @{ Thin = 1; Fat = 2; WindowThin = 2; WindowFat = 3 }
        Button  = @{ Light = @(142, 152, 166); Dark = @(58, 63, 72);   Fill = @(42, 45, 52) }
        Hover   = @{ Light = @(198, 210, 226); Dark = @(84, 92, 104);  Fill = @(60, 65, 75) }
        Click   = @{ Light = @(52, 57, 66);    Dark = @(130, 140, 154); Fill = @(30, 33, 39) }
        Subtle  = @{ Light = @(126, 136, 150); Dark = @(52, 57, 66);   Fill = @(38, 41, 48) }
        Tab     = @{ Light = @(170, 182, 198); Dark = @(76, 83, 95);   Fill = @(50, 54, 63) }
        Window  = @{ Light = @(132, 142, 156); Dark = @(54, 59, 68);   Fill = @(24, 26, 31) }
        Section = @{ Light = @(88, 95, 106);   Dark = @(40, 44, 51);   Fill = @(18, 20, 24) }
    }

    'Royal' = @{
        Ornament = 'Double'; Pattern = 'Medieval'
        Radius = @{ Button = 13; Tab = 12; Window = 26; Section = 12 }
        Fillet = @{ Thin = 3; Fat = 6; WindowThin = 4; WindowFat = 9 }
        Button  = @{ Light = @(214, 176, 88);  Dark = @(110, 88, 40);  Fill = @(45, 42, 78) }
        Hover   = @{ Light = @(252, 220, 130); Dark = @(150, 120, 56); Fill = @(62, 58, 104) }
        Click   = @{ Light = @(96, 78, 36);    Dark = @(200, 164, 82); Fill = @(32, 30, 58) }
        Subtle  = @{ Light = @(196, 160, 80);  Dark = @(96, 78, 36);   Fill = @(40, 38, 70) }
        Tab     = @{ Light = @(238, 202, 116); Dark = @(140, 112, 52); Fill = @(54, 50, 92) }
        Window  = @{ Light = @(206, 170, 84);  Dark = @(104, 84, 38);  Fill = @(24, 22, 44) }
        Section = @{ Light = @(140, 114, 56);  Dark = @(70, 57, 26);   Fill = @(18, 17, 34) }
    }

    'Obsidian' = @{
        Ornament = 'Chamfer'; Pattern = 'Chevron'
        Radius = @{ Button = 6; Tab = 6; Window = 12; Section = 6 }
        Fillet = @{ Thin = 1; Fat = 2; WindowThin = 2; WindowFat = 3 }
        Button  = @{ Light = @(176, 186, 200); Dark = @(40, 43, 50);   Fill = @(24, 25, 30) }
        Hover   = @{ Light = @(230, 240, 255); Dark = @(62, 67, 78);   Fill = @(38, 40, 48) }
        Click   = @{ Light = @(36, 39, 46);    Dark = @(160, 170, 186); Fill = @(16, 17, 21) }
        Subtle  = @{ Light = @(150, 160, 176); Dark = @(36, 39, 46);   Fill = @(20, 21, 26) }
        Tab     = @{ Light = @(200, 212, 228); Dark = @(56, 60, 70);   Fill = @(30, 32, 38) }
        Window  = @{ Light = @(160, 170, 188); Dark = @(38, 41, 48);   Fill = @(14, 15, 18) }
        Section = @{ Light = @(96, 104, 118);  Dark = @(28, 30, 36);   Fill = @(10, 11, 14) }
    }

    'Verdant' = @{
        Ornament = 'Studs'; Pattern = 'Scales'
        Radius = @{ Button = 10; Tab = 9; Window = 20; Section = 9 }
        Fillet = @{ Thin = 2; Fat = 3; WindowThin = 3; WindowFat = 4 }
        Button  = @{ Light = @(164, 142, 84);  Dark = @(52, 66, 46);   Fill = @(36, 50, 36) }
        Hover   = @{ Light = @(214, 190, 118); Dark = @(72, 92, 62);   Fill = @(52, 70, 50) }
        Click   = @{ Light = @(46, 58, 40);    Dark = @(150, 130, 76); Fill = @(26, 38, 26) }
        Subtle  = @{ Light = @(148, 128, 76);  Dark = @(46, 58, 40);   Fill = @(32, 44, 32) }
        Tab     = @{ Light = @(190, 166, 100); Dark = @(64, 80, 54);   Fill = @(44, 60, 42) }
        Window  = @{ Light = @(150, 130, 78);  Dark = @(48, 60, 42);   Fill = @(20, 30, 22) }
        Section = @{ Light = @(96, 84, 50);    Dark = @(34, 44, 32);   Fill = @(15, 23, 17) }
    }

    'Bone' = @{
        Ornament = 'Bone'; Pattern = 'Dots'
        Radius = @{ Button = 13; Tab = 10; Window = 26; Section = 11 }
        Fillet = @{ Thin = 2; Fat = 2; WindowThin = 3; WindowFat = 3 }
        Button  = @{ Light = @(240, 227, 178); Dark = @(128, 109, 64);  Fill = @(44, 40, 31) }
        Hover   = @{ Light = @(255, 248, 208); Dark = @(160, 138, 84);  Fill = @(63, 57, 44) }
        Click   = @{ Light = @(114, 97, 58);   Dark = @(222, 209, 162); Fill = @(32, 29, 22) }
        Subtle  = @{ Light = @(218, 205, 158); Dark = @(108, 92, 55);   Fill = @(38, 35, 27) }
        Tab     = @{ Light = @(250, 240, 194); Dark = @(142, 122, 74);  Fill = @(52, 47, 37) }
        Window  = @{ Light = @(233, 219, 168); Dark = @(120, 102, 60);  Fill = @(28, 25, 19) }
        Section = @{ Light = @(166, 150, 102); Dark = @(80, 68, 41);    Fill = @(20, 18, 13) }
    }

    'Crimson' = @{
        Ornament = 'Bracket'; Pattern = 'Scales'
        Radius = @{ Button = 9; Tab = 8; Window = 18; Section = 8 }
        Fillet = @{ Thin = 2; Fat = 4; WindowThin = 3; WindowFat = 5 }
        Button  = @{ Light = @(196, 96, 76);   Dark = @(74, 30, 28);   Fill = @(58, 26, 26) }
        Hover   = @{ Light = @(240, 138, 108); Dark = @(104, 44, 40);  Fill = @(80, 36, 34) }
        Click   = @{ Light = @(66, 26, 24);    Dark = @(178, 86, 68);  Fill = @(40, 18, 18) }
        Subtle  = @{ Light = @(170, 84, 66);   Dark = @(64, 26, 24);   Fill = @(50, 23, 23) }
        Tab     = @{ Light = @(220, 116, 92);  Dark = @(90, 38, 34);   Fill = @(68, 30, 30) }
        Window  = @{ Light = @(178, 88, 68);   Dark = @(70, 28, 26);   Fill = @(32, 16, 16) }
        Section = @{ Light = @(112, 54, 44);   Dark = @(50, 20, 20);   Fill = @(22, 11, 11) }
    }

    'Arcane' = @{
        Ornament = 'Double'; Pattern = 'Dots'
        Radius = @{ Button = 12; Tab = 11; Window = 24; Section = 11 }
        Fillet = @{ Thin = 2; Fat = 3; WindowThin = 2; WindowFat = 4 }
        Button  = @{ Light = @(120, 226, 224); Dark = @(52, 40, 92);   Fill = @(34, 26, 62) }
        Hover   = @{ Light = @(170, 250, 248); Dark = @(74, 58, 126);  Fill = @(48, 38, 86) }
        Click   = @{ Light = @(44, 34, 78);    Dark = @(104, 200, 200); Fill = @(24, 18, 46) }
        Subtle  = @{ Light = @(104, 196, 196); Dark = @(46, 36, 82);   Fill = @(30, 23, 56) }
        Tab     = @{ Light = @(146, 240, 238); Dark = @(64, 50, 108);  Fill = @(42, 32, 76) }
        Window  = @{ Light = @(110, 208, 208); Dark = @(50, 38, 88);   Fill = @(20, 15, 38) }
        Section = @{ Light = @(70, 134, 136);  Dark = @(38, 29, 66);   Fill = @(14, 11, 28) }
    }

    'Wood' = @{
        Ornament = 'Fillet'; Pattern = 'Woodgrain'
        Radius = @{ Button = 2; Tab = 2; Window = 4; Section = 2 }
        Fillet = @{ Thin = 2; Fat = 2; WindowThin = 2; WindowFat = 2 }
        Button  = @{ Light = @(154, 114, 70);  Dark = @(52, 35, 20);   Fill = @(98, 68, 41) }
        Hover   = @{ Light = @(192, 146, 94);  Dark = @(70, 48, 28);   Fill = @(126, 90, 55) }
        Click   = @{ Light = @(48, 33, 19);    Dark = @(140, 104, 63); Fill = @(74, 51, 30) }
        Subtle  = @{ Light = @(138, 102, 62);  Dark = @(46, 31, 18);   Fill = @(86, 60, 36) }
        Tab     = @{ Light = @(174, 130, 82);  Dark = @(62, 43, 25);   Fill = @(110, 78, 47) }
        Window  = @{ Light = @(140, 104, 64);  Dark = @(48, 33, 19);   Fill = @(64, 44, 26) }
        Section = @{ Light = @(102, 74, 45);   Dark = @(36, 25, 15);   Fill = @(48, 33, 20) }
    }

    'Flesh' = @{
        Ornament = 'Studs'; Pattern = 'Hatch'
        Radius = @{ Button = 14; Tab = 12; Window = 26; Section = 12 }
        Fillet = @{ Thin = 3; Fat = 5; WindowThin = 3; WindowFat = 6 }
        Button  = @{ Light = @(206, 132, 126); Dark = @(96, 48, 48);   Fill = @(122, 68, 66) }
        Hover   = @{ Light = @(240, 168, 160); Dark = @(126, 66, 64);  Fill = @(150, 88, 84) }
        Click   = @{ Light = @(88, 44, 44);    Dark = @(186, 116, 110); Fill = @(96, 52, 50) }
        Subtle  = @{ Light = @(184, 116, 110); Dark = @(84, 42, 42);   Fill = @(108, 60, 58) }
        Tab     = @{ Light = @(224, 150, 142); Dark = @(110, 56, 54);  Fill = @(136, 78, 74) }
        Window  = @{ Light = @(190, 118, 112); Dark = @(88, 44, 44);   Fill = @(70, 38, 38) }
        Section = @{ Light = @(126, 76, 72);   Dark = @(62, 31, 31);   Fill = @(52, 27, 27) }
    }

    'Gothic' = @{
        Ornament = 'Gothic'; Pattern = 'Medieval'
        Radius = @{ Button = 4; Tab = 4; Window = 8; Section = 4 }
        Fillet = @{ Thin = 2; Fat = 2; WindowThin = 3; WindowFat = 3 }
        Button  = @{ Light = @(178, 172, 158); Dark = @(38, 37, 41);   Fill = @(28, 27, 32) }
        Hover   = @{ Light = @(226, 220, 202); Dark = @(58, 56, 63);   Fill = @(43, 42, 49) }
        Click   = @{ Light = @(32, 31, 36);    Dark = @(158, 152, 139); Fill = @(20, 19, 24) }
        Subtle  = @{ Light = @(150, 145, 133); Dark = @(34, 33, 38);   Fill = @(24, 23, 28) }
        Tab     = @{ Light = @(202, 195, 178); Dark = @(50, 48, 55);   Fill = @(36, 35, 42) }
        Window  = @{ Light = @(170, 164, 150); Dark = @(40, 39, 44);   Fill = @(18, 17, 22) }
        Section = @{ Light = @(114, 110, 100); Dark = @(30, 29, 34);   Fill = @(13, 12, 17) }
    }

    'Aero' = @{
        Ornament = 'None'; Pattern = 'Dots'; Outline = $false
        Radius = @{ Button = 16; Tab = 14; Window = 32; Section = 16 }
        Fillet = @{ Thin = 1; Fat = 1; WindowThin = 1; WindowFat = 1 }
        FillAlpha = 150; Gloss = 0.55
        Button  = @{ Light = @(226, 244, 255); Dark = @(96, 132, 160); Fill = @(58, 92, 122) }
        Hover   = @{ Light = @(255, 255, 255); Dark = @(126, 170, 200); Fill = @(82, 124, 158) }
        Click   = @{ Light = @(84, 116, 142);  Dark = @(200, 226, 245); Fill = @(40, 68, 92) }
        Subtle  = @{ Light = @(200, 224, 240); Dark = @(84, 116, 142); Fill = @(50, 80, 108) }
        Tab     = @{ Light = @(240, 250, 255); Dark = @(110, 148, 178); Fill = @(66, 104, 134) }
        Window  = @{ Light = @(210, 232, 248); Dark = @(88, 122, 150); Fill = @(30, 52, 72) }
        Section = @{ Light = @(140, 172, 196); Dark = @(60, 86, 108);  Fill = @(22, 40, 56) }
    }

    'Copper' = @{
        Ornament = 'Studs'; Pattern = 'Scales'
        Radius = @{ Button = 10; Tab = 9; Window = 20; Section = 9 }
        Fillet = @{ Thin = 2; Fat = 4; WindowThin = 3; WindowFat = 5 }
        Button  = @{ Light = @(214, 132, 74);  Dark = @(62, 96, 88);   Fill = @(58, 74, 70) }
        Hover   = @{ Light = @(248, 172, 108); Dark = @(84, 126, 116); Fill = @(76, 96, 90) }
        Click   = @{ Light = @(56, 86, 80);    Dark = @(190, 116, 64); Fill = @(42, 56, 52) }
        Subtle  = @{ Light = @(190, 116, 66);  Dark = @(56, 86, 80);   Fill = @(50, 66, 62) }
        Tab     = @{ Light = @(236, 156, 92);  Dark = @(74, 112, 104); Fill = @(66, 84, 79) }
        Window  = @{ Light = @(198, 122, 68);  Dark = @(58, 90, 84);   Fill = @(32, 44, 42) }
        Section = @{ Light = @(128, 82, 48);   Dark = @(44, 68, 63);   Fill = @(24, 33, 31) }
    }

    'Ash' = @{
        Ornament = 'Fillet'; Pattern = 'Dots'
        Radius = @{ Button = 9; Tab = 8; Window = 18; Section = 8 }
        Fillet = @{ Thin = 1; Fat = 2; WindowThin = 1; WindowFat = 2 }
        Button  = @{ Light = @(150, 146, 142); Dark = @(58, 56, 54);   Fill = @(48, 47, 46) }
        Hover   = @{ Light = @(200, 196, 190); Dark = @(84, 82, 79);   Fill = @(68, 67, 65) }
        Click   = @{ Light = @(52, 51, 50);    Dark = @(132, 128, 124); Fill = @(34, 33, 33) }
        Subtle  = @{ Light = @(130, 127, 123); Dark = @(52, 51, 49);   Fill = @(42, 41, 40) }
        Tab     = @{ Light = @(176, 172, 167); Dark = @(72, 70, 68);   Fill = @(56, 55, 54) }
        Window  = @{ Light = @(142, 139, 134); Dark = @(56, 54, 52);   Fill = @(30, 30, 29) }
        Section = @{ Light = @(96, 94, 91);    Dark = @(42, 41, 40);   Fill = @(22, 22, 21) }
    }
}

function New-Plate {
    param([string]$Name, [int]$Size, [int]$Radius, [string]$Style)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $max = $Size - 1
    $r = [double]$Radius
    $band = $Size / 4.0

    for ($y = 0; $y -le $max; $y++) {
        for ($x = 0; $x -le $max; $x++) {
            $dx = [Math]::Min($x, $max - $x)
            $dy = [Math]::Min($y, $max - $y)

            if ($dx -lt $r -and $dy -lt $r) {
                $ox = $r - $dx; $oy = $r - $dy
                $dist = $r - [Math]::Sqrt($ox * $ox + $oy * $oy)
            } else {
                $dist = [Math]::Min($dx, $dy)
            }

            $a = 0.0
            switch ($Style) {
                'Plate' {
                    $edge = [Math]::Min(1.0, [Math]::Max(0.0, ($dist - 0.5) / 2.5))
                    $t = $y / [double]$max
                    $a = $edge * (0.80 + 0.20 * $t)
                }
                'Bar' {
                    $a = [Math]::Min(1.0, [Math]::Max(0.0, ($dist - 0.5) / 2.5))
                }
                'Frame' {
                    $inner = $band * 0.55
                    $fall = [Math]::Min(1.0, [Math]::Max(0.0, ($inner - $dist) / $inner))
                    $edge = [Math]::Min(1.0, [Math]::Max(0.0, ($dist - 0.5) / 2.0))
                    $a = $fall * $fall * $edge
                }
            }

            $av = [int][Math]::Round(255 * [Math]::Min(1.0, [Math]::Max(0.0, $a)))
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($av, 255, 255, 255))
        }
    }

    $bmp.Save((Join-Path $OutDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  $Name.png"
}

function Sd-Seg {
    param([double]$px, [double]$py, [double]$x0, [double]$y0, [double]$x1, [double]$y1, [double]$w)
    $vx = $x1 - $x0; $vy = $y1 - $y0
    $len2 = $vx * $vx + $vy * $vy
    if ($len2 -lt 1e-9) { $t = 0.0 } else {
        $t = (($px - $x0) * $vx + ($py - $y0) * $vy) / $len2
        if ($t -lt 0) { $t = 0.0 } elseif ($t -gt 1) { $t = 1.0 }
    }
    $qx = $px - ($x0 + $t * $vx); $qy = $py - ($y0 + $t * $vy)
    [Math]::Sqrt($qx * $qx + $qy * $qy) - $w * 0.5
}

function Sd-Box {
    param([double]$px, [double]$py, [double]$x0, [double]$y0, [double]$x1, [double]$y1, [double]$r)
    $cx = ($x0 + $x1) * 0.5; $cy = ($y0 + $y1) * 0.5
    $hx = ($x1 - $x0) * 0.5 - $r; $hy = ($y1 - $y0) * 0.5 - $r
    if ($hx -lt 0) { $hx = 0.0 }
    if ($hy -lt 0) { $hy = 0.0 }
    $qx = [Math]::Abs($px - $cx) - $hx; $qy = [Math]::Abs($py - $cy) - $hy
    $mx = [Math]::Max($qx, 0.0); $my = [Math]::Max($qy, 0.0)
    [Math]::Sqrt($mx * $mx + $my * $my) + [Math]::Min([Math]::Max($qx, $qy), 0.0) - $r
}

function Sd-Tri {
    param([double]$px, [double]$py, [double[]]$v)
    $cross = ($v[2] - $v[0]) * ($v[5] - $v[1]) - ($v[3] - $v[1]) * ($v[4] - $v[0])
    $wind = 1.0
    if ($cross -lt 0) { $wind = -1.0 }

    $d = -1e9
    for ($i = 0; $i -lt 3; $i++) {
        $j = ($i + 1) % 3
        $ax = $v[$i * 2]; $ay = $v[$i * 2 + 1]
        $bx = $v[$j * 2]; $by = $v[$j * 2 + 1]
        $ex = $bx - $ax; $ey = $by - $ay
        $len = [Math]::Sqrt($ex * $ex + $ey * $ey)
        if ($len -lt 1e-9) { continue }
        $h = $wind * ((($px - $ax) * $ey - ($py - $ay) * $ex)) / $len
        if ($h -gt $d) { $d = $h }
    }
    $d
}

function Sd-Shape {
    param([object[]]$s, [double]$px, [double]$py)
    switch ($s[0]) {
        'seg'  { Sd-Seg $px $py $s[1] $s[2] $s[3] $s[4] $s[5] }
        'disc' { [Math]::Sqrt(($px - $s[1]) * ($px - $s[1]) + ($py - $s[2]) * ($py - $s[2])) - $s[3] }
        'ring' {
            $l = [Math]::Sqrt(($px - $s[1]) * ($px - $s[1]) + ($py - $s[2]) * ($py - $s[2]))
            [Math]::Abs($l - $s[3]) - $s[4] * 0.5
        }
        'box'  { Sd-Box $px $py $s[1] $s[2] $s[3] $s[4] $s[5] }
        'diam' {
            $l1 = [Math]::Abs($px - $s[1]) + [Math]::Abs($py - $s[2])
            (($l1 - $s[3]) / 1.4142) - $s[4]
        }
        'ering' {
            $t = -$s[6] * [Math]::PI / 180.0
            $ct = [Math]::Cos($t); $st = [Math]::Sin($t)
            $ox = $px - $s[1]; $oy = $py - $s[2]
            $rx = $ox * $ct - $oy * $st
            $ry = $ox * $st + $oy * $ct
            $aa = $s[3] * $s[3]; $bb = $s[4] * $s[4]
            $fv = ($rx * $rx) / $aa + ($ry * $ry) / $bb - 1
            $gx = 2 * $rx / $aa; $gy = 2 * $ry / $bb
            $gl = [Math]::Sqrt($gx * $gx + $gy * $gy)
            if ($gl -lt 1e-9) { 1e9 } else { [Math]::Abs($fv / $gl) - $s[5] * 0.5 }
        }
        'tri'  { Sd-Tri $px $py @($s[1], $s[2], $s[3], $s[4], $s[5], $s[6]) }
        default { 1e9 }
    }
}

$IconManifest = Join-Path $PSScriptRoot 'generated-icons.txt'
$GeneratedHashes = @{}
$ManifestExisted = Test-Path $IconManifest
if ($ManifestExisted) {
    foreach ($line in (Get-Content $IconManifest)) {
        $parts = $line -split '\s+', 2
        if ($parts.Count -eq 2) { $GeneratedHashes[$parts[0]] = $parts[1] }
    }
}
$KeptByHand = New-Object System.Collections.Generic.List[string]

function Get-PngHash {
    param([string]$Path)
    (Get-FileHash -Path $Path -Algorithm SHA256).Hash
}

function Test-HandDrawn {
    param([string]$Path, [string]$Name)

    if ($Force -or -not $ManifestExisted -or -not (Test-Path $Path)) { return $false }
    if (-not $GeneratedHashes.ContainsKey($Name)) { return $true }
    (Get-PngHash $Path) -ne $GeneratedHashes[$Name]
}

function New-Icon {
    param([string]$Name, [object[]]$Shapes, [int]$Size = 64, [double]$Outline = 2.2)

    $path = Join-Path $OutDir "$Name.png"
    if (Test-HandDrawn $path $Name) {
        [void]$KeptByHand.Add($Name)
        Write-Host "  $Name.png kept (hand drawn; -Force overwrites)" -ForegroundColor Yellow
        return
    }

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $max = $Size - 1
    $pad = $Outline + 2.0

    $prep = New-Object System.Collections.Generic.List[object]
    foreach ($raw in $Shapes) {
        $isSub = $raw[0].StartsWith('-')
        if ($isSub) {
            $copy = @($raw)
            $copy[0] = $raw[0].Substring(1)
            [void]$prep.Add(@($true, $copy))
        } else {
            [void]$prep.Add(@($false, $raw))
        }
    }

    $box = New-Object System.Collections.Generic.List[object]
    foreach ($pair in $prep) {
        $s = $pair[1]
        $x0 = -1e9; $y0 = -1e9; $x1 = 1e9; $y1 = 1e9
        switch ($s[0]) {
            'seg' {
                $g = ($s[5] * 0.5) + $pad
                $x0 = ([Math]::Min($s[1], $s[3]) - $g); $y0 = ([Math]::Min($s[2], $s[4]) - $g)
                $x1 = ([Math]::Max($s[1], $s[3]) + $g); $y1 = ([Math]::Max($s[2], $s[4]) + $g)
            }
            'disc' {
                $g = $s[3] + $pad
                $x0 = ($s[1] - $g); $y0 = ($s[2] - $g); $x1 = ($s[1] + $g); $y1 = ($s[2] + $g)
            }
            'ring' {
                $g = $s[3] + $s[4] + $pad
                $x0 = ($s[1] - $g); $y0 = ($s[2] - $g); $x1 = ($s[1] + $g); $y1 = ($s[2] + $g)
            }
            'box' {
                $x0 = ($s[1] - $pad); $y0 = ($s[2] - $pad); $x1 = ($s[3] + $pad); $y1 = ($s[4] + $pad)
            }
            'diam' {
                $g = $s[3] + ($s[4] * 1.4142) + $pad
                $x0 = ($s[1] - $g); $y0 = ($s[2] - $g); $x1 = ($s[1] + $g); $y1 = ($s[2] + $g)
            }
            'ering' {
                $g = [Math]::Max($s[3], $s[4]) + $s[5] + $pad
                $x0 = ($s[1] - $g); $y0 = ($s[2] - $g); $x1 = ($s[1] + $g); $y1 = ($s[2] + $g)
            }
            'tri' {
                $x0 = ([Math]::Min([Math]::Min($s[1], $s[3]), $s[5]) - $pad)
                $y0 = ([Math]::Min([Math]::Min($s[2], $s[4]), $s[6]) - $pad)
                $x1 = ([Math]::Max([Math]::Max($s[1], $s[3]), $s[5]) + $pad)
                $y1 = ([Math]::Max([Math]::Max($s[2], $s[4]), $s[6]) + $pad)
            }
        }
        [void]$box.Add(@($x0, $y0, $x1, $y1))
    }

    for ($y = 0; $y -le $max; $y++) {
        $py = $y + 0.5
        for ($x = 0; $x -le $max; $x++) {
            $px = $x + 0.5

            $d = 1e9
            $cut = -1e9
            for ($i = 0; $i -lt $prep.Count; $i++) {
                $b = $box[$i]
                if ($px -lt $b[0] -or $py -lt $b[1] -or $px -gt $b[2] -or $py -gt $b[3]) { continue }
                $ds = Sd-Shape $prep[$i][1] $px $py
                if ($prep[$i][0]) {
                    if (-$ds -gt $cut) { $cut = -$ds }
                } elseif ($ds -lt $d) {
                    $d = $ds
                }
            }
            if ($cut -gt $d) { $d = $cut }

            $outl = 0.5 - ($d - $Outline)
            if ($outl -le 0) { continue }
            if ($outl -gt 1) { $outl = 1.0 }

            $cov = 0.5 - $d
            if ($cov -lt 0) { $cov = 0.0 } elseif ($cov -gt 1) { $cov = 1.0 }

            $t = $cov / $outl
            $v = [int][Math]::Round(255 * $t)
            $a = [int][Math]::Round(255 * $outl)
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($a, $v, $v, $v))
        }
    }

    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $GeneratedHashes[$Name] = Get-PngHash $path
    Write-Host "  $Name.png"
}

function Ring-Points {
    param([int]$Count, [double]$Cx, [double]$Cy, [double]$R, [double]$Phase = 0.0)
    $out = New-Object System.Collections.Generic.List[object]
    for ($i = 0; $i -lt $Count; $i++) {
        $a = $Phase + 2 * [Math]::PI * $i / $Count
        [void]$out.Add(@(($Cx + $R * [Math]::Cos($a)), ($Cy + $R * [Math]::Sin($a))))
    }
    , $out.ToArray()
}

function Sun-Shapes {
    $s = New-Object System.Collections.Generic.List[object]
    [void]$s.Add(@('ring', 32, 32, 10, 5))
    foreach ($p in (Ring-Points -Count 8 -Cx 0 -Cy 0 -R 1)) {
        $a = [Math]::Atan2($p[1], $p[0])
        [void]$s.Add(@('seg', (32 + 17 * [Math]::Cos($a)), (32 + 17 * [Math]::Sin($a)),
                              (32 + 25 * [Math]::Cos($a)), (32 + 25 * [Math]::Sin($a)), 5))
    }
    , $s.ToArray()
}

function Gear-Shapes {
    $s = New-Object System.Collections.Generic.List[object]
    [void]$s.Add(@('ring', 32, 32, 13, 8))
    foreach ($p in (Ring-Points -Count 7 -Cx 32 -Cy 32 -R 18)) {
        [void]$s.Add(@('disc', $p[0], $p[1], 5))
    }
    , $s.ToArray()
}

function Zone-Shapes {
    $s = New-Object System.Collections.Generic.List[object]
    $cx = @(13, 51, 13, 51)
    $cy = @(13, 13, 51, 51)
    $sx = @(1, -1, 1, -1)
    $sy = @(1, 1, -1, -1)
    for ($i = 0; $i -lt 4; $i++) {
        [void]$s.Add(@('seg', $cx[$i], $cy[$i], ($cx[$i] + 13 * $sx[$i]), $cy[$i], 5))
        [void]$s.Add(@('seg', $cx[$i], $cy[$i], $cx[$i], ($cy[$i] + 13 * $sy[$i]), 5))
    }
    , $s.ToArray()
}

$Icons = @{
    'Orders'      = @(@('seg', 19, 52, 19, 11, 5), @('tri', 19, 11, 47, 19, 19, 29))
    'Zone'        = (Zone-Shapes)
    'Structure'   = @(@('box', 10, 19, 30, 31, 1.5), @('box', 34, 19, 54, 31, 1.5),
                      @('box', 10, 35, 21, 47, 1.5), @('box', 25, 35, 45, 47, 1.5), @('box', 49, 35, 54, 47, 1.5))
    'Production'  = @(@('box', 16, 13, 48, 27, 2.5), @('seg', 32, 27, 32, 53, 7))
    'Furniture'   = @(@('box', 9, 17, 17, 52, 2.5), @('box', 9, 31, 55, 43, 3), @('box', 48, 37, 55, 52, 2.5),
                      @('-box', 21, 33, 33, 41, 2))
    'Power'       = @(@('tri', 38, 9, 19, 35, 34, 35), @('tri', 30, 31, 45, 31, 25, 55))
    'Security'    = @(@('box', 12, 10, 52, 56, 10),
                      @('-tri', 0, 12, 45, 70, -40, 110),
                      @('-tri', 64, 12, 19, 70, 104, 110))
    'Misc'        = @(@('disc', 17, 32, 5.5), @('disc', 32, 32, 5.5), @('disc', 47, 32, 5.5))
    'Floors'      = @(@('box', 11, 11, 30, 30, 1.5), @('box', 34, 11, 53, 30, 1.5),
                      @('box', 11, 34, 30, 53, 1.5), @('box', 34, 34, 53, 53, 1.5))
    'Joy'         = @(@('disc', 21, 45, 8.5), @('seg', 29, 45, 29, 13, 5), @('seg', 29, 13, 48, 9, 5))
    'Ship'        = @(@('seg', 32, 16, 32, 42, 15), @('tri', 23, 35, 23, 53, 12, 53), @('tri', 41, 35, 41, 53, 52, 53))
    'Temperature' = @(@('disc', 32, 45, 9.5), @('seg', 32, 14, 32, 43, 9))

    'Ideology'    = (Sun-Shapes)
    'Biotech'     = @(@('seg', 38, 38, 48, 48, 10), @('seg', 26, 38, 16, 48, 10),
                      @('seg', 26, 26, 16, 16, 10), @('seg', 38, 26, 48, 16, 10))
    'Anomaly'     = @(@('tri', 32, 5, 24, 32, 40, 32), @('tri', 32, 59, 24, 32, 40, 32),
                      @('tri', 5, 32, 32, 24, 32, 40), @('tri', 59, 32, 32, 24, 32, 40))
    'Odyssey'     = @(@('disc', 32, 32, 11), @('ering', 32, 32, 24, 6, 3.5, -20))

    'Storage'     = @(@('box', 9, 14, 55, 25, 2), @('box', 14, 30, 50, 53, 2.5),
                      @('-box', 27, 36, 37, 47, 1.5))
    'Medical'     = @(@('seg', 32, 14, 32, 50, 12), @('seg', 14, 32, 50, 32, 12))
    'Vehicle'     = @(@('ring', 32, 32, 18, 6), @('disc', 32, 32, 7),
                      @('seg', 21, 21, 43, 43, 4), @('seg', 43, 21, 21, 43, 4))
    'Industry'    = (Gear-Shapes)
    'Nature'      = @(@('tri', 32, 8, 14, 40, 32, 54), @('tri', 32, 8, 50, 40, 32, 54), @('seg', 32, 44, 32, 58, 4))
    'Arcane'      = @(@('tri', 32, 8, 12, 43, 52, 43), @('tri', 32, 56, 12, 21, 52, 21))
    'Water'       = @(@('tri', 32, 9, 21, 33, 43, 33), @('disc', 32, 38, 12.5))

    'Blueprint'   = @(@('seg', 11, 13, 53, 13, 4.5), @('seg', 11, 51, 53, 51, 4.5),
                      @('seg', 11, 13, 11, 51, 4.5), @('seg', 53, 13, 53, 51, 4.5),
                      @('seg', 22, 24, 43, 24, 4), @('seg', 22, 24, 22, 41, 4))
    'Sign'        = @(@('box', 21, 14, 54, 50, 3), @('tri', 21, 11, 21, 53, 5, 32),
                      @('-disc', 25, 32, 4.5))
}

$Shapes = @{
    'Square'  = @(, @('box', 4, 4, 60, 60, 9))
    'Circle'  = @(, @('disc', 32, 32, 27))
    'Tag'     = @( @('box', 3, 7, 42, 57, 7), @('tri', 42, 7, 42, 57, 62, 32) )
    'Shield'  = @( @('box', 5, 5, 59, 38, 8), @('tri', 5, 34, 59, 34, 32, 62) )
    'Hex'     = @( @('box', 16, 6, 48, 58, 3), @('tri', 16, 6, 16, 58, 3, 32), @('tri', 48, 6, 48, 58, 61, 32) )
}

$Shapes['Diamond'] = @(, @('diam', 32, 32, 22, 5))
$SkinsRoot = Join-Path $PSScriptRoot '..\Skins'

if (-not $DefineOnly -and -not $IconsOnly) {
    if (-not (Test-Path $SkinsRoot)) { New-Item -ItemType Directory -Path $SkinsRoot -Force | Out-Null }
    [IO.File]::WriteAllText((Join-Path $SkinsRoot 'atlas-scale.txt'), "$Scale", (New-Object Text.UTF8Encoding($false)))
}

foreach ($id in ($(if ($IconsOnly -or $DefineOnly) { @() } elseif ($Only.Count -gt 0) { @($Themes.Keys | Where-Object { $Only -contains $_ } | Sort-Object) } else { $Themes.Keys | Sort-Object }))) {
    $t = $Themes[$id]
    $S = $Scale

    $fa = 255; if ($t.ContainsKey('FillAlpha')) { $fa = $t.FillAlpha }
    $gl = 0.0; if ($t.ContainsKey('Gloss'))     { $gl = $t.Gloss }
    $ol = $true; if ($t.ContainsKey('Outline')) { $ol = $t.Outline }
    $ed = 'Plain'; if ($t.ContainsKey('Edge')) { $ed = $t.Edge }
    $rc = 0.0; if ($t.ContainsKey('Recess')) { $rc = $t.Recess }
    $fl = $false; if ($t.ContainsKey('Flat')) { $fl = $t.Flat }

    $OutDir = Join-Path $SkinsRoot $id
    if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }

    Write-Host ""
    Write-Host "=== theme $id -> $OutDir ===" -ForegroundColor Cyan

    New-RoundAtlas -Name 'ButtonBG' -Size (64*$S) -Radius ($t.Radius.Button*$S) `
        -Thin ($t.Fillet.Thin*$S) -Fat ($t.Fillet.Fat*$S) `
        -Light $t.Button.Light -Dark $t.Button.Dark -Fill $t.Button.Fill -Ornament $t.Ornament -Edge $ed -Recess $rc -Flat $fl -FillAlpha $fa -Gloss $gl -Outline $ol

    New-RoundAtlas -Name 'ButtonBGMouseover' -Size (64*$S) -Radius ($t.Radius.Button*$S) `
        -Thin ($t.Fillet.Thin*$S) -Fat (($t.Fillet.Fat + 0.5)*$S) `
        -Light $t.Hover.Light -Dark $t.Hover.Dark -Fill $t.Hover.Fill -Ornament $t.Ornament -Edge $ed -Recess $rc -Flat $fl -FillAlpha $fa -Gloss $gl -Outline $ol

    New-RoundAtlas -Name 'ButtonBGClick' -Size (64*$S) -Radius ($t.Radius.Button*$S) `
        -Thin ($t.Fillet.Thin*$S) -Fat ($t.Fillet.Fat*$S) `
        -Light $t.Click.Light -Dark $t.Click.Dark -Fill $t.Click.Fill -Ornament $t.Ornament -Edge $ed -Recess $rc -Flat $fl -FillAlpha $fa -Gloss $gl -Outline $ol

    New-RoundAtlas -Name 'ButtonSubtleAtlas' -Size (64*$S) -Radius ($t.Radius.Button*$S) `
        -Thin ($t.Fillet.Thin*$S) -Fat ($t.Fillet.Fat*$S) `
        -Light $t.Subtle.Light -Dark $t.Subtle.Dark -Fill $t.Subtle.Fill -Ornament $t.Ornament -Edge $ed -Recess $rc -Flat $fl -FillAlpha $fa -Gloss $gl -Outline $ol

    New-TabAtlas -Name 'TabAtlas' -Scale $S -Radius ($t.Radius.Tab*$S) `
        -Thin ($t.Fillet.Thin*$S) -Fat ($t.Fillet.Fat*$S) `
        -Light $t.Tab.Light -Dark $t.Tab.Dark -Fill $t.Tab.Fill

    New-RoundAtlas -Name 'WindowAtlas' -Size (128*$S) -Radius ($t.Radius.Window*$S) `
        -Thin ($t.Fillet.WindowThin*$S) -Fat ($t.Fillet.WindowFat*$S) `
        -Light $t.Window.Light -Dark $t.Window.Dark -Fill $t.Window.Fill -Ornament $t.Ornament -Edge $ed -Recess $rc -Flat $fl -FillAlpha $fa -Gloss $gl -Outline $ol

    New-RoundAtlas -Name 'SectionAtlas' -Size (64*$S) -Radius ($t.Radius.Section*$S) `
        -Thin ($t.Fillet.Thin*$S) -Fat ($t.Fillet.Fat*$S) `
        -Light $t.Section.Light -Dark $t.Section.Dark -Fill $t.Section.Fill -Ornament $t.Ornament -Edge $ed -Recess $rc -Flat $fl -FillAlpha $fa -Gloss $gl -Outline $ol

    New-RoundAtlas -Name 'SliderRail' -Size (64*$S) -Radius (8*$S) -Thin (2*$S) -Fat (2*$S) `
        -Light $t.Section.Light -Dark $t.Section.Dark -Fill $t.Section.Fill -Ornament 'Fillet' -Outline $ol

    New-RoundAtlas -Name 'TooltipBG' -Size (64*$S) -Radius ($t.Radius.Section*$S) `
        -Thin ($t.Fillet.Thin*$S) -Fat ($t.Fillet.Fat*$S) `
        -Light $t.Tab.Light -Dark $t.Tab.Dark -Fill $t.Window.Fill -Ornament $t.Ornament -Edge $ed -Recess $rc -Flat $fl -FillAlpha $fa -Gloss $gl -Outline $ol

    New-RoundAtlas -Name 'FloatMenuOptionBG' -Size (64*$S) -Radius (6*$S) -Thin (1*$S) -Fat (2*$S) `
        -Light $t.Subtle.Light -Dark $t.Subtle.Dark -Fill $t.Subtle.Fill -Ornament 'Fillet' -Outline $ol

    New-RoundAtlas -Name 'GizmoBG' -Size (64*$S) -Radius (6*$S) -Thin (2*$S) -Fat (3*$S) `
        -Light $t.Button.Light -Dark $t.Button.Dark -Fill $t.Button.Fill -Ornament $t.Ornament -Edge $ed -Recess $rc -Flat $fl -FillAlpha $fa -Gloss $gl -Outline $ol

    New-Checkbox -Name 'CheckOn'      -State 'On'      -Light $t.Button.Light -Dark $t.Button.Dark -Fill $t.Section.Fill -Mark $t.Tab.Light
    New-Checkbox -Name 'CheckOff'     -State 'Off'     -Light $t.Button.Light -Dark $t.Button.Dark -Fill $t.Section.Fill -Mark $t.Tab.Light
    New-Checkbox -Name 'CheckPartial' -State 'Partial' -Light $t.Button.Light -Dark $t.Button.Dark -Fill $t.Section.Fill -Mark $t.Tab.Light

    New-Radio -Name 'RadioButOn'  -On $true  -Light $t.Button.Light -Dark $t.Button.Dark -Fill $t.Section.Fill -Mark $t.Tab.Light
    New-Radio -Name 'RadioButOff' -On $false -Light $t.Button.Light -Dark $t.Button.Dark -Fill $t.Section.Fill -Mark $t.Tab.Light

    New-Knob -Name 'SliderHandle' -Light $t.Tab.Light -Dark $t.Button.Dark -Fill $t.Button.Fill

    New-Plate -Name 'Plate'      -Size (64*$S) -Radius ($t.Radius.Button*$S) -Style 'Plate'
    New-Plate -Name 'PlateFrame' -Size (64*$S) -Radius ($t.Radius.Button*$S) -Style 'Frame'

    New-Strip -Name 'BarFill' -W 16 -H 32 `
        -Top $t.Tab.Light -Bottom $t.Button.Dark -Edge $t.Button.Dark

    New-Strip -Name 'BarBG' -W 16 -H 32 `
        -Top $t.Section.Fill -Bottom $t.Window.Fill -Edge $t.Section.Dark

    New-Strip -Name 'ScrollTrack' -W 16 -H 32 `
        -Top $t.Window.Fill -Bottom $t.Section.Fill -Edge $t.Section.Dark -Outline $false

    New-Strip -Name 'ScrollThumb' -W 16 -H 32 `
        -Top $t.Button.Light -Bottom $t.Button.Dark -Edge $t.Button.Dark

    foreach ($k in @('Hatch', 'Medieval', 'Scales', 'Bricks', 'Dots', 'Chevron', 'Woodgrain')) {
        New-Pattern -Kind $k -InkA $t.Section.Light -InkB $t.Tab.Light -Wash $t.Window.Light
    }
}

$OutDir = Join-Path $SkinsRoot 'Shared'
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }

Write-Host ""
Write-Host "=== icons -> $OutDir ===" -ForegroundColor Cyan
foreach ($name in ($(if ($DefineOnly) { @() } else { $Icons.Keys | Sort-Object }))) {
    New-Icon -Name "Icon$name" -Shapes $Icons[$name]
}

foreach ($name in ($(if ($DefineOnly) { @() } else { $Shapes.Keys | Sort-Object }))) {
    New-Icon -Name "Shape$name" -Shapes $Shapes[$name] -Outline 0.0
}

if (-not $DefineOnly) {
    $lines = $GeneratedHashes.Keys | Sort-Object | ForEach-Object { "$_ $($GeneratedHashes[$_])" }
    Set-Content -Path $IconManifest -Value $lines -Encoding ascii

    if ($KeptByHand.Count -gt 0) {
        Write-Host ""
        Write-Host "$($KeptByHand.Count) hand drawn, left alone: $($KeptByHand -join ', ')" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "OK: $($Themes.Count) themes, $($Icons.Count) icons (scale ${Scale}x)" -ForegroundColor Green

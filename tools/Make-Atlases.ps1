<#
  Generates every UI texture in Skins/.

  VISUAL LANGUAGE
  Genuinely rounded corners (not a chamfer, not an L-bracket), with a brass fillet
  that THICKENS at the corner and thins along the edge - forged metal rather than a
  square piece glued on. A black outline around everything, which is what separates
  the piece from the background and makes it read as drawn instead of smudged.

  Bevel: light brass top-left, dark bottom-right.

  TWO FORMATS, DIFFERENT RULES

  1) 9-slice atlases (ButtonBG*, ButtonSubtleAtlas, WindowAtlas, SectionAtlas) go
     through Widgets.DrawAtlas. Corner = width * 0.25, clamped to
     min(rect.h/2, rect.w/2). The centre slice is stretched on BOTH axes so it has
     to be flat; the edges stretch on ONE axis.
     Hence fillet thickness may only vary INSIDE the corner region (cx and cy both
     < Size/4). Along the edge bands it must be constant or it stretches into a
     smear. The code below respects that.

  2) TabAtlas is NOT 9-slice. TabRecord.Draw cuts it in three horizontally with
     hardcoded pixels: 0..30 / 30..34 stretched / 34..64. The texture must be
     EXACTLY 64px wide. Only the ends may be rounded; columns 30..33 have to be
     uniform horizontally.

  Widgets.DrawTexturePart flips the UV Y (uvRect.y = 1 - y - height), so the game's
  UV rects are TOP-DOWN, same as the PNG: a light bevel at the top of the file comes
  out at the top of the screen.
#>
param(
    [switch]$IconsOnly,
    [switch]$DefineOnly,
    # Texel density of the 9-slice atlases, the tab and the plates. MUST match
    # AtlasSwap.Scale in the C#: the draw divides the corner by it, which is what
    # keeps the on-screen geometry identical while the texels double.
    #
    # Deliberately NOT applied to the patterns, the checkbox, the radio, the slider
    # knob or the strips. Those are drawn at or below 1:1 already, so density buys
    # them nothing, and the patterns hardcode feature periods that have to divide
    # the tile for the tiling to close.
    [int]$Scale = 2,

    # Regenerate only these themes. A full run is minutes; iterating on one theme
    # should not be.
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

<#
  9-slice atlas with a rounded corner and a variable fillet.
    Size       side in pixels (the 9-slice corner is Size/4)
    Radius     corner radius
    Thin       fillet thickness along the edge
    Fat        fillet thickness at the corner
#>
function New-RoundAtlas {
    param(
        [string]$Name, [int]$Size, [int]$Radius, [double]$Thin, [double]$Fat,
        [int[]]$Light, [int[]]$Dark, [int[]]$Fill,

        # How the corner is treated. This is what makes a theme read as ANOTHER UI
        # rather than the same one in a different colour:
        #   Fillet   fillet thickens and lightens at the corner (forged metal)
        #   Bracket  L-bracket laid over the corner (bolted-on hardware)
        #   Chamfer  corner cut at 45 degrees (chipped stone, austere)
        #   Studs    round rivet set into each corner
        #   Double   two concentric lines (engraving frame)
        #   Bone     two round condyles per corner (the end of a long bone)
        #   Gothic   double moulding + a tracery trefoil in the corner
        #   None     nothing but the rim, for materials with no hardware
        [string]$Ornament = 'Fillet',

        # Opacity of the INTERIOR, 0-255. The frame stays opaque: it is what
        # separates the piece from the background. Only the fill lets the map show.
        [int]$FillAlpha = 255,

        # Vertical gradient on the interior, light at the top. The 9-slice centre is
        # stretched, but a VERTICAL gradient survives vertical stretching - it just
        # scales with it. This is what gives the glass "bubble" sheen.
        [double]$Gloss = 0.0,

        # 1px black outline around everything. It separates the piece from the
        # background, so it stays on in nearly every theme. Turning it off only makes
        # sense where the piece should NOT have a hard edge - glass, where black
        # reads as a plastic rim and kills the bubble.
        [bool]$Outline = $true,

        # Cross-section of the border, i.e. what happens as you walk INWARD from the
        # rim. This is the only kind of detail an edge band can carry: the band is
        # stretched along its run, so anything that varies along the length smears,
        # while everything that varies across it survives untouched. A moulding
        # profile is exactly that shape, which is why these read as carpentry.
        #   Plain   fillet, inner shadow, fill (the original)
        #   Ribbed  fillet, gap, thin bright rule, gap (blind-tooled bookbinding)
        #   Step    terraced shoulders stepping down into the fill (machined)
        #   Cove    concave sweep from fillet to fill, with a bead at the bottom
        #   Rail    two thin rules with a dark channel between them (bus bar)
        #   Bead    a single rounded half-round moulding
        [string]$Edge = 'Plain',

        # Depth, in 1x pixels, of a recess cut into the interior just inside the
        # border. The shading is INVERTED against the usual bevel - shadow on the
        # top-left lip, light on the bottom-right - which is what reads as sunk
        # rather than raised, and is what makes an applied corner piece sit proud
        # of the surface around it.
        [double]$Recess = 0.0,

        # Flat shading: no ramp across the fillet, no ramp into the inner shadow,
        # and a two-tone corner piece instead of a smooth relief. Every one of those
        # ramps is legible on its own, but stacked on one theme they read as noise,
        # and the piece stops having a silhouette you can name.
        [bool]$Flat = $false
    )

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $clear = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)
    $max = $Size - 1
    $zone = $Size / 4    # 9-slice corner region
    $scale = $Size / 64.0

    # Depth of the edge profile, past the fillet. Zero for Plain so the fourteen
    # existing themes come out byte for byte identical.
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

            # signed distance to the outline
            if ($Ornament -eq 'Chamfer') {
                # 45-degree cut: Chebyshev distance clamped by the diagonal
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

            if ($Ornament -eq 'Scallop' -and $cx -lt $zone -and $cy -lt $zone) {
                # Amplitude baixa de proposito: a 3.2 o canto vinha rasgado em vez de
                # recortado. Louca tem lobulo raso e largo.
                $sa = [Math]::Atan2([Math]::Max(0.01, $zone - $cy), [Math]::Max(0.01, $zone - $cx))
                $dist -= [Math]::Abs([Math]::Sin(2.0 * $sa)) * 1.1 * $scale
            }

            if ($dist -lt 0) {
                $bmp.SetPixel($x, $y, $clear)
                continue
            }

            # "how close to the corner" - only valid INSIDE the 9-slice corner
            # region. Along the edge bands everything must be constant, or the slice
            # gets stretched and the gradient smears.
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
                # the metal also LIGHTENS where it thickens: reads as a polished
                # piece rather than "the border got thicker here"
                $metal = Blend $metal @(255, 236, 194) ($cornerness * 0.55)
            }
            elseif ($Ornament -eq 'Bone') {
                # the condyle catches more light than the shaft, like any convex
                # surface: the centre blows out, the rim falls to shadow.
                # Warm ivory, never white.
                $metal = Blend (Blend $metal $Black 0.18) @(255, 250, 224) ($lobe * 1.3)
            }

            # --- ornaments added on top of the outline ---------------------
            $ornamentHit = $false
            $plate = 0.0
            $bcU = 0.0
            $ow = [Math]::Max(1.0, $scale * 0.8)

            if ($Ornament -eq 'Bracket') {
                # overlaid L, hugging the corner
                $arm = [int](13 * $scale)
                $bar = [int](3 * $scale)
                $ornamentHit = ($cx -lt $arm -and $cy -lt $arm) -and
                               (($cx -le $bar) -or ($cy -le $bar)) -and ($dist -ge 1)
            }
            elseif ($Ornament -eq 'BookCorner') {
                # The metal corner piece nailed to a bound book. Built as a signed
                # DISTANCE rather than a boolean hit, which is what buys the two
                # things a boolean cannot: an outline that follows the piece on the
                # inside as well as the outside, and a relief that knows how far it
                # is from either edge.
                $reach = 13.5 * $scale
                $diag = $cx + $cy

                # Distance to the far side of the plate, measured perpendicular to
                # the diagonal. 1.4142 converts the sum-of-axes into real distance.
                $toInner = ($reach - $diag) / 1.4142
                $toOuter = $dist - 1.0

                $plate = [Math]::Min($toOuter, $toInner)
                $ornamentHit = ($plate -gt 0) -and ($dist -ge 1)

                if ($ornamentHit) {
                    # 1 hard against the rim, 0 at the inner edge.
                    $bcU = $toInner / [Math]::Max(0.001, $toOuter + $toInner)
                }
            }
            elseif ($Ornament -eq 'Trace') {
                # A right-angled circuit trace running past the corner into a via.
                $off = 6.0 * $scale
                $w = 1.3 * $scale

                # Clamped INSIDE the corner region. A trace that reached past $zone
                # would land in the edge band, which is stretched along its run, and
                # the far end would come out as one smeared dash near the corner.
                $run = [Math]::Min(20.0 * $scale, $zone - 1.0)
                $onX = ([Math]::Abs($cy - $off) -lt $w) -and ($cx -lt $run) -and ($cx -gt $off)
                $onY = ([Math]::Abs($cx - $off) -lt $w) -and ($cy -lt $run) -and ($cy -gt $off)
                $vdx = $cx - $off; $vdy = $cy - $off
                $vr = [Math]::Sqrt($vdx * $vdx + $vdy * $vdy)
                $via = ([Math]::Abs($vr - 2.6 * $scale) -lt (1.2 * $scale))
                $ornamentHit = ($onX -or $onY -or $via) -and ($dist -ge 1)
            }

            elseif ($Ornament -eq 'Rivets') {
                # Three rivets in an arc across the corner: a bolted plate, heavier
                # than the single stud of Studs.
                $ornamentHit = $false
            $plate = 0.0
            $bcU = 0.0
            $ow = [Math]::Max(1.0, $scale * 0.8)
                $rr = 3.1 * $scale
                foreach ($a in @(22.0, 68.0)) {
                    $rad = $a * [Math]::PI / 180.0
                    $px = 8.0 * $scale + 5.0 * $scale * [Math]::Cos($rad)
                    $py = 8.0 * $scale + 5.0 * $scale * [Math]::Sin($rad)
                    $rdx = $cx - $px; $rdy = $cy - $py
                    if ((($rdx * $rdx) + ($rdy * $rdy)) -lt ($rr * $rr)) {
                        $ornamentHit = $true
                        # reaproveita o sombreado de cupula do Studs
                        $ddx = $rdx; $ddy = $rdy
                    }
                }
                $ornamentHit = $ornamentHit -and ($dist -ge 1)
            }
            elseif ($Ornament -eq 'Fan') {
                # Quarter fan: five ribs radiating from the corner, deliberately
                # asymmetric in weight so it does not read as a rosette.
                $ang = [Math]::Atan2([Math]::Max(0.01, $cy), [Math]::Max(0.01, $cx))
                $rib = [Math]::Abs([Math]::Sin(4.0 * $ang))
                $reach = 14.0 * $scale
                $r = [Math]::Sqrt($cx * $cx + $cy * $cy)
                # A largura angular de um raio vira largura real proporcional ao raio,
                # entao o limiar aperta perto do vertice: sem isso eles se encontram
                # no centro e o leque fecha numa cunha solida.
                $tight = 0.94 - 0.10 * [Math]::Min(1.0, $r / $reach)
                $ornamentHit = ($r -lt $reach) -and ($r -gt 6.0 * $scale) -and
                               ($rib -gt $tight) -and ($dist -ge 1)
            }
            elseif ($Ornament -eq 'Studs') {
                # round rivet, set back from the corner
                $sc = [int](7 * $scale)
                $sr = [double](3.2 * $scale)
                $ddx = $cx - $sc
                $ddy = $cy - $sc
                $ornamentHit = (($ddx * $ddx) + ($ddy * $ddy)) -le ($sr * $sr)
            }
            elseif ($Ornament -eq 'Double') {
                # second concentric line, parallel to the frame
                $gap = 3 * $scale
                $ornamentHit = ($dist -ge (1 + $thick + $gap)) -and
                               ($dist -lt (1 + $thick + $gap + [Math]::Max(1, $scale)))
            }
            elseif ($Ornament -eq 'Bone' -and $cx -lt $Radius -and $cy -lt $Radius) {
                # The end of a long bone does not taper, it opens into TWO condyles -
                # two round bosses with a narrow valley between them. That, not the
                # colour, is what makes someone read "bone".
                #
                # First attempt thickened the fillet along |sin(4a)|. That puts two
                # peaks in the right places but draws a solid fan stuck to the
                # corner: thickness has no silhouette, and silhouette was the point.
                #
                # So: two real CIRCLES centred on the corner arc, sunk far enough to
                # meet the frame. 12 and 78 degrees, not 18 and 72 - with the centres
                # nearer the ends of the arc their separation exceeds the sum of the
                # radii and the two stop merging into one. Two bosses with a valley
                # is the drawing; one fat boss is just a thick border.
                $ang1 = 12.0 * [Math]::PI / 180.0
                $ang2 = 78.0 * [Math]::PI / 180.0
                $kd = 3.2 * $scale       # how far the centre sinks inward
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
                # Two parts: the double moulding, which runs all the way round at a
                # constant thickness and is therefore safe in the stretched bands,
                # and the TREFOIL, which exists only in the corner region.
                #
                # Three lobes at 45/165/285 degrees, chosen so the drawing is
                # symmetric about the diagonal: reflecting cx<->cy maps angle t to
                # 90-t, and {45,165,285} is closed under that. Without the symmetry
                # the four corners would not match each other.
                $gap = 3 * $scale
                $ornamentHit = ($dist -ge (1 + $thick + $gap)) -and
                               ($dist -lt (1 + $thick + $gap + [Math]::Max(1, $scale)))

                if (-not $ornamentHit -and $cx -lt $zone -and $cy -lt $zone) {
                    $tc = 9.0 * $scale       # trefoil centre, measured from the edge
                    $ld = 3.1 * $scale       # offset of each lobe
                    $lr = 3.1 * $scale       # lobe radius
                    $w  = [Math]::Max(1.0, 0.9 * $scale)

                    # tc + ld + lr must fit inside $zone, or the tracery spills into
                    # the stretched band and turns into a streak.
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

                    # each lobe''s stroke stops where it enters another: that is how a
                    # trefoil is drawn, the arcs cut each other.
                    $ornamentHit = (-not $inside) -and ($best -le ($w * 0.5)) -and
                                   ($dist -ge (1 + $thick))
                }
            }

            # interior, with the vertical sheen already applied
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
                # without an outline the edge becomes the frame itself: the pale
                # glass rim instead of black drawn around it
                if ($Outline) { $c = $Black } else { $c = $metal }
            }
            elseif ($ornamentHit -and ($Ornament -eq 'Studs' -or $Ornament -eq 'Rivets')) {
                # rivet with a highlight on top and shadow underneath
                $lit = ($ddx + $ddy) -lt 0
                $c = if ($lit) { Blend $Light @(255, 240, 210) 0.4 } else { Blend $Dark $Black 0.3 }
            }
            elseif ($ornamentHit -and $Ornament -eq 'BookCorner') {
                if ($toInner -lt $ow) {
                    # ONLY the inner edge. The outer edge already sits against the
                    # rim's own outline, and drawing another there stacks two black
                    # texels into one line of twice the asked width.
                    $c = $Black
                }
                elseif ($Flat) {
                    # Two flat tones rather than a ramp: still reads as a folded
                    # piece of metal, with nothing to smear at small sizes.
                    if ($bcU -gt 0.45) { $c = $Light } else { $c = $Dark }
                }
                else {
                    # Raised: the edge against the rim catches the light, the inner
                    # edge falls away into shadow.
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
                # Distance INTO the profile, past the fillet.
                $d = $dist - 1 - $thick
                $u = $d / $profileDepth
                $alpha = $FillAlpha

                switch ($Edge) {
                    'Ribbed' {
                        # gap, bright rule, gap. The rule is what the eye reads as
                        # tooling on a book board.
                        if ($d -lt $r1) { $c = Blend $body $Black 0.5 }
                        elseif ($d -lt $r2) { $c = Blend $metal @(255, 244, 214) 0.35; $alpha = 255 }
                        else { $c = Blend $body $Black 0.3 }
                    }
                    'Step' {
                        # each tread darker than the last: a shoulder, not a slope
                        $tread = [Math]::Floor($u * 3.0)
                        $c = Blend (Blend $metal $Black 0.25) $body (($tread + 1) / 3.5)
                        if ($d -lt 1.2) { $c = Blend $metal @(255, 240, 210) 0.25; $alpha = 255 }
                    }
                    'Cove' {
                        # quarter-circle sweep: dark at the top of the curve, opening
                        # into the fill, with a lit bead at the very bottom
                        $sweep = 1.0 - [Math]::Sqrt([Math]::Max(0.0, 1.0 - $u * $u))
                        $c = Blend (Blend $body $Black 0.55) $body $sweep
                        if ($u -gt 0.82) { $c = Blend $c @(255, 240, 210) 0.5; $alpha = 255 }
                    }
                    'Rail' {
                        # two rules with a dark channel: reads as a bus, not a frame
                        if ($d -lt $r1) { $c = Blend $body $Black 0.65 }
                        elseif ($d -lt $r2) { $c = $metal; $alpha = 255 }
                        else { $c = Blend $body $Black 0.4 }
                    }
                    'Bead' {
                        # half-round: brightest at the crown, falling off both ways
                        $crown = 1.0 - [Math]::Abs($u - 0.5) * 2.0
                        $c = Blend (Blend $metal $Black 0.45) (Blend $metal @(255, 245, 220) 0.5) $crown
                        $alpha = 255
                    }
                    default { $c = $body }
                }
            }
            elseif ($dist -lt 1 + $thick + $profileDepth + $recessDepth) {
                $alpha = $FillAlpha
                if ($Recess -le 0) {
                    if ($Flat) { $c = Blend $body $Black 0.28 }
                    else { $c = Blend $body $Black 0.45 }     # sombra interna
                }
                elseif ($Flat) {
                    # Two flat steps rather than a ramp. The direction is what does
                    # the work: shadow on the top-left lip, light on the bottom-right
                    # is the INVERTED bevel, and inverted is what reads as sunk.
                    if ($nearTopLeft) { $c = Blend $body $Black 0.45 }
                    else { $c = Blend $body @(255, 246, 220) 0.16 }
                }
                else {
                    # Deep into the recess the light returns to the flat interior.
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

<#
  TabAtlas: 64x32 required. Rounds only the top, and only at the ends.
  A ultima linha fica aberta (a aba encosta no painel de baixo).
#>
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

            $isEnd = ($x -lt (30 * $Scale)) -or ($x -ge (34 * $Scale))   # miolo uniforme
            $ex = [Math]::Min($x, $w - 1 - $x)     # distancia a lateral

            $db = $h - 1 - $y                      # distancia ate embaixo

            if ($isEnd -and $y -lt $Radius -and $ex -lt $Radius) {
                $dx = $Radius - $ex
                $dy = $Radius - $y
                $dist = $Radius - [Math]::Sqrt($dx * $dx + $dy * $dy)
            }
            elseif ($isEnd) {
                # the ENDS close at the bottom too, giving the tab a foot. The middle
                # stays open so the selected tab merges with the panel below.
                $dist = [Math]::Min([Math]::Min($ex, $y), $db)
            }
            else {
                $dist = $y                          # miolo: so a borda de cima
            }

            if ($Ornament -eq 'Scallop' -and $cx -lt $zone -and $cy -lt $zone) {
                # Amplitude baixa de proposito: a 3.2 o canto vinha rasgado em vez de
                # recortado. Louca tem lobulo raso e largo.
                $sa = [Math]::Atan2([Math]::Max(0.01, $zone - $cy), [Math]::Max(0.01, $zone - $cx))
                $dist -= [Math]::Abs([Math]::Sin(2.0 * $sa)) * 1.1 * $scale
            }

            if ($dist -lt 0) {
                $bmp.SetPixel($x, $y, $clear)
                continue
            }

            # fillet thickens near the top of the ends
            # thicker at the top of the ends, slightly less at the foot
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

<#
  NOISE FOR THE PATTERNS.

  This cannot be 9-slice: the centre slice stretches on both axes and any pattern
  baked there smears. So the background is drawn separately, with
  GUI.DrawTextureWithTexCoords and UVs proportional to the rect, so it repeats
  instead of stretching. Requires wrapMode Repeat on the texture.

  Value noise on a lattice that wraps (modulo), so the texture matches itself on
  all four edges. Two octaves: a broad one splitting the two tones into patches,
  and a fine one dirtying the surface.
#>

function Hash01 {
    param([int]$x, [int]$y, [int]$seed)

    # Mask to 31 bits AFTER each multiply. Without it the product overflows int64,
    # PowerShell promotes to double, and the following -band fails with
    # InvalidCast - an error easy to miss, because it only corrupts some pixels
    # and the PNG is written anyway.
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

    # smoothstep, so the lattice grid does not show
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

function New-GrungeFill {
    param(
        [string]$Name, [int]$Size,
        [int[]]$ToneA, [int[]]$ToneB, [double]$Dirt
    )

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

    for ($y = 0; $y -lt $Size; $y++) {
        for ($x = 0; $x -lt $Size; $x++) {

            # broad octave: splits the two tones into patches. Period Size/8, not
            # Size/4 - patches any larger start reading as camouflage.
            $blotch = ValueNoise $x $y ([int]($Size / 4)) $Size 11
            # oitava fina: sujeira
            $grain = ValueNoise $x $y ([int]($Size / 12)) $Size 37

            # TWO TONES, no middle ground: no Blend between them, a pixel is one or
            # the other. The fine octave only pushes the threshold, so the boundary
            # between patches is ragged instead of a smooth contour - that is what
            # reads as grunge rather than as a gradient.
            $threshold = 0.5 + ($grain - 0.5) * $Dirt
            if ($blotch -lt $threshold) { $c = $ToneA } else { $c = $ToneB }

            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c[0], $c[1], $c[2]))
        }
    }

    $bmp.Save((Join-Path $OutDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  $Name.png  ($Size x $Size, ladrilhavel)"
}



<#
  BACKGROUND PATTERNS.

  These used to be 1-bit alpha masks (alpha 0 or 255) tinted black by the code. The
  result was jagged and monochrome - it read as drawn in Paint. Each pattern is now
  COLOURED and anti-aliased:

  1. COVERAGE instead of on/off. Each pattern computes the distance to its feature
     (line, arc, dot) and converts it to a soft alpha. A 1px edge with intermediate
     alpha is what separates "a line" from "a staircase".

  2. TWO INKS. Each pixel mixes two inks from the theme according to low-frequency
     noise, so the stroke is not one flat colour.

  3. A WASH ON TOP. A broad, very weak mottle over the whole tile. This is what
     removes the flat look without becoming a gradient: it is irregular, not
     directional.

  PERFORMANCE: geometry and noise are IDENTICAL across themes - only the inks
  differ. So the map (coverage + mix + wash) is computed ONCE per pattern kind and
  reused by all 14 themes. Without that it is 126 maps instead of 9, and the script
  takes minutes instead of seconds.

  All of them tile: the periods divide 128 and the noise lattice wraps.
#>

# cache: pattern kind -> @{ Cov=[double[]]; Mix=[double[]]; Wash=[double[]] }
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

            # d = distance in pixels to the pattern feature
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

                'Veins' {
                    # a fronteira da mancha, convertida em pixels aproximados
                    $d = [Math]::Abs((ValueNoise $x $y 24 $Size 17) - 0.5) * 45
                }

                'Tracery' {
                    $cx2 = $x % 32
                    $cy2 = $y % 40
                    if ($cy2 -lt 40) {
                        $r1 = [Math]::Sqrt($cx2 * $cx2 + ($cy2 - 40) * ($cy2 - 40))
                        $r2 = [Math]::Sqrt(($cx2 - 32) * ($cx2 - 32) + ($cy2 - 40) * ($cy2 - 40))
                        $d = [Math]::Min([Math]::Abs($r1 - 32), [Math]::Abs($r2 - 32))
                    }
                }

                'Stars' {
                    foreach ($c in $starCenters) {
                        $sx = [Math]::Abs($x - $c[0]) / 13.0
                        $sy = [Math]::Abs($y - $c[1]) / 13.0
                        if ($sx -gt 1 -or $sy -gt 1) { continue }
                        $cand = ([Math]::Pow($sx, 0.6667) + [Math]::Pow($sy, 0.6667) - 1.0) * 13
                        if ($cand -lt $d) { $d = $cand }
                    }
                    $d = [Math]::Max(0, $d)
                }
            }

            # cobertura suave: 1 no centro da linha, 0 a 1.4px dela
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

    # peak alpha for the line and the wash, before the in-game multiplier
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



<#
  SMALL WIDGETS: checkbox, radio and the slider knob.

  These are NOT 9-slice - the game stretches the whole texture into a 24px square
  (12px for the slider). So they are drawn at a fixed size with the mark centred,
  and have no corner region to respect.
#>
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
                # Tick: two diagonal strokes, the long one thicker
                $a = [Math]::Abs(($x - 10) - ($y - 18))          # ramo curto
                $b = [Math]::Abs(($x - 12) + ($y - 24))          # ramo longo
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

<#
  VERTICAL STRIPS: bar fill, scrollbar track and thumb.

  Drawn stretched on both axes (GUI.DrawTexture) or 9-sliced via GUIStyle.border.
  The gradient here is VERTICAL, and stretching preserves its shape, so it
  survives - unlike the centre of a 9-slice, where it would smear.
#>
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
# ---------------------------------------------------------------------------
#  THEMES
#
#  Each theme is a complete texture set under Skins/<Id>/. The mod reads those
#  PNG bytes straight off disk, so they are deliberately NOT in Textures/ -
#  that way RimWorld never loads dozens of textures it will not use.
#
#  A theme changes far more than colour: corner radius, fillet weight and the
#  contrast between plate and frame all move together. That is what separates
#  "the same UI recoloured" from "another UI".
# ---------------------------------------------------------------------------

$Themes = @{

    # Bound book, stripped back. The corner piece is the ONLY feature: no ribbed
    # edge, no recess, no fillet that thickens, and a fillet that does not ramp.
    # The palette is warm grey rather than oxblood and gilt, because saturation
    # was doing as much of the clutter as the geometry was.
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
    # Foundry plate: hot-rolled steel with the orange still in it. Heavy, wide
    # stepped shoulders and three rivets per corner. Mass is the whole read.
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
    # The original: dark wood and brass. Warm, round, opulent.
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

    # Cold, austere steel. Nearly square corner and a thin fillet on purpose: the
    # hard geometry is half the read, colour alone would not do it.
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

    # Deep indigo and gold. Wide corner and a heavy fillet: the exact opposite of
    # Iron on the same structure.
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

    # Volcanic glass. Corner cut at 45, an ice-pale hairline, no warmth at all.
    # The opposite of Brass on every axis.
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

    # Forest and aged bronze. Rivets at the corners over a scale backdrop - the read
    # is studded leather, not polished metal.
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

    # Real bone, not "a light frame". Two things carry the read:
    #
    # 1. COLOUR. Dry bone is neither white nor grey - it is yellowed ivory, and its
    #    shadow pulls towards ochre, never towards neutral grey. The previous
    #    version was light grey and so read as stone or plastic.
    # 2. PROFILE. The Bone ornament opens into two condyles per corner, which is
    #    the end of a long bone. An L-bracket is bolted-on hardware - the opposite
    #    of organic.
    #
    # Larger radius than before because bone has no sharp edge, and Fat well above
    # Thin because the difference between thin shaft and thick end IS the shape.
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

    # Oxblood and iron. Dark bracket over deep wine, with a scale pattern behind.
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

    # Deep violet with a luminous fillet and a double frame. The only theme that
    # imitates no material at all - arcane on purpose rather than forged.
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

    # Sawn plank. Square and with no hardware at all: what defines a plank is the
    # straight cut and the chamfered face, not an ornament stuck on.
    #
    # Hence Thin == Fat: equal, the fillet does NOT thicken at the corner and
    # becomes a constant bevel all the way round - exactly how a planed edge reads.
    # The variable fillet used by Brass would give forged metal.
    #
    # The grain cannot go in the button texture (the 9-slice centre is stretched and
    # would smear it into streaks), so it comes from the background PATTERN, which
    # tiles at its own size. That is why Woodgrain exists.
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

    # Flesh. Greyed pink with red veining and nodules at the corners. The most
    # uncomfortable of the set, on purpose.
    'Flesh' = @{
        Ornament = 'Studs'; Pattern = 'Veins'
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

    # Cathedral. The previous version was grey stone with a chamfered corner, so all
    # the gothic lived in the background pattern - and vanished the moment someone
    # changed it. Now the drawing is in the FRAME itself:
    #
    #   - a double moulding running the whole way round (dressed stone)
    #   - a TREFOIL in each corner, the gothic tracery motif
    #
    # The trefoil fits because it only occupies the 9-slice corner region, where
    # the drawing can be anything. A pointed arch still does not fit (the corner is
    # a Size/4 square and an ogive needs height), so that keeps coming from the
    # Tracery background pattern.
    #
    # Thin == Fat: stone moulding has constant thickness. A thickening fillet is
    # beaten metal, which is the wrong vocabulary.
    'Gothic' = @{
        Ornament = 'Gothic'; Pattern = 'Tracery'
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

    # Glass bubble. The only theme with a TRANSLUCENT interior: the map shows
    # through the window.
    #
    # Here ornament IS noise. The previous version had a double frame and a black
    # outline, and both fight what glass is - a surface with no hardware and no
    # hard edge. So:
    #
    #   Ornament = None    nothing but the rim
    #   Outline  = false   the pale rim IS the border; black outside it reads as a
    #                      plastic rim and kills the bubble
    #   Thin == Fat = 1    a very thin, constant rim
    #   radius = Size/4    the most the 9-slice allows: the corner becomes a full
    #                      quarter circle, the roundest bubble possible
    #
    # The rim stays opaque: translucent, it disappears and the piece loses its edge.
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

    # Copper with patina. Burnt orange against oxidised verdigris - the two halves
    # of the same plate ageing.
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

    # Volcanic ash. The quietest of the set: no colour at all and the thinnest
    # fillet, for anyone who wants the frame and nothing else.
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


<#
  Colour plate behind an architect category button. Greyscale + alpha, so one
  texture serves every category colour: the hue arrives as GUI.color at draw time.

  9-slice like everything else, which dictates each style. The centre slice is
  stretched on both axes, so only a VERTICAL gradient may live there (it scales
  rather than smearing). Bar and Frame keep the centre empty and put their marks
  in the fixed edge bands, which is why they survive any button size.
#>
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

            # Distance to the rounded outline: euclidean inside the corner box,
            # plain edge distance along the sides.
            if ($dx -lt $r -and $dy -lt $r) {
                $ox = $r - $dx; $oy = $r - $dy
                $dist = $r - [Math]::Sqrt($ox * $ox + $oy * $oy)
            } else {
                $dist = [Math]::Min($dx, $dy)
            }

            $a = 0.0
            switch ($Style) {
                'Plate' {
                    # Feather the rim so the plate never shows a hard edge against
                    # the frame, then fade upward: heavier at the base reads as depth.
                    # The ramp stays near opaque: the plate is already drawn at the
                    # player alpha, and fading inside the texture too would leave the
                    # strength slider unable to reach the top of its own range.
                    $edge = [Math]::Min(1.0, [Math]::Max(0.0, ($dist - 0.5) / 2.5))
                    $t = $y / [double]$max
                    $a = $edge * (0.80 + 0.20 * $t)
                }
                'Bar' {
                    # A flat patch. This style is no longer squeezed into the left
                    # band of the 9-slice, which is stuck at atlas.width/4 wide and
                    # could never be widened without stretching into the centre. It
                    # is drawn into a SQUARE rect of its own instead, so it needs no
                    # gradient to fake depth and no fade to hide a hard edge.
                    $a = [Math]::Min(1.0, [Math]::Max(0.0, ($dist - 0.5) / 2.5))
                }
                'Frame' {
                    # A ring hugging the outline: colour as a border, centre clear.
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

<#
  ICONS

  Signed distance fields, not stamped pixels. Every icon is a union of a few
  primitives; the distance to that union gives both the shape and, by widening
  the same threshold, the black outline around it - one field, two cutoffs, so
  the outline can never disagree with the shape or leave a gap.

  Drawn white on transparent: the category colour is applied as GUI.color at
  draw time, so these are generated ONCE and shared by every theme.

  Primitives, in a 64x64 space:
    @('seg',  x0,y0,x1,y1,w)     thick line with round caps
    @('disc', cx,cy,r)
    @('ring', cx,cy,r,w)
    @('box',  x0,y0,x1,y1,round)
    @('tri',  x0,y0,x1,y1,x2,y2) convex, via half-planes
    @('ering',cx,cy,a,b,w,deg)   elliptical ring, rotated (a planet ring)

  Prefix a type with '-' to SUBTRACT it, e.g. @('-disc', 32, 32, 5) punches a
  hole. Without this the field is a pure union, and any shape drawn inside
  another simply vanishes - which is why the pillow in the bed and the hole in
  the tag disappeared before.
#>

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
    # Convex half-plane intersection: enough for the triangles used here and far
    # cheaper than a general polygon field.
    #
    # The sign flip is not optional. A half-plane field is only an intersection
    # when every edge is wound the same way, so without normalising the winding
    # half the triangles come out inside-out - which looks like a shape that
    # simply failed to draw rather than like an error.
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
        'ering' {
            # The tilt is the whole point: a level ring around a disc reads as an
            # eye, not as a planet.
            $t = -$s[6] * [Math]::PI / 180.0
            $ct = [Math]::Cos($t); $st = [Math]::Sin($t)
            $ox = $px - $s[1]; $oy = $py - $s[2]
            $rx = $ox * $ct - $oy * $st
            $ry = $ox * $st + $oy * $ct
            # First-order distance: f / |grad f|. The naive (k-1)*min(a,b) form
            # under-reports distance along the major axis, so the ring came out
            # several times thicker at its tips - which is what clipped them
            # against the edge of the canvas.
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

<#
  Renders one icon. The outline is the SAME distance field at a wider cutoff,
  which is why it can never break up or drift the way a redrawn copy does.
#>
function New-Icon {
    param([string]$Name, [object[]]$Shapes, [int]$Size = 64, [double]$Outline = 2.2)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $max = $Size - 1
    $pad = $Outline + 2.0

    # Additive and subtractive shapes are separated once, here, rather than
    # tested per pixel.
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

    # Per-shape bounds, so a pixel only evaluates the shapes that can reach it.
    # Every term is parenthesised: see the comma-precedence note above.
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
            'ering' {
                # Conservative once rotated: the major axis can point either way.
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
                    # Subtraction is max(shape, -hole); a culled hole is -infinity,
                    # which is why skipping it above stays correct.
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

            # Black ring, white core, grey only where the two antialias into
            # each other. t is coverage relative to the outline it sits in.
            $t = $cov / $outl
            $v = [int][Math]::Round(255 * $t)
            $a = [int][Math]::Round(255 * $outl)
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($a, $v, $v, $v))
        }
    }

    $bmp.Save((Join-Path $OutDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "  $Name.png"
}

<#
  Two PowerShell traps are load-bearing here.

  1) The comma operator binds TIGHTER than + and *, so every arithmetic term
     inside an array literal needs its own parentheses. Without them
     @('seg', $a, $b + 1 * $c) parses as ('seg',$a,$b) + (1 * $c).
  2) "+=" on an array of arrays and a bare "return $array" each let PowerShell
     unroll one level, which silently yields an empty icon. Hence List[object]
     and the leading comma on the return.
#>
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
    # Four corner brackets: a dashed boundary, which is what a zone marker is.
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

<#
  The icon set. Every architect category resolves to one of these; mod
  categories reach them through keyword matching, so "Storage" gets the crate
  rather than a random colour and no picture.

  Few strokes and thick ones on purpose: these are drawn at roughly 24px.
#>
$Icons = @{
    # Vanilla categories
    'Orders'      = @(@('seg', 19, 52, 19, 11, 5), @('tri', 19, 11, 47, 19, 19, 29))
    'Zone'        = (Zone-Shapes)
    'Structure'   = @(@('box', 10, 19, 30, 31, 1.5), @('box', 34, 19, 54, 31, 1.5),
                      @('box', 10, 35, 21, 47, 1.5), @('box', 25, 35, 45, 47, 1.5), @('box', 49, 35, 54, 47, 1.5))
    'Production'  = @(@('box', 16, 13, 48, 27, 2.5), @('seg', 32, 27, 32, 53, 7))
    'Furniture'   = @(@('box', 9, 17, 17, 52, 2.5), @('box', 9, 31, 55, 43, 3), @('box', 48, 37, 55, 52, 2.5),
                      @('-box', 21, 33, 33, 41, 2))
    'Power'       = @(@('tri', 38, 9, 19, 35, 34, 35), @('tri', 30, 31, 45, 31, 25, 55))
    'Security'    = @(@('box', 15, 13, 49, 33, 3), @('tri', 15, 31, 49, 31, 32, 55))
    'Misc'        = @(@('disc', 17, 32, 5.5), @('disc', 32, 32, 5.5), @('disc', 47, 32, 5.5))
    'Floors'      = @(@('box', 11, 11, 30, 30, 1.5), @('box', 34, 11, 53, 30, 1.5),
                      @('box', 11, 34, 30, 53, 1.5), @('box', 34, 34, 53, 53, 1.5))
    'Joy'         = @(@('disc', 21, 45, 8.5), @('seg', 29, 45, 29, 13, 5), @('seg', 29, 13, 48, 9, 5))
    'Ship'        = @(@('seg', 32, 16, 32, 42, 15), @('tri', 23, 35, 23, 53, 12, 53), @('tri', 41, 35, 41, 53, 52, 53))
    'Temperature' = @(@('disc', 32, 45, 9.5), @('seg', 32, 14, 32, 43, 9))

    # DLC
    'Ideology'    = (Sun-Shapes)
    # A chromosome, not a helix: a double helix is four strokes crossing in the
    # middle and turns to mush at the 24px these are actually drawn at.
    'Biotech'     = @(@('seg', 38, 38, 48, 48, 10), @('seg', 26, 38, 16, 48, 10),
                      @('seg', 26, 26, 16, 16, 10), @('seg', 38, 26, 48, 16, 10))
    'Anomaly'     = @(@('tri', 32, 5, 24, 32, 40, 32), @('tri', 32, 59, 24, 32, 40, 32),
                      @('tri', 5, 32, 32, 24, 32, 40), @('tri', 59, 32, 32, 24, 32, 40))
    'Odyssey'     = @(@('disc', 32, 32, 11), @('ering', 32, 32, 24, 6, 3.5, -20))

    # Families for categories added by other mods
    'Storage'     = @(@('box', 9, 14, 55, 25, 2), @('box', 14, 30, 50, 53, 2.5),
                      @('-box', 27, 36, 37, 47, 1.5))
    'Medical'     = @(@('seg', 32, 14, 32, 50, 12), @('seg', 14, 32, 50, 32, 12))
    'Vehicle'     = @(@('ring', 32, 32, 18, 6), @('disc', 32, 32, 7),
                      @('seg', 21, 21, 43, 43, 4), @('seg', 43, 21, 21, 43, 4))
    'Industry'    = (Gear-Shapes)
    'Nature'      = @(@('tri', 32, 8, 14, 40, 32, 54), @('tri', 32, 8, 50, 40, 32, 54), @('seg', 32, 44, 32, 58, 4))
    'Arcane'      = @(@('tri', 32, 8, 12, 43, 52, 43), @('tri', 32, 56, 12, 21, 52, 21))
    'Water'       = @(@('tri', 32, 9, 21, 33, 43, 33), @('disc', 32, 38, 12.5))

    # A plan drawing: sheet outline plus one interior corner. The inner strokes
    # sit clear of the outline, or the union would swallow them.
    'Blueprint'   = @(@('seg', 11, 13, 53, 13, 4.5), @('seg', 11, 51, 53, 51, 4.5),
                      @('seg', 11, 13, 11, 51, 4.5), @('seg', 53, 13, 53, 51, 4.5),
                      @('seg', 22, 24, 43, 24, 4), @('seg', 22, 24, 22, 41, 4))
    'Sign'        = @(@('box', 21, 14, 54, 50, 3), @('tri', 21, 11, 21, 53, 5, 32),
                      @('-disc', 25, 32, 4.5))
}

$SkinsRoot = Join-Path $PSScriptRoot '..\Skins'

# The scale is written next to the skins and read back by AtlasSwap, so the two
# can never disagree. They must not: the draw divides the 9-slice corner by this
# number, and a mismatch changes every corner on screen without any error.
if (-not $DefineOnly -and -not $IconsOnly) {
    if (-not (Test-Path $SkinsRoot)) { New-Item -ItemType Directory -Path $SkinsRoot -Force | Out-Null }
    [IO.File]::WriteAllText((Join-Path $SkinsRoot 'atlas-scale.txt'), "$Scale", (New-Object Text.UTF8Encoding($false)))
}

foreach ($id in ($(if ($IconsOnly -or $DefineOnly) { @() } elseif ($Only.Count -gt 0) { @($Themes.Keys | Where-Object { $Only -contains $_ } | Sort-Object) } else { $Themes.Keys | Sort-Object }))) {
    $t = $Themes[$id]
    $S = $Scale

    # FillAlpha and Gloss only exist on the themes that need them.
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

    # Window at 128px: the 9-slice corner is width/4, so only here is there room
    # for a large radius without crushing the fillet.
    New-RoundAtlas -Name 'WindowAtlas' -Size (128*$S) -Radius ($t.Radius.Window*$S) `
        -Thin ($t.Fillet.WindowThin*$S) -Fat ($t.Fillet.WindowFat*$S) `
        -Light $t.Window.Light -Dark $t.Window.Dark -Fill $t.Window.Fill -Ornament $t.Ornament -Edge $ed -Recess $rc -Flat $fl -FillAlpha $fa -Gloss $gl -Outline $ol

    New-RoundAtlas -Name 'SectionAtlas' -Size (64*$S) -Radius ($t.Radius.Section*$S) `
        -Thin ($t.Fillet.Thin*$S) -Fat ($t.Fillet.Fat*$S) `
        -Light $t.Section.Light -Dark $t.Section.Dark -Fill $t.Section.Fill -Ornament $t.Ornament -Edge $ed -Recess $rc -Flat $fl -FillAlpha $fa -Gloss $gl -Outline $ol

    # These also go through Widgets.DrawAtlas, so they are 9-slice like the buttons.
    # The slider rail is flat and dark on purpose - the knob is what must stand out.
    New-RoundAtlas -Name 'SliderRail' -Size (64*$S) -Radius (8*$S) -Thin (2*$S) -Fat (2*$S) `
        -Light $t.Section.Light -Dark $t.Section.Dark -Fill $t.Section.Fill -Ornament 'Fillet' -Outline $ol

    New-RoundAtlas -Name 'TooltipBG' -Size (64*$S) -Radius ($t.Radius.Section*$S) `
        -Thin ($t.Fillet.Thin*$S) -Fat ($t.Fillet.Fat*$S) `
        -Light $t.Tab.Light -Dark $t.Tab.Dark -Fill $t.Window.Fill -Ornament $t.Ornament -Edge $ed -Recess $rc -Flat $fl -FillAlpha $fa -Gloss $gl -Outline $ol

    New-RoundAtlas -Name 'FloatMenuOptionBG' -Size (64*$S) -Radius (6*$S) -Thin (1*$S) -Fat (2*$S) `
        -Light $t.Subtle.Light -Dark $t.Subtle.Dark -Fill $t.Subtle.Fill -Ornament 'Fillet' -Outline $ol

    # Gizmo: the game STRETCHES the whole texture with no 9-slice, so keep the
    # radius small or the corner deforms on a 75px button.
    New-RoundAtlas -Name 'GizmoBG' -Size (64*$S) -Radius (6*$S) -Thin (2*$S) -Fat (3*$S) `
        -Light $t.Button.Light -Dark $t.Button.Dark -Fill $t.Button.Fill -Ornament $t.Ornament -Edge $ed -Recess $rc -Flat $fl -FillAlpha $fa -Gloss $gl -Outline $ol

    New-Checkbox -Name 'CheckOn'      -State 'On'      -Light $t.Button.Light -Dark $t.Button.Dark -Fill $t.Section.Fill -Mark $t.Tab.Light
    New-Checkbox -Name 'CheckOff'     -State 'Off'     -Light $t.Button.Light -Dark $t.Button.Dark -Fill $t.Section.Fill -Mark $t.Tab.Light
    New-Checkbox -Name 'CheckPartial' -State 'Partial' -Light $t.Button.Light -Dark $t.Button.Dark -Fill $t.Section.Fill -Mark $t.Tab.Light

    New-Radio -Name 'RadioButOn'  -On $true  -Light $t.Button.Light -Dark $t.Button.Dark -Fill $t.Section.Fill -Mark $t.Tab.Light
    New-Radio -Name 'RadioButOff' -On $false -Light $t.Button.Light -Dark $t.Button.Dark -Fill $t.Section.Fill -Mark $t.Tab.Light

    New-Knob -Name 'SliderHandle' -Light $t.Tab.Light -Dark $t.Button.Dark -Fill $t.Button.Fill

    # Architect category plates. Greyscale: the category colour arrives as
    # GUI.color, so one set of three serves every category.
    New-Plate -Name 'Plate'      -Size (64*$S) -Radius ($t.Radius.Button*$S) -Style 'Plate'
    New-Plate -Name 'PlateBar'   -Size (64*$S) -Radius ($t.Radius.Button*$S) -Style 'Bar'
    New-Plate -Name 'PlateFrame' -Size (64*$S) -Radius ($t.Radius.Button*$S) -Style 'Frame'

    # Progress bar: fill lit at the top, dark track behind it.
    New-Strip -Name 'BarFill' -W 16 -H 32 `
        -Top $t.Tab.Light -Bottom $t.Button.Dark -Edge $t.Button.Dark

    New-Strip -Name 'BarBG' -W 16 -H 32 `
        -Top $t.Section.Fill -Bottom $t.Window.Fill -Edge $t.Section.Dark

    # Scrollbar: the track nearly disappears, the thumb is what has to be grabbed.
    # No outline on the track, or it becomes a box inside a box.
    New-Strip -Name 'ScrollTrack' -W 16 -H 32 `
        -Top $t.Window.Fill -Bottom $t.Section.Fill -Edge $t.Section.Dark -Outline $false

    New-Strip -Name 'ScrollThumb' -W 16 -H 32 `
        -Top $t.Button.Light -Bottom $t.Button.Dark -Edge $t.Button.Dark

    # Pattern inks come from the theme itself: the line mixes the panel highlight
    # with the tab highlight, and the wash uses the window highlight. That is what
    # makes the background BELONG to the skin instead of being stamped over it.
    foreach ($k in @('Hatch', 'Medieval', 'Scales', 'Bricks', 'Dots', 'Chevron', 'Woodgrain', 'Veins', 'Tracery')) {
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

Write-Host ""
Write-Host "OK - $($Themes.Count) themes, $($Icons.Count) icons (scale ${Scale}x)" -ForegroundColor Green

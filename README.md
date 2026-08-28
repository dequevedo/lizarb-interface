# Lizarb Interface

A complete reskin of RimWorld 1.6's interface: window frames, buttons, tabs, tooltips,
widgets, scrollbars, fonts and the Architect menu, in 14 themes.

![Themes](docs/themes.png)

![Architect menu](docs/architect.png)

## What it does

- **14 themes.** Each one changes palette, corner radius, fillet weight, corner ornament
  and background pattern. Iron is square and austere, Royal is wide and heavy, Aero is a
  translucent bubble with no outline at all.
- **9 tileable background patterns**, baked per theme so they carry the theme's own inks.
- **Fonts.** Fifteen faces ship inside an AssetBundle, so they are there with nothing
  to install; any font already on the machine can be picked as well. Three text sizes
  are adjustable, and every label can carry a real outline at a chosen opacity.
- **Architect colours.** Every category button is coloured by what the category is for.
  Colours come from twelve families rather than the whole hue circle, and categories doing
  related jobs share one on purpose - that similarity is what keeps thirty buttons from
  reading as noise. Categories added by other mods are matched on their name, so Storage
  lands with storage and Genetics with the medical group, with no patch on their side.
  The colour is drawn through a 9-slice, so it follows the theme corner radius: fill the
  button, a stripe down one edge, a border, or a flat rectangle.
- **Architect icons.** An optional set of 23 icons of this mod own making, white with a
  black outline. They are distance fields rather than drawn pixels, so the outline is the
  same field at a wider cutoff and cannot gap or drift.
- **Spacing.** Paints each element slightly inside its own rect, so neighbouring buttons
  stop touching. No layout number is changed, so nothing moves.

## Install

Requires [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077).

Clone into RimWorld's `Mods` folder. The repository root **is** the mod folder:

```
git clone https://github.com/<you>/lizarb-interface "…/RimWorld/Mods/LizarbInterface"
```

Load it last if you want its look to win over other UI mods.

## Settings

Six tabs: **Theme**, **Text**, **Surfaces**, **Windows**, **Architect**, **Components**.

The Components tab is the compatibility valve. Buttons, window frames, tabs, widgets and
scrollbars can each be handed back to the game independently, so one clash with another UI
mod does not cost you the whole skin.

## Compatibility

The mod logs every other mod that patches a method it also patches:

```
[LizarbInterface] patch audit: 1 method(s) also patched by other mods.
  TabRecord.Draw  <-  some.other.mod (prefix)
```

A transpiler alongside our prefix composes fine. Another **prefix** on `TabRecord.Draw`,
`DrawWindowBackground` or `DrawMenuSection` is the one to watch, since those are replaced
outright. This mod deliberately carries no Harmony priority on them, so it yields,
losing its own styling rather than breaking someone else's feature.

## Building

```
dotnet build -c Release Source/LizarbInterface/LizarbInterface.csproj
```

References come from NuGet (`Krafs.Rimworld.Ref`, `Lib.Harmony`); no game DLL is copied
into the repository. Output goes straight to `Assemblies/`.

`tools/Make-Atlases.ps1` regenerates every texture in `Skins/` from the theme table at the
bottom of that script. Adding a theme is one entry there plus one tuple in
`LizarbInterfaceMod.Themes` and two language keys.

## Licence

Code and textures are GPL-3.0 (see `LICENSE`).

The fonts are **not**. Each is under the SIL Open Font License, with its own licence
file beside it in `Fonts/`. They are redistributed under those terms, both as the `.ttf`
sources in `Fonts/` and inside `AssetBundles/lizarbinterface_fonts_win`.

`Fonts/` is the input to `tools/Make-FontBundle.ps1`; nothing reads it at runtime.

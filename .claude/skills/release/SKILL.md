---
name: release
description: Publish a Lizarb Interface update to the Steam Workshop. Use when the user asks to release, ship, publish or upload a new version of the mod.
---

# Release

Steps marked **Daniel** are his. Do the rest, then stop and tell him.

## 1. Check (Claude)

- `git status` clean, nothing untracked that should ship
- if art changed: `tools/Make-Icons.ps1`, `tools/Make-Atlases.ps1`, `tools/Make-Docs.ps1`
- build: `dotnet build -c Release Source/LizarbInterface/LizarbInterface.csproj`

## 2. What went stale (Claude tells Daniel)

Check each and name the ones that need him. Never guess that one is fine.

| changed | goes stale | who |
|---|---|---|
| themes, fonts, plate shapes, Architect | `docs/*.png` | Claude, run Make-Docs |
| the thumbnail | `About/Preview.png`, ships in the package | Daniel repaints, Claude never |
| settings screen, tabs | `docs/settings/*.png`, `tour.gif` | Daniel rescreenshots |
| themes added or removed | gallery images, the theme count in the text | Daniel |
| features, wording, credits | `docs/steam-description.bbcode` | Claude edits, Daniel pastes |
| any `docs` image the description links | push before he pastes | Claude |

`About/Preview.png` is the Workshop thumbnail and comes from the package, not
from the item page. `Make-Docs` will not overwrite a hand painted one, but say
out loud which of the two is about to ship.

## 3. Change note (Claude)

- rewrite `About/ChangeNote.txt` from the commits since the last release
- what the player gets, not how it works
- no em dash, no spaced hyphen
- it ships inside the package and becomes the Steam change note by itself

## 4. Commit and push (Claude)

- commit everything, `git push` with no arguments
- never write the destination by hand: local is `master`, remote is `main`

## 5. Package (Claude)

```
powershell -File tools/Make-Release.ps1 -Link
```

Bumps the minor version, stages `dist/`, points the junction at it.

## 6. Verify the package (Claude)

- `About/PublishedFileId.txt` present and equal to `3791745392`
- `ChangeNote.txt` present
- no `0Harmony.dll`, no `.aseprite`, no `tools/`, `dev/`, `docs/`
- `Fonts/` licence count equals `.ttf` count
- the default theme folder has its files
- commit the version bump and push

## 7. Upload (Daniel)

- close RimWorld, the junction is only read at start
- open it, Mods, select Lizarb Interface, Upload
- nothing to paste: the change note travels in the package

## 8. After (Daniel asks, Claude runs)

```
powershell -File tools/Make-Release.ps1 -Dev
```

Points the junction back at the working tree. Until then, editing the repo
changes nothing in the game: a new theme folder, a rebuilt DLL and a redrawn
texture all go unnoticed. If something added to the repo does not show up in
game, check where the junction points before anything else.

## Manual, only Daniel can do

- gallery images: the item page, Add/edit images & videos
- description: `docs/steam-description.bbcode`, paste into Edit title & description
- images it links come from `raw.githubusercontent`, so push before pasting

# Restore Rush (working title)

Mobile hybrid-casual puzzle game built in Unity.

## Current objective
Ship a playable MVP fast, validate retention and ad monetization, then scale with UGC-first growth.

## Tech baseline
- Unity 6.3 LTS (6000.3.x)
- Primary dev machine: Windows
- iOS build/signing machine: Mac mini
- Growth automation and analytics helpers: Linux hub

## Repository layout
- `Assets/` game content and scripts
- `Packages/` Unity package manifest and lock file
- `ProjectSettings/` Unity project settings
- `docs/` roadmap, checklist, and growth plan

## Unity project settings (required)
Set these in Unity Editor:
1. `Edit > Project Settings > Editor > Version Control` = `Visible Meta Files`
2. `Edit > Project Settings > Editor > Asset Serialization` = `Force Text`

## 4-week MVP focus
See `docs/ROADMAP.md` and `docs/MVP_CHECKLIST.md`.

## Next immediate execution
1. Create the Unity project in this folder from Unity Hub.
2. Commit generated `Packages/manifest.json` and `ProjectSettings/*`.
3. Implement the Week 1 gameplay vertical slice.

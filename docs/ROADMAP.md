# MVP Roadmap

Date range: 2026-02-19 to 2026-03-18

## Product choice
Primary: Restore Rush
Fallback prototype: Dice Builder Lucky Blocks

## Week 1 (2026-02-19 to 2026-02-25)
Goal: Core gameplay is playable end-to-end in one level.
- Setup Unity project and scene flow (Boot -> Menu -> Gameplay -> Results)
- Implement dirty layer cleaning interaction
- Add 3 tools (brush, spray, scraper)
- Add win and fail states
- Add tutorial v0 and base HUD

## Week 2 (2026-02-26 to 2026-03-04)
Goal: MVP content and economy foundation.
- Build 25-40 short levels with 2 rule variations
- Add tool upgrade economy (speed, impact)
- Add simple workshop meta unlock
- Integrate ads skeleton (rewarded, low-frequency interstitial)
- Add remove-ads IAP flow stub

## Week 3 (2026-03-05 to 2026-03-11)
Goal: Stability, analytics, and Android test package.
- Implement save system with recovery and checksum
- Add analytics event schema and debug logger
- Add remote parameter stubs
- Run Android internal testing build
- Fix crashes, soft locks, and ad lifecycle issues

## Week 4 (2026-03-12 to 2026-03-18)
Goal: Soft launch ready package + first growth assets.
- Prepare store assets and listing copy
- Record and edit first UGC content batch
- Validate core KPI gates before soft launch
- Ship soft launch build

## KPI gates for go/no-go
- Tutorial completion >= 85%
- First 3 levels completion >= 60%
- D1 retention >= 28%
- Stable 60 FPS on target low-mid Android device

## Pivot rule
If KPI gates are missed after two weekly iterations, start fallback prototype (Dice Builder) in parallel.

# Data Model

Documents the EF Core entity relationships for the Cutline Fantasy platform.

## Entity Overview

- **League** — root aggregate; owns `Teams`, `Weeks`, `ScoringSettings`, `RosterSettings`
- **Team** — belongs to a `League`; has `RosterSlots`; carries elimination state
- **Player** — global (not league-scoped); canonical key is `GsisId`; synced from nflverse + Sleeper
- **Week** — belongs to a `League`; tracks `WeekStatus` and the `EliminatedTeam` for that week
- **TeamScore** — per-team score for a given `Week`; `IsLocked = true` once reconciled against nflverse final stats
- **RosterSlot** — links a `Team` to a `Player` with a `SlotType` and starter flag
- **WaiverClaim** — filed against a `Week`; supports priority and FAAB bid modes

## ID Strategy

All entities use `Guid` primary keys. `Player` additionally carries platform-specific IDs:
- `GsisId` — canonical identifier (sourced from nflverse `load_rosters()`)
- `SleeperId` — used for metadata and injury sync
- `EspnId` — used for live scoring via the ESPN unofficial API

## Relationships

```
League ──< Team ──< RosterSlot >── Player
       ──< Week ──< TeamScore  >── Team
                ──< WaiverClaim >── Team
                                 >── Player (add)
                                 >── Player (drop, nullable)
```

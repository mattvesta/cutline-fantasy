# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Cutline Fantasy is an open source fantasy football platform built specifically for the guillotine format — every week the lowest-scoring team is eliminated and their roster hits waivers. The codebase has two deployment targets: a self-hostable open source core and a hosted platform (cutlinefantasy.app) that adds AI-powered features via the Anthropic Claude API.

## Commands

### Backend (.NET 10)

```bash
# Build
dotnet build

# Run API
dotnet run --project src/Cutline.Api

# Run ingest worker
dotnet run --project src/Cutline.Ingest

# Run all tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Apply EF Core migrations
dotnet ef database update --project src/Cutline.Infrastructure

# Add a new migration
dotnet ef migrations add <MigrationName> --project src/Cutline.Infrastructure --startup-project src/Cutline.Api
```

### Frontend (Vue 3 / Vite / TypeScript)

```bash
cd client

# Install dependencies
npm install

# Dev server
npm run dev

# Build for production
npm run build

# Type check
npm run type-check

# Lint
npm run lint
```

### Docker (full stack)

```bash
# Development
docker compose -f docker-compose.dev.yml up

# Production
docker compose up -d
```

## Architecture

### Backend — 5 projects, 2 deployables

The backend splits into two independently deployable processes that share `Cutline.Core` and `Cutline.Infrastructure` but have no reference to each other.

- **`Cutline.Core`** — Domain layer. All entities (League, Team, Player, Week, WaiverClaim, etc.), the guillotine elimination engine logic, and interfaces (repository contracts, service contracts). No infrastructure dependencies.
- **`Cutline.Infrastructure`** — EF Core DbContext, repository implementations, Redis integration. Shared by both the API and the ingest worker. Contains no sports data clients.
- **`Cutline.Api`** — ASP.NET Core 10 Minimal APIs and SignalR hubs. **Read-only from a data perspective** — it only reads what the ingest worker has written to the DB and Redis. Has no reference to `Cutline.Ingest` and no knowledge of any external sports data source.
- **`Cutline.Ingest`** — .NET Worker Service. The only process that talks to external sports data sources. Runs four `BackgroundService` workers: `SleeperSyncWorker` (daily), `NflverseRosterSyncWorker` (weekly), `NflverseFinalStatsWorker` (post-game), `EspnLiveScoringWorker` (game day 60s poll). Writes everything to the shared DB and Redis.
- **`Cutline.AI`** — Anthropic Claude API integration. **Hosted platform only** — weekly reports, trade analysis, waiver assistant, draft assistant. Do not wire this into self-hostable flows.
- **`Cutline.Tests`** — Unit and integration tests across all layers.

### Frontend — Vue 3 + TypeScript + Tailwind CSS v4

Located in `client/`. All frontend code is TypeScript (`.ts` / `.vue` with `<script setup lang="ts">`). Key subdirectories:
- `stores/` — Pinia stores for app state
- `composables/` — Reusable composition functions (`useSignalR`, `useLeague`, etc.)
- `api/` — Typed API client (wraps backend endpoints)
- `components/` / `views/` — Standard Vue component hierarchy

**Tailwind CSS v4** — uses `@import 'tailwindcss'` syntax (not `@tailwind` directives). Design tokens are CSS custom properties defined in `client/src/style.css`, not in a Tailwind config file. Always use `var(--token-name)` for colors/surfaces rather than hardcoded hex values. Key tokens:
- `--bg: #0c0c12`, `--surface: #131319`, `--surface-raised` — backgrounds
- `--accent: #e31e24` (brand red), `--accent-warm: #f97316` (orange), `--accent-dim` — brand colors
- `--text: #f0ede6` (warm cream), `--text-secondary`, `--text-muted` — text hierarchy
- `--border`, `--border-subtle` — borders

Shared utility classes in `style.css`: `.page`, `.card`, `.card-raised`, `.btn`, `.btn-primary`, `.btn-ghost`, `.input`, `.label`, `.data-table`, `.divider`.

**Brand assets** — `client/public/logo.png` and `client/public/hero-logo.png` are both the square Cutline Fantasy logo. Avoid running ImageMagick background-removal on them without explicit user request — the white background has previously caused color distortion.

**vuedraggable** (`vuedraggable@next` / v4) is installed but `TeamView.vue` currently uses native HTML5 drag-and-drop for lineup management, not vuedraggable, to allow fine-grained control over what is draggable (player card only, not the slot label).

### Key domain concepts

The **guillotine engine** (lives in `Cutline.Core`) is the heart of the platform. Each week it: identifies the lowest-scoring team, triggers elimination, releases that team's roster to waivers, and processes waiver claims for remaining teams. The elimination process is immutable and produces a full audit trail — never mutate past elimination records.

**Roster slots** (`RosterSlot`) have a fixed `SlotType` (QB, RB, WR, TE, Flex, SuperFlex, K, DEF, Bench, IR) and a nullable `PlayerId` — slots can be empty. Lineup changes swap `PlayerId` values between two slots; the slot types themselves never change. Position eligibility rules (enforced in both frontend and should be enforced in backend):
- QB → QB, SuperFlex, Bench
- RB → RB, Flex, SuperFlex, Bench
- WR → WR, Flex, SuperFlex, Bench
- TE → TE, Flex, SuperFlex, Bench
- K → K, Bench
- DEF → DEF, Bench

**Lineup swap endpoint**: `POST /api/leagues/{leagueId}/teams/{teamId}/lineup/swap` with body `{ slotAId, slotBId }`. Swaps `PlayerId` between the two slots and returns the full updated team. Because `PlayerId` is nullable, dragging a player to an empty slot moves them there (source becomes null); dragging between two occupied slots swaps them.

### Multi-tenancy

Each league is an isolated tenant. Tenant context (league ID) must be established at the API boundary and propagated through all layers — never leak data across league boundaries. All repository queries and Redis keys must be scoped to the tenant. Use a consistent Redis key prefix convention: `league:{leagueId}:*`.

### Sports data pipeline

All external data fetching lives exclusively in `Cutline.Ingest` — the API has no knowledge of any sports data source. The ingest worker fetches in **bulk** (never per-league) and writes to the shared DB and Redis. `Cutline.Core` scoring logic then fans out per-tenant, applying each league's roster and scoring rules (PPR, half PPR, custom bonuses, etc.) to produce per-team scores.

| Source | Purpose | Sync cadence |
|---|---|---|
| Sleeper API | Player metadata, injury status, bye weeks, depth charts | Daily |
| nflverse `load_rosters()` | Cross-platform player ID mapping | Weekly |
| nflverse `load_player_stats()` | Final weekly stats — authoritative source for scoring | Post-game |
| ESPN unofficial API | Live in-game scoring on game day only | Game day polling (default 60s) |

**Canonical player ID is `gsis_id`** — the NFL's own identifier, used throughout nflverse. All other platform IDs (`sleeper_id`, `espn_id`, etc.) are stored alongside it and resolved at sync time via nflverse's ID map. Never use name-matching to correlate players across sources.

Live scoring flow: ESPN polling → map `espn_id` to roster → write scored data to Redis → push to clients via SignalR. Once a game completes, final scores are reconciled against nflverse `load_player_stats()` as the source of truth.

### Redis

Redis serves two purposes:
- **Cache** — bulk player data, raw stats, standings
- **Real-time stats** — live scoring data is written to Redis after per-league scoring is applied and pushed to clients via SignalR; do not poll the database for in-progress game scores

### Self-host vs hosted distinction

Features in `Cutline.AI` and anything behind the `hosted-only` label in issues are exclusive to the hosted platform. All default data sources (Sleeper, nflverse, ESPN unofficial) are free and require no API keys. The hosted platform uses a managed feed. Keep these paths cleanly separated.

### API endpoints

Key endpoint files in `src/Cutline.Api/Endpoints/`:

- **`LeagueEndpoints.cs`** — CRUD for leagues. `POST /api/leagues` accepts `CreateLeagueRequest` with full roster slots, scoring preset, `UseFaab`, and `FaabBudget` set at creation time.
- **`ScoringEndpoints.cs`** — Live scoring:
  - `GET /api/leagues/{leagueId}/scoring/live` — all team point totals for the current week
  - `GET /api/leagues/{leagueId}/scoring/teams/{teamId}` — full per-player scorecard; returns roster with `stats: null` gracefully when no active week
  - `GET /api/leagues/{leagueId}/scoring/matchup` — all teams sorted by live points, last active team flagged `isOnCutLine`, includes eliminated teams
  - **Important:** these endpoints query `db.Weeks` directly (not `league.Weeks`) because `LeagueRepository.GetByIdAsync` never eager-loads `Weeks`.
- **`WaiverEndpoints.cs`** — Waiver wire:
  - `GET /api/leagues/{leagueId}/waivers?teamId=&position=&search=&page=` — available free agents (paginated/filtered), pending claims, recent results, team roster, FAAB balance
  - `POST /api/leagues/{leagueId}/waivers/claims` — submit a claim; auto-detects the open week
  - `DELETE /api/leagues/{leagueId}/waivers/claims/{claimId}?teamId=` — cancel a pending claim
  - Optional query param `int page = 1` must come **after** `CancellationToken ct` in the lambda to avoid CS1737.

### SignalR

`ScoringHub` at `/hubs/scoring`. Client methods: `JoinLeague(leagueId)` / `LeaveLeague`, `JoinTeam(teamId)` / `LeaveTeam`.

Server events: `TeamScoreUpdate { teamId, totalPoints }`, `PlayerStatUpdate { teamId, playerId, stats, points }`.

**Always broadcast `TeamScoreUpdate` to both `team:{id}` AND `league:{leagueId}` groups** — managers viewing other teams' live pages subscribe to the league group but not every team group, so they need the broadcast on the league channel to update their leaderboard.

### Frontend views

Key views added in `client/src/views/`:
- **`LiveTeamView.vue`** — `/leagues/:leagueId/teams/:teamId/live` — live per-player scorecard with league leaderboard sidebar. Joins both `team:{teamId}` and `league:{leagueId}` SignalR groups.
- **`WeeklyMatchupView.vue`** — `/leagues/:leagueId/matchup` — all teams' live scores with dashed cut-line divider and red callout for the team on the cut line. Joins `league:{leagueId}`.
- **`WaiverWireView.vue`** — `/leagues/:leagueId/teams/:teamId/waivers` — browse free agents, submit FAAB blind-auction bids (large `$X` input + slider + quick-bid presets) or priority claims, view claim results.

### Dev seed

`POST /api/dev/seed` creates a test league with 8 teams and seeds players onto their rosters from the DB. **Requires Sleeper sync to have run first** — it queries top-ADP players by position and returns HTTP 400 if fewer than 8 QBs or 16 RBs/WRs are found. Each team gets 8 starters (QB, RB×2, WR×2, TE, K, DEF) + 6 bench spots distributed round-robin by ADP rank. `DELETE /api/dev/seed` wipes all league data — **must delete `DraftPicks` first** before removing leagues because `DraftPick → Team` FK is `OnDelete(Restrict)`.

`POST /api/dev/seed-scores` — creates a Week 8 InProgress, populates mock `PlayerGameStats`, and broadcasts `TeamScoreUpdate` to both `team:{id}` and `league:{leagueId}` groups.

`POST /api/dev/simulate-score-tick` — increments a random player's stats and re-broadcasts.

### EF Core migrations

`dotnet ef database update --project src/Cutline.Infrastructure --startup-project src/Cutline.Api`

Current migrations (in order): `InitialCreate` → `AddPlayerMetadata` → `AddPlayerAdp` → `AddFaabSettings` → `AllowEmptyRosterSlots` → `AddPlayerGameStats`.

## License

AGPL-3.0. Any modified version run as a network service must open-source those modifications under the same license.

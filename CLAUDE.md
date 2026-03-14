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

### Frontend — Vue 3 + TypeScript + Tailwind CSS

Located in `client/`. All frontend code is TypeScript (`.ts` / `.vue` with `<script setup lang="ts">`). Key subdirectories:
- `stores/` — Pinia stores for app state
- `composables/` — Reusable composition functions (`useSignalR`, `useLeague`, etc.)
- `api/` — Typed API client (wraps backend endpoints)
- `components/` / `views/` — Standard Vue component hierarchy

Styling uses Tailwind CSS utility classes. Avoid writing custom CSS unless Tailwind cannot express the style.

### Key domain concept

The **guillotine engine** (lives in `Cutline.Core`) is the heart of the platform. Each week it: identifies the lowest-scoring team, triggers elimination, releases that team's roster to waivers, and processes waiver claims for remaining teams. The elimination process is immutable and produces a full audit trail — never mutate past elimination records.

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

## License

AGPL-3.0. Any modified version run as a network service must open-source those modifications under the same license.

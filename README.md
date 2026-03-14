# Cutline Fantasy

> Will you make the cut?

**Cutline** is an open source fantasy football platform built specifically for the guillotine format — the most addicting format in fantasy sports. Every week, the lowest scoring team is eliminated and their roster hits waivers. Survive long enough and you win everything.

Built with Vue 3 + ASP.NET Core. Self-hostable. AI-powered analysis on the hosted platform.

---

## What is guillotine fantasy?

Standard fantasy football has a winner and a loser each week. Guillotine fantasy has a **survivor**. Every week, the lowest scoring team is cut from the league — and their entire roster is dropped to waivers for the remaining teams to pick over. 18 teams enter. 1 team wins. The blade falls every Sunday.

It changes everything about how you play. Floor beats ceiling. Waiver strategy becomes survival strategy. Every week is sudden death.

---

## Features

### Core (open source, self-hostable)
- Full guillotine league management — create, invite, configure, run
- Flexible roster slot configuration — 2QB, SuperFlex, TE premium, any format
- Configurable scoring rules — PPR, half PPR, standard, custom bonuses
- Snake and auction draft with live draft room
- Real-time scoring and standings via WebSockets
- Weekly elimination engine with full audit trail
- Waiver claim processing — priority-based or FAAB
- Trade management
- PWA — installable on iOS and Android, no app store required
- Full season history and elimination timeline

### Hosted platform (cutlinefantasy.app)
- **AI weekly report** — personalized elimination risk analysis for every team, every week
- **AI trade analyzer** — standing-aware trade grades that understand you're playing to survive
- **AI waiver assistant** — survival-first pickup recommendations based on your position in the standings
- **Survival odds engine** — weekly elimination probability with key risk factors
- **AI draft assistant** — floor-focused guidance tuned for the guillotine format
- **Cross-league survival stats** — see how your team stacks up across all Cutline leagues
- **Multi-season trophy case** — champion history, elimination timelines, league records
- **Push notifications** — lineup reminders, waiver results, elimination alerts
- Managed sports data — we handle the feed so you don't have to

---

## Getting started

### Hosted (recommended)

The fastest way to run a Cutline league is on our hosted platform at [cutlinefantasy.app](https://cutlinefantasy.app). Free to start. No setup required.

### Self-hosting

Run your own Cutline instance in minutes with Docker.

**Prerequisites**
- Docker and Docker Compose
- No paid API keys required — Cutline uses free data sources by default (see [Sports data](#sports-data) below)

**One-command setup**

```bash
git clone https://github.com/your-username/cutline-fantasy.git
cd cutline-fantasy
cp .env.example .env
# Edit .env with your configuration
docker compose up -d
```

Cutline will be running at `http://localhost:3000`.

For full self-hosting documentation including SSL, reverse proxy setup, and sports data configuration, see [docs/self-hosting.md](docs/self-hosting.md).

---

## Tech stack

| Layer | Technology |
|---|---|
| Front end | Vue 3, Vite, Pinia, Tailwind CSS |
| Mobile | PWA via vite-plugin-pwa |
| API | ASP.NET Core 10 Minimal APIs |
| Real-time | SignalR |
| Database | PostgreSQL + EF Core 10 |
| Cache | Redis |
| AI (hosted) | Anthropic Claude API |
| Player data + IDs | Sleeper API — metadata, injury status, bye weeks |
| ID mapping | nflverse `load_rosters()` — cross-platform player ID bridge |
| Game stats | nflverse `load_player_stats()` — final stats after games complete |
| Live scoring | ESPN unofficial API — in-game scoring on game day only |

---

## Sports data

Cutline uses a layered approach to sports data, combining multiple free sources rather than depending on a single paid provider. Each source is used for what it does best.

| Source | Purpose | Frequency |
|---|---|---|
| [Sleeper API](https://docs.sleeper.com) | Player metadata, injury status, bye weeks, depth charts | Daily sync |
| [nflverse](https://nflverse.com) `load_rosters()` | Cross-platform player ID mapping — bridges Sleeper IDs, ESPN IDs, GSIS IDs, and more | Weekly sync |
| [nflverse](https://nflverse.com) `load_player_stats()` | Final weekly stats after games complete — the source of truth for scoring | Post-game sync |
| ESPN unofficial API | Live in-game scoring on game day — the only piece that needs to be real-time | Game day polling |

### Why this approach

No single free API provides everything a fantasy platform needs. Sleeper has great player metadata but no live stats. The ESPN unofficial API has live scoring but no documented player ID system. nflverse is the glue — its `load_rosters()` endpoint provides a unified ID mapping table across nine different platforms, making it straightforward to connect Sleeper player IDs to ESPN scoring data without fragile name-matching heuristics.

The canonical player identifier in Cutline's data model is the `gsis_id` — the NFL's own Game Statistics and Information System ID, which is the core identifier used throughout nflverse. All other platform IDs (`sleeper_id`, `espn_id`, etc.) are stored alongside it and resolved at sync time.

### Live scoring

Live scoring on game day is handled by polling the ESPN unofficial API at a configurable interval (default 60 seconds during active games). Player scores are mapped back to Cutline's roster data via the `espn_id` field populated from nflverse's ID map. Once a game is complete, final stats are reconciled against nflverse's `load_player_stats()` as the authoritative source.

This approach means a temporary ESPN API outage affects live score updates only — all final scoring, historical data, and player metadata remain unaffected.

### Self-hosting note

All data sources used by default are free and require no API keys. The ESPN unofficial API is undocumented and carries no SLA — it works reliably for the fantasy community but could change at any time. If you need a production-grade SLA for live scoring, see [docs/self-hosting.md](docs/self-hosting.md) for instructions on configuring a paid provider such as [MySportsFeeds](https://mysportsfeeds.com) or [SportsDataIO](https://sportsdata.io) as a drop-in replacement.

---

## AI

Cutline uses AI in two distinct ways — as a development tool and as a product feature.

**In the product:** The hosted platform at cutlinefantasy.app offers AI-powered analysis features — weekly team reports, trade grades, waiver recommendations, and survival odds — all powered by the [Anthropic Claude API](https://anthropic.com). These features are part of the hosted tier and are not required to run a self-hosted instance. See [CLAUDE.md](CLAUDE.md) for details.

**In development:** This project was built with AI assistance. Claude was used throughout the design and development process — architecture decisions, code generation, documentation, and more. All AI-generated code and content has been reviewed, tested, and is owned by the project maintainers. Contributors are welcome to use AI tools in their workflow; the same standard applies — you are responsible for what you submit.

---

## Project structure

```
cutline-fantasy/
├── src/
│   ├── Cutline.Api/          # ASP.NET Core — endpoints, SignalR hubs (read-only)
│   ├── Cutline.Core/         # Domain entities, guillotine engine, interfaces
│   ├── Cutline.Infrastructure/ # EF Core, repositories, Redis
│   ├── Cutline.Ingest/       # Worker service — all sports data ingestion
│   ├── Cutline.AI/           # Anthropic integration (hosted platform only)
│   └── Cutline.Tests/        # Unit + integration tests
├── client/                   # Vue 3 front end
│   └── src/
│       ├── components/
│       ├── views/
│       ├── stores/           # Pinia
│       ├── composables/      # useSignalR, useLeague, etc.
│       └── api/              # Typed API client
├── infra/                    # Docker, nginx, deployment config
├── docs/                     # Self-hosting guide, API docs, data model
├── docker-compose.yml        # Production self-host
└── docker-compose.dev.yml    # Local development
```

---

## Contributing

Cutline is open source and welcomes contributions. The guillotine format deserves a great platform and we can't build it alone.

**Good first issues** are tagged [`good first issue`](../../issues?q=is%3Aopen+label%3A%22good+first+issue%22) in the issue tracker — these are self-contained, well-scoped tasks with clear acceptance criteria.

**Before contributing:**
1. Check the [open issues](../../issues) and [discussions](../../discussions) to avoid duplicating effort
2. For significant changes, open a discussion first so we can align on approach
3. Read [CONTRIBUTING.md](CONTRIBUTING.md) for code style, branch conventions, and PR guidelines

**Issues labeled [`hosted-only`](../../issues?q=is%3Aopen+label%3Ahosted-only)** involve the AI features or hosted platform infrastructure. These are still open for discussion but require coordination before implementation.

---

## Roadmap

- [x] Data model + EF Core entities
- [ ] Guillotine engine — elimination + waiver processing
- [ ] Draft room (snake)
- [ ] Live scoring via SignalR
- [ ] Lineup management
- [ ] PWA setup + mobile install
- [ ] AI weekly report (hosted)
- [ ] AI trade analyzer (hosted)
- [ ] Auction draft
- [ ] Multi-season trophy case
- [ ] Native push notifications

Follow along or contribute in [Discussions](../../discussions).

---

## License

Cutline Fantasy is licensed under the [GNU Affero General Public License v3.0](LICENSE).

This means you are free to self-host, modify, and distribute Cutline — but if you run a modified version as a network service, you must make your modifications available under the same license. You cannot build a competing hosted service on top of this code without open sourcing your changes.

---

## Acknowledgements

Built for guillotine league players, by guillotine league players.

The guillotine format was pioneered by [Paul Charchian](https://twitter.com/PaulCharchian). We're standing on the shoulders of a great format.

---

<p align="center">
  <strong>cutlinefantasy.app</strong> &nbsp;·&nbsp; Will you make the cut?
</p>

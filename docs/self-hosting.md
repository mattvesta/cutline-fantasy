# Self-Hosting Cutline Fantasy

## Prerequisites

- Docker and Docker Compose
- A PostgreSQL 17 instance (or use the provided compose file)
- A Redis 7 instance (or use the provided compose file)

## Quick Start

1. Copy `.env.example` to `.env` and set `POSTGRES_PASSWORD`.
2. Start the dev infrastructure (Postgres + Redis only):
   ```bash
   docker compose -f docker-compose.dev.yml up -d
   ```
3. Run the API:
   ```bash
   dotnet run --project src/Cutline.Api
   ```
4. Run the client:
   ```bash
   cd client && npm install && npm run dev
   ```

## Production Deployment

Build and run the full stack:
```bash
docker compose up --build
```

The API is exposed on port 3000 and the client on port 80.

## Configuration

All configuration is via environment variables or `appsettings.json`:

| Variable | Description |
|---|---|
| `ConnectionStrings__Postgres` | PostgreSQL connection string |
| `ConnectionStrings__Redis` | Redis connection string (host:port) |
| `ESPN_POLL_INTERVAL_SECONDS` | Live score polling interval (default: 60) |

## AI Features

The `Cutline.AI` project (weekly reports, trade grader, waiver assistant) requires an Anthropic API key and is disabled in self-hosted deployments by default.

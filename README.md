# Snack Counter

The repository contains a .NET 8 API and React/Vite client for the snack replenishment prototype.

## Run locally

```powershell
dotnet run --project .\SnackCounter.Server
Set-Location .\snackcounter.client
npm run dev
```

The API vertical slice is available under `/api/snack-counter` and the health check is `/health`.

The current first slice keeps state in a concurrency-protected service so the workflow can be exercised without credentials. PostgreSQL persistence, transactional outbox delivery, and simulated external adapters should be added behind this HTTP contract. Never put a database password in source control; use `dotnet user-secrets` or `DATABASE_URL`.

## Configure PostgreSQL

The server reads `ConnectionStrings:DefaultConnection` first, then `DATABASE_URL`.
Use a rotated Neon connection string only on your machine:

```powershell
dotnet user-secrets init --project .\SnackCounter.Server
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<ROTATED_NEON_CONNECTION_STRING>" --project .\SnackCounter.Server
dotnet run --project .\SnackCounter.Server
```

`GET /health` reports PostgreSQL availability. An unconfigured database is reported as unhealthy rather than silently treated as connected.

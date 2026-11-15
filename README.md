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
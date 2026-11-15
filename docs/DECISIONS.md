# Decisions

## 2026-11-15: Start with a vertical slice

The repository is a clean starter rather than an existing business application. The smallest useful increment is a report-to-proof flow with dashboard and monthly report endpoints. A service boundary keeps the API usable while PostgreSQL repositories, outbox delivery, simulated HR/chat/email adapters, and background workers are added without changing the HTTP contract.

## 2026-11-15: No credentials in source

No database connection string is stored in this repository. When the PostgreSQL adapter is added, it will read `DATABASE_URL` or .NET user-secrets.

# Snack Counter Automation

Automated snack replenishment and reimbursement system for two international offices: **Oslo** (6 000 NOK/mo) and **Bengaluru** (15 000 INR/mo).

Designed to solve four core failures in informal snack management:
1. **Empty boxes sitting for days** (solved via instant chat/email notifications, 4-hour auto-pick rotation, and 48-hour completion deadline enforcement).
2. **Duplicate shopping on the same day** (solved via strict container concurrency locking).
3. **Volunteer debt burden & uneven workload** (solved via fair rotation, max 2 open tickets limit, debt protection warning when unpaid claims ≥ 2, and automatic HR claim creation).
4. **Lack of spending & consumption visibility** (solved via local currency budget reservations, consolidated NOK management reports at locked 0.13 FX rate, and top snacks consumption analytics).

---

## Quick Start (Run Locally)

### 1. Run the Backend (.NET 8 Web API)
```powershell
dotnet run --project .\SnackCounter.Server
```
- API Base: `http://localhost:5212/api/v1/snack-counter` (or https: `https://localhost:7223`)
- Swagger UI: `http://localhost:5212/swagger`
- Health check: `http://localhost:5212/health`

### 2. Run the Frontend (React / Vite)
```powershell
Set-Location .\snackcounter.client
npm run dev
```
Open your browser at `https://localhost:60750` (or the port Vite outputs).

---

## Run Automated Tests
```powershell
dotnet test .\SnackCounter.Server\Tests\SnackCounter.Server.Tests\SnackCounter.Server.Tests.csproj
```
All 12 automated tests verify:
- Race condition protection (two people cannot commit to the same refill)
- Active ticket lock per container (`Refill already in progress`)
- Over-budget reservation requiring Admin approval
- Financial debt protection consent prompt when unpaid claims ≥ 2
- Fair auto-pick rotation (fewest open tickets, longest since last assignment)
- 48-hour deadline expiry & automatic reassignment with fresh deadline
- Ticket transfers preserving original deadline
- Proof submission with excess spend flagged but never blocked
- Simulated HR downtime queue and idempotent retries with stable reference IDs (`SNACK-{id}`)
- Nightly batch HR reimbursement sync settling paid claims and closing tickets
- "Already refilled" side-path closing tickets and filing exception claims
- Multi-currency consolidated management report (NOK + INR at locked 0.13 rate)

---

## Documentation
- **Design Document (1–2 pages)**: [`docs/DESIGN.md`](file:///d:/Projects/snack-counter/docs/DESIGN.md)
- **10-Minute Walkthrough Script**: [`docs/WALKTHROUGH.md`](file:///d:/Projects/snack-counter/docs/WALKTHROUGH.md)
- **Technical Decisions**: [`docs/DECISIONS.md`](file:///d:/Projects/snack-counter/docs/DECISIONS.md)

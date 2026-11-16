# Snack Counter Automation — System Design Document

## 1. Executive Summary & Problem Context
Today, the snack counters across our two offices (**Oslo**, ~15 volunteers, 6 000 NOK/mo; and **Bengaluru**, ~20 volunteers, 15 000 INR/mo) run on informal goodwill and ad-hoc chat messages. This informal process suffers from four systemic failures:
1. **Empty boxes sit for days** because of diffusion of responsibility ("someone else will do it").
2. **Duplicate purchases occur** when two volunteers buy snacks on the same day without coordination.
3. **Volunteer burnout and debt burden**: A few dedicated volunteers make most purchases and wait weeks for HR reimbursement, tying up significant personal funds.
4. **Zero spending and consumption visibility**: Management lacks consolidated visibility across currencies and has no data on actual snack consumption.

This system automates the complete replenishment lifecycle—from QR scanning to HR reimbursement—with fair rotation, budget reservation, financial protection, and automated claim submission.

---

## 2. Refill Lifecycle: From "Empty" to "Reimbursed"

```
[Empty Container]
       │
       ▼ (Scan QR code / manual pick + 3 confirmations + SSO login)
[1. Ticket Creation] ──(Already active?)──► Conflict: "Refill already in progress" (immutable snapshot)
       │
       ▼ (Check monthly office budget in local currency: NOK/INR)
[2. Budget Gate] ────(Exceeds limit?)────► [Awaiting Admin Approval] ──(Decline)──► Cancelled
       │ (Fits budget or approved)
       ▼ (Reserve budget from monthly pool)
[3. Notify Volunteers] ──► Dispatches Chat (Slack/Teams) & Email with 1-time link to eligible volunteers
       │
       ├─────────────────────────────────┐
       ▼ (Volunteer accepts)             ▼ (No accept within 4 hours)
[4. Ownership Claim]             [5. Auto-Pick Fallback]
  • If unpaid claims >= 2:         • Orders by: fewest open tickets -> longest since last -> random
    Financial consent prompt       • Sends consent request if unpaid claims >= 2
  • First to accept wins           • Alerts Admin if no volunteers available
       │                                 │
       └────────────────┬────────────────┘
                        ▼
           [6. "Do It" Phase (48h)]
             • Transferrable (preserves original 48h deadline)
             • If 48h expires without proof: assignment expires; auto-picked to next volunteer
                        │
                        ▼ (Owner buys snacks & uploads receipt, photo, actual amount)
           [7. Proof Submission]
             • Reservation converts to actual spend
             • Amount > estimate: flagged as ProofExceedsEstimate (never blocked)
                        │
                        ▼ (Automated claim with unique deterministic ID: SNACK-{ticketId})
           [8. External HR Claim]
             • If HR down: queued as PendingRetry; retried with SAME reference ID (idempotent)
                        │
                        ▼ (Nightly batch sync job)
           [9. Reimbursement Settlement]
             • HR approves and pays claim
             • Ticket moves to Closed; volunteer debt count decremented
                        │
                        ▼
           [10. Management Report]
             • Native office spend (NOK / INR) + consolidated NOK total at locked FX (0.13)
             • Top snacks ranked by quantity and spend

  ★ Side Path: "Already Refilled" Without a Ticket
    Employee taps "Already Refilled" ──► Active ticket closes immediately (no redundant shopping)
                                      ──► Reservation released
                                      ──► Exception claim submitted to Admin in buyer's name (never credited to assigned volunteer)
```

---

## 3. Main System Components & Data Kept

| Component | Responsibility | Data Kept |
| :--- | :--- | :--- |
| **Catalog & Counters** | Office, counter, and container hierarchy with default quantities and prices. | `ContainerId`, `OfficeId`, `CounterName`, `SnackName`, `DefaultQuantity`, `DefaultUnitPrice`. |
| **Ticket Engine** | State machine managing ticket lifecycle, reservations, and ownership. | `TicketId`, `ContainerId`, `Quantity`, `UnitPrice`, `OfficeId`, `Currency`, `ReservedBudget`, `Status`, `VolunteerId`, `CreatedAt`, `AcknowledgementDeadline`, `CompletionDeadline`, `SpentAmount`, `Items`, `ReceiptFileName`, `CounterPhotoFileName`, `ProofExceedsEstimate`, `IsAutoAssigned`, `ClosedReason`. |
| **Volunteer Directory** | Volunteer status, hybrid location, and debt load tracking. | `VolunteerId`, `Name`, `Email`, `HomeOfficeId`, `CurrentOfficeId`, `PayrollCurrency`, `IsActive`, `IsOnLeave`, `OpenTicketsCount`, `UnpaidClaimsCount`, `LastAssignedAt`. |
| **Simulated Chat & Email Stand-in** | Dispatches real-time chat (Slack/Teams) and emails to eligible volunteers. | `Id`, `OfficeId`, `Channel`, `RecipientId`, `Title`, `Body`, `ActionUrl`, `Timestamp`. |
| **Simulated HR Stand-in** | External payroll claim adapter with downtime queue and batch status sync. | `ClaimId`, `TicketId`, `ReferenceId` (`SNACK-{id}`), `VolunteerId`, `Amount`, `Currency`, `Status` (`PendingRetry`, `Submitted`, `Approved`, `Paid`, `Rejected`), `CreatedAt`, `SettledAt`. |
| **Budget & Reporting** | Office budget enforcement and multi-currency consolidated analytics. | `OfficeId`, `MonthlyBudget`, `Reserved`, `Spent`, `LockedFxRate` (0.13 INR->NOK), `TopSnacks`. |

---

## 4. How the Core Breakdown Problems Are Prevented

| Problem | Root Cause | Automated Solution |
| :--- | :--- | :--- |
| **1. Box sits empty for days** | Diffusion of responsibility; no clear owner. | Instant chat & email notification to all eligible volunteers. If unclaimed within 4 hours, **Auto-Pick** assigns the fairest volunteer. A strict **48-hour completion window** reassigns uncompleted refills to prevent stagnation. |
| **2. Two people buy snacks on the same day** | Disjoint communication; no single source of truth. | Strict **Container Lock**: exactly 1 active ticket per container. The first volunteer to acknowledge wins the race condition; all others receive "Refill already in progress" or "Already taken". If someone already refilled informally, the **"Already Refilled" side path** closes the ticket immediately. |
| **3. A few volunteers do all refills & wait weeks** | Uneven volunteer burden and payroll reimbursement lag. | **Fair Rotation**: Auto-pick prioritizes volunteers with fewest open tickets and longest time since last refill. Max **2 open tickets** per volunteer. **Financial Protection**: If a volunteer has ≥ 2 unpaid claims, the system warns them that funds are tied up and requires explicit consent. Claims are filed to HR automatically upon proof. |
| **4. Nobody knows monthly spend or consumption** | No central tracking; dual currencies (NOK/INR). | **Budget Reservation**: Estimated cost is reserved upfront in local currency. If a purchase exceeds the monthly budget, it halts for Admin approval. The **Consolidated Management Report** displays native currencies, consolidated NOK total at the locked monthly rate (0.13), and top snacks ranked by spend and quantity. |

---

## 5. Architectural Trade-offs & What Was Deliberately Not Built

1. **Deliberately Not Built: Native Mobile Application**
   - *Rationale*: A lightweight, responsive web application accessible via mobile browser fulfills QR scanning (`/r/{containerId}`) and desktop workflows without mobile app store deployment, device fragmentation, or MDM friction.
2. **Trade-Off: Asynchronous Stand-in HR Integration with Idempotent Reference IDs**
   - *Rationale*: The external HR system is owned by a third party and subject to 1-10 day approval delays and network downtime. We deliberately avoid synchronous dependencies. Claims use deterministic reference IDs (`SNACK-{ticketId}`) and a `PendingRetry` queue to guarantee zero duplicate reimbursements even during outages.
3. **Trade-Off: Locked Monthly Exchange Rate (1 INR = 0.13 NOK) vs. Live Forex API**
   - *Rationale*: Financial budgeting requires predictable monthly allocations. Live forex rates introduce intra-month volatility into office budgets. Locking the exchange rate monthly simplifies cross-office reporting while maintaining strict financial control.
4. **Deliberately Not Built: Heavy OCR / Automated Machine Learning Receipt Parsing**
   - *Rationale*: Complex OCR models introduce classification failures for handwritten or international receipts. Requiring volunteer input of the actual amount alongside uploaded photo proof is simpler, faster, and reliable; excess spend is flagged but never blocked.

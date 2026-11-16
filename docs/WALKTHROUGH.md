# 10-Minute Walkthrough & Demonstration Script

Use this script to present your system, architecture, and tests to the evaluation panel.

---

## Minute 0–2: Introduction & Problem Context (The "Why")
- **The Situation**: We operate two offices (**Oslo**, ~15 volunteers, 6 000 NOK/mo; and **Bengaluru**, ~20 volunteers, 15 000 INR/mo).
- **The Breakdown**: Informal goodwill created four core failure points:
  1. Boxes sit empty for days (diffusion of responsibility).
  2. Two people buy snacks on the same day (no concurrency lock).
  3. A few volunteers do all the work and wait weeks for HR reimbursement (financial burden).
  4. Zero management visibility into monthly spending across currencies or snack consumption.
- **The Solution**: An automated, end-to-end web system with concurrency-safe ticket locking, budget reservation, financial protection, fair auto-pick rotation, and automated HR claim submission.

---

## Minute 2–4: Architecture & Design Rationale (The "How")
- Open [`docs/DESIGN.md`](file:///d:/Projects/snack-counter/docs/DESIGN.md).
- Explain the key architectural principles:
  - **Thread-safe state engine** ensuring exactly 1 active ticket per container.
  - **Budget gate**: Estimates are reserved from the monthly budget in local currency (NOK/INR). Over-budget requests require Admin approval before volunteers are notified.
  - **Stand-In Adapters**: Simulated internal chat/email service and HR payroll adapter with network downtime simulator and idempotent reference IDs (`SNACK-{ticketId}`).
  - **Debt protection**: Volunteers with ≥ 2 unpaid claims must explicitly consent before taking more tickets, preventing out-of-pocket financial strain.

---

## Minute 4–8: Live Web UI Demonstration (The 11 Steps + Side Path)

### Step 1 & 2: Report & Concurrency Lock
1. In the **Workflow & Refill Hub**, select the Oslo office and click on container `pantry-oslo-01` (Salted Almonds).
2. Check the 3 mandatory verification boxes (*Right container*, *Verified empty*, *Ready to report*).
3. Click **Create Refill Ticket →**.
4. *Demonstrate duplicate protection*: Click **Create Refill Ticket →** again. Show that the system immediately rejects the duplicate with `"Refill already in progress"`, displaying the locked snapshot.

### Step 3: Budget Gate & Admin Override
1. In the Report form, enter an over-budget refill (e.g. 70 units of chocolate = 7,000 NOK, exceeding Oslo's 6,000 NOK budget).
2. Show that the ticket moves to `AwaitingBudgetApproval` without reserving funds.
3. Switch to the **HR & Admin Control Room** tab. Click **Approve Budget Override** to release the ticket to volunteers.

### Step 4: Simulated Internal Chat & Email Feed
1. Open the **Simulated Chat & Emails** tab.
2. Show the real-time Slack/Teams messages and email notifications sent to eligible volunteers with one-time action links. Ineligible volunteers (on leave or already holding 2 open tickets) are automatically excluded.

### Step 5: Volunteer Accept & Debt Protection
1. In the top SSO switcher, select **Astrid Lind** (who has 2 unpaid claims pending in HR).
2. On an open ticket, click **Accept Refill →**.
3. **Show the Debt Protection Modal**: The system warns:
   > *"Your earlier claims are still being reimbursed. Another purchase will tie up more of your money. Continue?"*
4. Click *No, Keep Ticket Open* to show that the ticket stays available for others.
5. Switch SSO user to **Lars Holm** (0 unpaid claims) and click **Accept Refill →** to become the confirmed owner.

### Step 6: 4-Hour Auto-Pick Rotation
1. On any remaining open ticket, click **⏱️ Simulate 4h Auto-Pick**.
2. Show how the system executes the rotation algorithm: selects the volunteer with the fewest open tickets and longest time since their last assignment.

### Step 7: 48h Deadline, Transfers & Expiry
1. On an assigned ticket, click **🔄 Transfer Ticket**. Select a teammate in the same office and confirm the transfer, pointing out that the original 48h deadline is preserved.
2. Click **⌛ Simulate 48h Expiry**. Show that the assignment expires and the ticket is immediately handed off to the next candidate with a fresh 48-hour deadline.

### Step 8: Proof Submission & Excess Spend Handling
1. Click **📸 Upload Proof & Refill Done**.
2. Enter an actual spend higher than the estimate (e.g. 520 NOK vs 450 NOK estimate).
3. Point out the live notice: *Amount exceeds estimated budget. Flagged as ProofExceedsEstimate but never blocked.* Submit proof.

### Step 9 & 10: HR System, Downtime, Retries & Nightly Sync
1. In **HR & Admin Control Room**, click **🔴 Simulate HR Downtime**.
2. When a claim is created, show that it enters `PendingRetry` with reference ID `SNACK-{id}`.
3. Bring HR back online (**🟢 Bring HR Back Online**) and click **🔄 Retry Pending Claims**. Show that the claim retried with the exact same ID, guaranteeing idempotency.
4. Click **🌙 Run Nightly Batch Sync**. Show that HR approves and pays the claim, settling the ticket to `Closed` and decrementing the volunteer's debt count.

### Step 11: Management Spending & Analytics Report
1. Open the **Monthly Management Report** tab.
2. Point out:
   - Oslo native spending in NOK with utilization bar.
   - Bengaluru native spending in INR with utilization bar.
   - **Consolidated Executive Total in NOK** calculated at the locked monthly exchange rate (1 INR = 0.13 NOK).
   - **Top Consumed Snacks** ranking table tracking actual consumption and spending.

### Side Path: "Already Refilled" Without a Ticket
1. In the Report form, click **✨ Already Refilled? (Side Path)**.
2. Enter the amount and receipt reference for an employee who refilled informally.
3. Show that any in-flight ticket for that container is immediately closed so nobody shops redundantly, and an exception claim is filed directly in the buyer's name (never credited to the assigned volunteer).

---

## Minute 8–10: Automated Test Suite & Code Walkthrough
- Open terminal and run:
  ```powershell
  dotnet test .\SnackCounter.Server\Tests\SnackCounter.Server.Tests\SnackCounter.Server.Tests.csproj
  ```
- Highlight the 12 comprehensive unit tests:
  - `Rule1`: Duplicate container report returns existing ticket (`Refill already in progress`).
  - `Rule2`: Two people cannot both commit to the same refill (concurrency race condition).
  - `Rule3`: Over-budget reservation requires Admin approval.
  - `Rule4`: Volunteer debt protection enforces consent when unpaid claims ≥ 2.
  - `Rule5`: Auto-pick selects fair volunteer and excludes ineligible staff.
  - `Rule6`: Deadline expiry reassigns to next volunteer with fresh deadline.
  - `Rule7`: Ticket transfer preserves original completion deadline.
  - `Rule8`: Proof submission flags excess spend without blocking.
  - `Rule9`: Simulated HR downtime queues claims and retries idempotently with same reference ID.
  - `Rule10`: Nightly batch sync settles paid claims and closes tickets.
  - `Rule11`: "Already refilled" side path closes ticket and files exception claim.
  - `Rule12`: Monthly report consolidates currencies with locked FX rate (0.13).
- Conclude with architecture highlights: separation of concerns, clean interfaces, and extensible domain design.

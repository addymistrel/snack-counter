# Reimbursement claim

## Core acceptance criteria

1. Valid proof creates one simulated reimbursement claim.
2. The claim has a stable reference ID derived from the ticket, volunteer, amount, and currency.
3. New claims start in `Submitted` status and can be retrieved for the ticket.

The prototype simulates HR in-process. A real outbox and HR adapter are
intentionally out of scope for this core demonstration.

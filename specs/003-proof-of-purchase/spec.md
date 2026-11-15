# Proof of purchase

## Core acceptance criteria

1. Only the assigned volunteer can submit proof for an acknowledged ticket.
2. Proof requires a positive amount, at least one non-empty item, a receipt reference, and a counter-photo reference.
3. Valid proof moves the ticket to `Submitted` and records the actual amount and cleaned item names.
4. An amount above the reservation is accepted and flagged as `ProofExceedsEstimate`.

This prototype uses filenames as local proof references. Actual file storage and
reimbursement claims are separate future slices.

# P0 replenishment prototype

## Acceptance criteria

1. A report is rejected unless the reporter confirms an empty container, location, and snack type.
2. A container has at most one active ticket.
3. A ticket reserves budget in the office currency and is rejected when the monthly budget is exhausted.
4. Only one volunteer can acknowledge an open ticket; a volunteer cannot hold more than two open tickets.
5. Proof requires a positive amount, items, receipt filename, and counter-photo filename.
6. A monthly report shows native office totals and a consolidated NOK total using the stored prototype exchange rate.

The API currently exposes these criteria through `api/snack-counter`. Persistence and external adapters are intentionally isolated as the next infrastructure slice.

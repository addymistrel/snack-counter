# Volunteer acknowledgement

## Core acceptance criteria

1. The first volunteer to acknowledge an open ticket wins.
2. A later acknowledgement receives a conflict response and cannot replace the owner.
3. A volunteer can have at most two active tickets.
4. A volunteer with an existing active ticket must explicitly consent before taking another.

The prototype uses a process lock for this demo slice. PostgreSQL conditional updates
and row locks will replace it when the ticket workflow moves to the persisted service.

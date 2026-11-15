# Report and ticket

## AC-01: QR and manual reporting share one use case

Given a user opens `/r/{containerId}`, when the report form renders, then the
container ID is prefilled and the user still confirms the container is empty,
the location, and the snack type before submitting.

Given a user opens the normal dashboard, when the user enters a container ID
manually, then the same report form and API request are used.

## Scope

This slice covers the client entry point and reuse of the existing report
endpoint. Container lookup, authentication, and database-backed ticket
creation remain separate slices.

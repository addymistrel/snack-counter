namespace SnackCounter.Server.Models;

public enum TicketStatus
{
    Open,
    Acknowledged,
    AwaitingProof,
    Submitted,
    Closed
}

public sealed record Office(
    string Id,
    string Name,
    string Currency,
    decimal MonthlyBudget);

public sealed record SnackTicket(
    Guid Id,
    string ContainerId,
    string SnackName,
    int Quantity,
    decimal UnitPrice,
    string OfficeId,
    string OfficeName,
    string Currency,
    decimal ReservedBudget,
    TicketStatus Status,
    string? VolunteerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset AcknowledgementDeadline,
    decimal? SpentAmount,
    IReadOnlyList<string> Items);

public sealed record ReportContainerRequest(
    string ContainerId,
    string OfficeId,
    bool ContainerEmpty,
    bool LocationConfirmed,
    bool SnackTypeConfirmed,
    string SnackName = "Assorted snacks",
    int Quantity = 5,
    decimal UnitPrice = 100m);

public sealed record AcknowledgeRequest(string VolunteerId, bool ReimbursementConsent);

public sealed record ProofRequest(
    string VolunteerId,
    decimal Amount,
    IReadOnlyList<string> Items,
    string ReceiptFileName,
    string CounterPhotoFileName);

public sealed record OfficeSummary(
    string OfficeId,
    string OfficeName,
    string Currency,
    decimal Budget,
    decimal Reserved,
    decimal Spent,
    int OpenTickets,
    int ClosedTickets);

public sealed record MonthlyReport(
    string Month,
    decimal ConsolidatedNokTotal,
    IReadOnlyList<OfficeSummary> Offices,
    IReadOnlyList<SnackSummary> TopSnacks);

public sealed record SnackSummary(string Name, int Quantity, decimal Spend, string Currency);

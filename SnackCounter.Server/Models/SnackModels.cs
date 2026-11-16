namespace SnackCounter.Server.Models;

public enum TicketStatus
{
    Open,
    AwaitingBudgetApproval,
    Acknowledged,
    Submitted,
    Closed,
    Cancelled
}

public sealed record Office(
    string Id,
    string Name,
    string Currency,
    decimal MonthlyBudget);

public sealed record ContainerCatalogItem(
    string ContainerId,
    string OfficeId,
    string CounterName,
    string SnackName,
    int DefaultQuantity,
    decimal DefaultUnitPrice);

public sealed record Volunteer(
    string Id,
    string Name,
    string Email,
    string HomeOfficeId,
    string CurrentOfficeId,
    string PayrollCurrency,
    bool IsActive,
    bool IsOnLeave,
    int OpenTicketsCount,
    int UnpaidClaimsCount,
    DateTimeOffset? LastAssignedAt);

public sealed record SnackTicket(
    Guid Id,
    string ContainerId,
    string CounterName,
    string SnackName,
    int Quantity,
    decimal UnitPrice,
    string OfficeId,
    string OfficeName,
    string Currency,
    decimal ReservedBudget,
    TicketStatus Status,
    string? VolunteerId,
    string? VolunteerName,
    DateTimeOffset CreatedAt,
    DateTimeOffset AcknowledgementDeadline,
    DateTimeOffset? CompletionDeadline,
    decimal? SpentAmount,
    IReadOnlyList<string> Items,
    string? ReceiptFileName,
    string? CounterPhotoFileName,
    bool ProofExceedsEstimate = false,
    bool IsAutoAssigned = false,
    string? ExpiredVolunteerId = null,
    string? ClosedReason = null,
    string? ReporterId = null,
    string? ReporterName = null,
    string? ExceptionClaimId = null);

public sealed record ReportContainerRequest(
    string ContainerId,
    string OfficeId,
    bool ContainerEmpty,
    bool LocationConfirmed,
    bool SnackTypeConfirmed,
    string CounterName = "Main Pantry",
    string SnackName = "Assorted snacks",
    int Quantity = 5,
    decimal UnitPrice = 100m,
    string ReporterId = "emp-guest",
    string ReporterName = "Guest Employee");

public sealed record AcknowledgeRequest(
    string VolunteerId,
    bool ReimbursementConsent = false);

public sealed record TransferTicketRequest(
    string FromVolunteerId,
    string ToVolunteerId,
    bool ReimbursementConsent = false);

public sealed record ProofRequest(
    string VolunteerId,
    decimal Amount,
    IReadOnlyList<string> Items,
    string ReceiptFileName,
    string CounterPhotoFileName);

public sealed record BudgetApprovalRequest(
    bool Approved,
    string? AdminNotes = null);

public sealed record AlreadyRefilledRequest(
    string ContainerId,
    string OfficeId,
    string ReporterId,
    string ReporterName,
    decimal Amount,
    IReadOnlyList<string> Items,
    string ReceiptFileName);

public sealed record NotificationMessage(
    Guid Id,
    string OfficeId,
    string Channel, // "Chat" or "Email"
    string RecipientId,
    string RecipientName,
    string Title,
    string Body,
    string ActionUrl,
    DateTimeOffset Timestamp);

public enum ClaimStatus
{
    PendingRetry,
    Submitted,
    Approved,
    Paid,
    Rejected
}

public sealed record ReimbursementClaim(
    Guid Id,
    Guid TicketId,
    string ReferenceId,
    string VolunteerId,
    string VolunteerName,
    decimal Amount,
    string Currency,
    ClaimStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SettledAt = null,
    string? RejectionReason = null,
    bool IsException = false,
    string? ExceptionReporterName = null);

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
    decimal LockedExchangeRateInrToNok,
    IReadOnlyList<OfficeSummary> Offices,
    IReadOnlyList<SnackSummary> TopSnacks);

public sealed record SnackSummary(string Name, int Quantity, decimal Spend, string Currency);

public sealed record DashboardData(
    IReadOnlyCollection<Office> Offices,
    IReadOnlyCollection<ContainerCatalogItem> Containers,
    IReadOnlyCollection<Volunteer> Volunteers,
    IReadOnlyCollection<SnackTicket> Tickets,
    IReadOnlyCollection<NotificationMessage> Notifications,
    IReadOnlyCollection<ReimbursementClaim> Claims,
    bool IsHRDown);

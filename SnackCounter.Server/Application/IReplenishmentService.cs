using SnackCounter.Server.Models;

namespace SnackCounter.Server.Application;

public interface IReplenishmentService
{
    Task<DashboardData> GetDashboardDataAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Office>> GetOfficesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ContainerCatalogItem>> GetContainersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Volunteer>> GetVolunteersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SnackTicket>> GetTicketsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<NotificationMessage>> GetNotificationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ReimbursementClaim>> GetClaimsAsync(CancellationToken cancellationToken = default);
    Task<ReimbursementClaim?> GetClaimAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<bool> ToggleHRDownAsync(CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, string? Error)> ReportContainerAsync(
        ReportContainerRequest request,
        CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, string? Error)> AcknowledgeAsync(
        Guid ticketId,
        AcknowledgeRequest request,
        CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, string? Error)> TransferTicketAsync(
        Guid ticketId,
        TransferTicketRequest request,
        CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, string? Error)> SimulateAutoPickAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, string? Error)> SimulateExpiryAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, string? Error)> ApproveBudgetAsync(
        Guid ticketId,
        BudgetApprovalRequest request,
        CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, string? Error)> SubmitProofAsync(
        Guid ticketId,
        ProofRequest request,
        CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, ReimbursementClaim? Claim, string? Error)> ReportAlreadyRefilledAsync(
        AlreadyRefilledRequest request,
        CancellationToken cancellationToken = default);

    Task<int> RetryPendingClaimsAsync(CancellationToken cancellationToken = default);
    Task<int> RunNightlySyncAsync(CancellationToken cancellationToken = default);
    Task ResetDemoDataAsync(CancellationToken cancellationToken = default);

    Task<MonthlyReport> GetMonthlyReportAsync(DateOnly month, CancellationToken cancellationToken = default);
}

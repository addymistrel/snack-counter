using SnackCounter.Server.Models;

namespace SnackCounter.Server.Application;

public interface IReplenishmentRepository
{
    Task<IReadOnlyCollection<Office>> GetOfficesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ContainerCatalogItem>> GetContainersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Volunteer>> GetVolunteersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SnackTicket>> GetTicketsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<NotificationMessage>> GetNotificationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ReimbursementClaim>> GetClaimsAsync(CancellationToken cancellationToken = default);
    Task<ReimbursementClaim?> GetClaimAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<bool> IsHRDownAsync(CancellationToken cancellationToken = default);
    Task<bool> ToggleHRDownAsync(CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, string? Error)> CreateTicketAsync(
        Office office,
        ReportContainerRequest request,
        decimal estimatedCost,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, string? Error)> AcknowledgeAsync(
        Guid ticketId,
        string volunteerId,
        bool reimbursementConsent,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, string? Error)> TransferTicketAsync(
        Guid ticketId,
        string fromVolunteerId,
        string toVolunteerId,
        bool reimbursementConsent,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, string? Error)> SimulateAutoPickAsync(
        Guid ticketId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, string? Error)> SimulateExpiryAsync(
        Guid ticketId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, string? Error)> ApproveBudgetAsync(
        Guid ticketId,
        bool approved,
        string? adminNotes,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, string? Error)> SubmitProofAsync(
        Guid ticketId,
        ProofRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<(SnackTicket? Ticket, ReimbursementClaim? Claim, string? Error)> ReportAlreadyRefilledAsync(
        AlreadyRefilledRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<int> RetryPendingClaimsAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    Task<int> RunNightlySyncAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    Task ResetDemoDataAsync(CancellationToken cancellationToken = default);
}

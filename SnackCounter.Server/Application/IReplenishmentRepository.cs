using SnackCounter.Server.Models;

namespace SnackCounter.Server.Application;

public interface IReplenishmentRepository
{
    Task<IReadOnlyCollection<Office>> GetOfficesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SnackTicket>> GetTicketsAsync(CancellationToken cancellationToken = default);
    Task<ReimbursementClaim?> GetClaimAsync(Guid ticketId, CancellationToken cancellationToken = default);
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
    Task<(SnackTicket? Ticket, string? Error)> SubmitProofAsync(
        Guid ticketId,
        ProofRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}

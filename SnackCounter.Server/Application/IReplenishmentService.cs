using SnackCounter.Server.Models;

namespace SnackCounter.Server.Application;

public interface IReplenishmentService
{
    Task<IReadOnlyCollection<Office>> GetOfficesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SnackTicket>> GetTicketsAsync(CancellationToken cancellationToken = default);
    Task<(SnackTicket? Ticket, string? Error)> ReportContainerAsync(
        ReportContainerRequest request,
        CancellationToken cancellationToken = default);
    Task<(SnackTicket? Ticket, string? Error)> AcknowledgeAsync(
        Guid ticketId,
        AcknowledgeRequest request,
        CancellationToken cancellationToken = default);
    Task<(SnackTicket? Ticket, string? Error)> SubmitProofAsync(
        Guid ticketId,
        ProofRequest request,
        CancellationToken cancellationToken = default);
    Task<ReimbursementClaim?> GetClaimAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<MonthlyReport> GetMonthlyReportAsync(DateOnly month, CancellationToken cancellationToken = default);
}

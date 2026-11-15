using SnackCounter.Server.Models;

namespace SnackCounter.Server.Application;

public interface IReplenishmentService
{
    IReadOnlyCollection<Office> Offices { get; }
    IReadOnlyCollection<SnackTicket> GetTickets();
    (SnackTicket? Ticket, string? Error) ReportContainer(ReportContainerRequest request);
    (SnackTicket? Ticket, string? Error) Acknowledge(Guid ticketId, AcknowledgeRequest request);
    (SnackTicket? Ticket, string? Error) SubmitProof(Guid ticketId, ProofRequest request);
    ReimbursementClaim? GetClaim(Guid ticketId);
    MonthlyReport GetMonthlyReport(DateOnly month);
}

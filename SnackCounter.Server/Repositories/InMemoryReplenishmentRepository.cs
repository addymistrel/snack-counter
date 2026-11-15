using System.Collections.Concurrent;
using SnackCounter.Server.Application;
using SnackCounter.Server.Models;

namespace SnackCounter.Server.Repositories;

public sealed class InMemoryReplenishmentRepository : IReplenishmentRepository
{
    private readonly ConcurrentDictionary<Guid, SnackTicket> _tickets = new();
    private readonly ConcurrentDictionary<Guid, ReimbursementClaim> _claims = new();
    private readonly ConcurrentDictionary<string, int> _volunteerOpenTickets = new();
    private readonly Dictionary<string, decimal> _monthlyReserved = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, decimal> _monthlySpent = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _stateGate = new();
    private readonly IReadOnlyDictionary<string, Office> _offices = new Dictionary<string, Office>(StringComparer.OrdinalIgnoreCase)
    {
        ["oslo"] = new("oslo", "Oslo", "NOK", 6000m),
        ["bengaluru"] = new("bengaluru", "Bengaluru", "INR", 15000m)
    };

    public Task<IReadOnlyCollection<Office>> GetOfficesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Office>>(_offices.Values.ToArray());

    public Task<IReadOnlyCollection<SnackTicket>> GetTicketsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<SnackTicket>>(
            _tickets.Values.OrderByDescending(ticket => ticket.CreatedAt).ToArray());

    public Task<ReimbursementClaim?> GetClaimAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_claims.TryGetValue(ticketId, out var claim) ? claim : null);

    public Task<(SnackTicket? Ticket, string? Error)> CreateTicketAsync(
        Office office,
        ReportContainerRequest request,
        decimal estimatedCost,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            var existing = _tickets.Values.FirstOrDefault(ticket =>
                ticket.ContainerId.Equals(request.ContainerId, StringComparison.OrdinalIgnoreCase) &&
                ticket.Status != TicketStatus.Closed);
            if (existing is not null)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>(
                    (existing, "This container already has an active ticket."));

            var budgetKey = GetBudgetKey(office.Id, now);
            var reserved = _monthlyReserved.GetValueOrDefault(budgetKey);
            var spent = _monthlySpent.GetValueOrDefault(budgetKey);
            if (spent + reserved + estimatedCost > office.MonthlyBudget)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>(
                    (null, $"The {office.Name} monthly budget has no room for another reservation."));

            _monthlyReserved[budgetKey] = reserved + estimatedCost;
            var ticket = new SnackTicket(
                Guid.NewGuid(),
                request.ContainerId.Trim(),
                request.SnackName.Trim(),
                request.Quantity,
                request.UnitPrice,
                office.Id,
                office.Name,
                office.Currency,
                estimatedCost,
                TicketStatus.Open,
                null,
                now,
                now.AddHours(4),
                null,
                Array.Empty<string>());
            _tickets[ticket.Id] = ticket;
            return Task.FromResult<(SnackTicket? Ticket, string? Error)>((ticket, null));
        }
    }

    public Task<(SnackTicket? Ticket, string? Error)> AcknowledgeAsync(
        Guid ticketId,
        string volunteerId,
        bool reimbursementConsent,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            if (!_tickets.TryGetValue(ticketId, out var ticket))
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Ticket not found."));
            if (ticket.Status != TicketStatus.Open)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "This ticket is no longer available."));
            if (ticket.AcknowledgementDeadline <= now)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "The acknowledgement window has expired."));
            var openCount = _volunteerOpenTickets.GetValueOrDefault(volunteerId);
            if (openCount >= 2)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Volunteers can have at most two open tickets."));
            if (openCount >= 1 && !reimbursementConsent)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Reimbursement consent is required when you have two or more unpaid claims."));

            var acknowledged = ticket with { Status = TicketStatus.Acknowledged, VolunteerId = volunteerId };
            _tickets[ticketId] = acknowledged;
            _volunteerOpenTickets[volunteerId] = openCount + 1;
            return Task.FromResult<(SnackTicket? Ticket, string? Error)>((acknowledged, null));
        }
    }

    public Task<(SnackTicket? Ticket, string? Error)> SubmitProofAsync(
        Guid ticketId,
        ProofRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            if (!_tickets.TryGetValue(ticketId, out var ticket))
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Ticket not found."));
            if (ticket.Status != TicketStatus.Acknowledged || ticket.VolunteerId != request.VolunteerId)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>(
                    (null, "Only the assigned volunteer can submit proof for an acknowledged ticket."));

            var submitted = ticket with
            {
                Status = TicketStatus.Submitted,
                SpentAmount = request.Amount,
                Items = request.Items,
                ProofExceedsEstimate = request.Amount > ticket.ReservedBudget
            };
            _tickets[ticketId] = submitted;
            var budgetKey = GetBudgetKey(ticket.OfficeId, ticket.CreatedAt);
            _monthlyReserved[budgetKey] = Math.Max(0, _monthlyReserved.GetValueOrDefault(budgetKey) - ticket.ReservedBudget);
            _monthlySpent[budgetKey] = _monthlySpent.GetValueOrDefault(budgetKey) + request.Amount;
            _claims[ticketId] = new ReimbursementClaim(
                Guid.NewGuid(),
                ticketId,
                $"SNACK-{ticketId:N}",
                request.VolunteerId,
                request.Amount,
                ticket.Currency,
                ClaimStatus.Submitted,
                now);
            _volunteerOpenTickets.AddOrUpdate(request.VolunteerId, 0, (_, value) => Math.Max(0, value - 1));
            return Task.FromResult<(SnackTicket? Ticket, string? Error)>((submitted, null));
        }
    }

    private static string GetBudgetKey(string officeId, DateTimeOffset date) =>
        $"{officeId}:{date.Year:D4}-{date.Month:D2}";
}

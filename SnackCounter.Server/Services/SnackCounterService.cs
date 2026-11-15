using System.Collections.Concurrent;
using SnackCounter.Server.Application;
using SnackCounter.Server.Models;

namespace SnackCounter.Server.Services;

public sealed class SnackCounterService : IReplenishmentService
{
    private readonly ConcurrentDictionary<Guid, SnackTicket> _tickets = new();
    private readonly ConcurrentDictionary<string, int> _volunteerOpenTickets = new();
    private readonly IReadOnlyDictionary<string, Office> _offices = new Dictionary<string, Office>(StringComparer.OrdinalIgnoreCase)
    {
        ["oslo"] = new("oslo", "Oslo", "NOK", 6000m),
        ["bengaluru"] = new("bengaluru", "Bengaluru", "INR", 15000m)
    };
    private readonly object _stateGate = new();

    public IReadOnlyCollection<Office> Offices => _offices.Values.ToArray();

    public IReadOnlyCollection<SnackTicket> GetTickets() =>
        _tickets.Values.OrderByDescending(ticket => ticket.CreatedAt).ToArray();

    public (SnackTicket? Ticket, string? Error) ReportContainer(ReportContainerRequest request)
    {
        if (!request.ContainerEmpty || !request.LocationConfirmed || !request.SnackTypeConfirmed)
            return (null, "Confirm that the container is empty, the location is correct, and the snack type is known.");

        if (!_offices.TryGetValue(request.OfficeId, out var office))
            return (null, "Choose a supported office.");
        if (request.Quantity <= 0 || request.UnitPrice <= 0 || string.IsNullOrWhiteSpace(request.SnackName))
            return (null, "Snack name, quantity, and unit price must be positive.");

        lock (_stateGate)
        {
            var existing = _tickets.Values.FirstOrDefault(ticket =>
                ticket.ContainerId.Equals(request.ContainerId, StringComparison.OrdinalIgnoreCase) &&
                ticket.Status != TicketStatus.Closed);
            if (existing is not null)
                return (existing, "This container already has an active ticket.");

            var monthStart = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var reserved = _tickets.Values
                .Where(ticket => ticket.OfficeId == office.Id && ticket.CreatedAt >= monthStart && ticket.Status != TicketStatus.Closed)
                .Sum(ticket => ticket.ReservedBudget);
            var estimatedCost = request.Quantity * request.UnitPrice;
            if (reserved + estimatedCost > office.MonthlyBudget)
                return (null, $"The {office.Name} monthly budget has no room for another reservation.");

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
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(4),
                null,
                Array.Empty<string>());
            _tickets[ticket.Id] = ticket;
            return (ticket, null);
        }
    }

    public (SnackTicket? Ticket, string? Error) Acknowledge(Guid ticketId, AcknowledgeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VolunteerId))
            return (null, "Volunteer ID is required.");

        lock (_stateGate)
        {
            if (!_tickets.TryGetValue(ticketId, out var ticket))
                return (null, "Ticket not found.");
            if (ticket.Status != TicketStatus.Open)
                return (null, "This ticket is no longer available.");
            if (ticket.AcknowledgementDeadline <= DateTimeOffset.UtcNow)
                return (null, "The acknowledgement window has expired.");
            var openCount = _volunteerOpenTickets.GetValueOrDefault(request.VolunteerId);
            if (openCount >= 2)
                return (null, "Volunteers can have at most two open tickets.");
            if (openCount >= 1 && !request.ReimbursementConsent)
                return (null, "Reimbursement consent is required when you have two or more unpaid claims.");

            var acknowledged = ticket with
            {
                Status = TicketStatus.Acknowledged,
                VolunteerId = request.VolunteerId.Trim()
            };
            _tickets[ticketId] = acknowledged;
            _volunteerOpenTickets.AddOrUpdate(request.VolunteerId, 1, (_, value) => value + 1);
            return (acknowledged, null);
        }
    }

    public (SnackTicket? Ticket, string? Error) SubmitProof(Guid ticketId, ProofRequest request)
    {
        if (request.Amount <= 0 || request.Items.Count == 0 ||
            string.IsNullOrWhiteSpace(request.ReceiptFileName) ||
            string.IsNullOrWhiteSpace(request.CounterPhotoFileName))
            return (null, "Amount, at least one item, receipt, and counter photo are required.");

        lock (_stateGate)
        {
            if (!_tickets.TryGetValue(ticketId, out var ticket))
                return (null, "Ticket not found.");
            if (ticket.Status != TicketStatus.Acknowledged || ticket.VolunteerId != request.VolunteerId)
                return (null, "Only the assigned volunteer can submit proof for an acknowledged ticket.");

            var submitted = ticket with
            {
                Status = TicketStatus.Submitted,
                SpentAmount = request.Amount,
                Items = request.Items.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToArray()
            };
            _tickets[ticketId] = submitted;
            _volunteerOpenTickets.AddOrUpdate(request.VolunteerId, 0, (_, value) => Math.Max(0, value - 1));
            return (submitted, null);
        }
    }

    public MonthlyReport GetMonthlyReport(DateOnly month)
    {
        var monthStart = new DateTimeOffset(month.Year, month.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var nextMonth = monthStart.AddMonths(1);
        var tickets = _tickets.Values.Where(ticket => ticket.CreatedAt >= monthStart && ticket.CreatedAt < nextMonth).ToArray();
        var summaries = _offices.Values.Select(office =>
        {
            var officeTickets = tickets.Where(ticket => ticket.OfficeId == office.Id).ToArray();
            return new OfficeSummary(
                office.Id,
                office.Name,
                office.Currency,
                office.MonthlyBudget,
                officeTickets.Sum(ticket => ticket.ReservedBudget),
                officeTickets.Sum(ticket => ticket.SpentAmount ?? 0),
                officeTickets.Count(ticket => ticket.Status is TicketStatus.Open or TicketStatus.Acknowledged),
                officeTickets.Count(ticket => ticket.Status == TicketStatus.Submitted || ticket.Status == TicketStatus.Closed));
        }).ToArray();

        var nok = summaries.Single(summary => summary.Currency == "NOK");
        var inr = summaries.Single(summary => summary.Currency == "INR");
        var exchangeRate = 0.13m;
        var topSnacks = tickets.SelectMany(ticket => ticket.Items.Select(item => new { item, ticket.SpentAmount, ticket.Currency }))
            .GroupBy(item => new { item.item, item.Currency })
            .Select(group => new SnackSummary(group.Key.item, group.Count(), group.Sum(item => item.SpentAmount ?? 0), group.Key.Currency))
            .OrderByDescending(item => item.Quantity)
            .ThenByDescending(item => item.Spend)
            .Take(5)
            .ToArray();

        return new MonthlyReport(month.ToString("yyyy-MM"), nok.Spent + (inr.Spent * exchangeRate), summaries, topSnacks);
    }
}

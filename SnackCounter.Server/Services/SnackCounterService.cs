using SnackCounter.Server.Application;
using SnackCounter.Server.Models;

namespace SnackCounter.Server.Services;

public sealed class SnackCounterService(IReplenishmentRepository repository) : IReplenishmentService
{
    public Task<IReadOnlyCollection<Office>> GetOfficesAsync(CancellationToken cancellationToken = default) =>
        repository.GetOfficesAsync(cancellationToken);

    public Task<IReadOnlyCollection<SnackTicket>> GetTicketsAsync(CancellationToken cancellationToken = default) =>
        repository.GetTicketsAsync(cancellationToken);

    public Task<ReimbursementClaim?> GetClaimAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default) =>
        repository.GetClaimAsync(ticketId, cancellationToken);

    public async Task<(SnackTicket? Ticket, string? Error)> ReportContainerAsync(
        ReportContainerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.ContainerEmpty || !request.LocationConfirmed || !request.SnackTypeConfirmed)
            return (null, "Confirm that the container is empty, the location is correct, and the snack type is known.");
        var offices = await repository.GetOfficesAsync(cancellationToken);
        if (offices.FirstOrDefault(office => office.Id.Equals(request.OfficeId, StringComparison.OrdinalIgnoreCase)) is not { } office)
            return (null, "Choose a supported office.");
        if (request.Quantity <= 0 || request.UnitPrice <= 0 || string.IsNullOrWhiteSpace(request.SnackName))
            return (null, "Snack name, quantity, and unit price must be positive.");

        return await repository.CreateTicketAsync(
            office,
            request,
            request.Quantity * request.UnitPrice,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public async Task<(SnackTicket? Ticket, string? Error)> AcknowledgeAsync(
        Guid ticketId,
        AcknowledgeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.VolunteerId))
            return (null, "Volunteer ID is required.");

        return await repository.AcknowledgeAsync(
            ticketId,
            request.VolunteerId.Trim(),
            request.ReimbursementConsent,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public async Task<(SnackTicket? Ticket, string? Error)> SubmitProofAsync(
        Guid ticketId,
        ProofRequest request,
        CancellationToken cancellationToken = default)
    {
        var items = request.Items
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .ToArray();
        if (request.Amount <= 0 || items.Length == 0 ||
            string.IsNullOrWhiteSpace(request.ReceiptFileName) ||
            string.IsNullOrWhiteSpace(request.CounterPhotoFileName))
            return (null, "Amount, at least one item, receipt, and counter photo are required.");

        return await repository.SubmitProofAsync(
            ticketId,
            request with { Items = items, VolunteerId = request.VolunteerId.Trim() },
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public async Task<MonthlyReport> GetMonthlyReportAsync(
        DateOnly month,
        CancellationToken cancellationToken = default)
    {
        var monthStart = new DateTimeOffset(month.Year, month.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var nextMonth = monthStart.AddMonths(1);
        var tickets = (await repository.GetTicketsAsync(cancellationToken))
            .Where(ticket => ticket.CreatedAt >= monthStart && ticket.CreatedAt < nextMonth)
            .ToArray();
        var summaries = (await repository.GetOfficesAsync(cancellationToken)).Select(office =>
        {
            var officeTickets = tickets.Where(ticket => ticket.OfficeId == office.Id).ToArray();
            return new OfficeSummary(
                office.Id,
                office.Name,
                office.Currency,
                office.MonthlyBudget,
                officeTickets.Where(ticket => ticket.Status is TicketStatus.Open or TicketStatus.Acknowledged)
                    .Sum(ticket => ticket.ReservedBudget),
                officeTickets.Sum(ticket => ticket.SpentAmount ?? 0),
                officeTickets.Count(ticket => ticket.Status is TicketStatus.Open or TicketStatus.Acknowledged),
                officeTickets.Count(ticket => ticket.Status is TicketStatus.Submitted or TicketStatus.Closed));
        }).ToArray();

        var nok = summaries.Single(summary => summary.Currency == "NOK");
        var inr = summaries.Single(summary => summary.Currency == "INR");
        var topSnacks = tickets.SelectMany(ticket => ticket.Items.Select(item => new { item, ticket.SpentAmount, ticket.Currency }))
            .GroupBy(item => new { item.item, item.Currency })
            .Select(group => new SnackSummary(group.Key.item, group.Count(), group.Sum(item => item.SpentAmount ?? 0), group.Key.Currency))
            .OrderByDescending(item => item.Quantity)
            .ThenByDescending(item => item.Spend)
            .Take(5)
            .ToArray();

        return new MonthlyReport(month.ToString("yyyy-MM"), nok.Spent + inr.Spent * 0.13m, summaries, topSnacks);
    }
}

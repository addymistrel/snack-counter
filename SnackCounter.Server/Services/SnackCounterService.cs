using SnackCounter.Server.Application;
using SnackCounter.Server.Models;

namespace SnackCounter.Server.Services;

public sealed class SnackCounterService(IReplenishmentRepository repository) : IReplenishmentService
{
    // Locked monthly prototype exchange rate (1 INR = 0.13 NOK)
    public const decimal InrToNokRate = 0.13m;

    public async Task<DashboardData> GetDashboardDataAsync(CancellationToken cancellationToken = default)
    {
        var offices = await repository.GetOfficesAsync(cancellationToken);
        var containers = await repository.GetContainersAsync(cancellationToken);
        var volunteers = await repository.GetVolunteersAsync(cancellationToken);
        var tickets = await repository.GetTicketsAsync(cancellationToken);
        var notifications = await repository.GetNotificationsAsync(cancellationToken);
        var claims = await repository.GetClaimsAsync(cancellationToken);
        var isHRDown = await repository.IsHRDownAsync(cancellationToken);

        return new DashboardData(offices, containers, volunteers, tickets, notifications, claims, isHRDown);
    }

    public Task<IReadOnlyCollection<Office>> GetOfficesAsync(CancellationToken cancellationToken = default) =>
        repository.GetOfficesAsync(cancellationToken);

    public Task<IReadOnlyCollection<ContainerCatalogItem>> GetContainersAsync(CancellationToken cancellationToken = default) =>
        repository.GetContainersAsync(cancellationToken);

    public Task<IReadOnlyCollection<Volunteer>> GetVolunteersAsync(CancellationToken cancellationToken = default) =>
        repository.GetVolunteersAsync(cancellationToken);

    public Task<IReadOnlyCollection<SnackTicket>> GetTicketsAsync(CancellationToken cancellationToken = default) =>
        repository.GetTicketsAsync(cancellationToken);

    public Task<IReadOnlyCollection<NotificationMessage>> GetNotificationsAsync(CancellationToken cancellationToken = default) =>
        repository.GetNotificationsAsync(cancellationToken);

    public Task<IReadOnlyCollection<ReimbursementClaim>> GetClaimsAsync(CancellationToken cancellationToken = default) =>
        repository.GetClaimsAsync(cancellationToken);

    public Task<ReimbursementClaim?> GetClaimAsync(Guid ticketId, CancellationToken cancellationToken = default) =>
        repository.GetClaimAsync(ticketId, cancellationToken);

    public Task<bool> ToggleHRDownAsync(CancellationToken cancellationToken = default) =>
        repository.ToggleHRDownAsync(cancellationToken);

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

    public async Task<(SnackTicket? Ticket, string? Error)> TransferTicketAsync(
        Guid ticketId,
        TransferTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FromVolunteerId) || string.IsNullOrWhiteSpace(request.ToVolunteerId))
            return (null, "Both sender and recipient volunteer IDs are required.");

        if (string.Equals(request.FromVolunteerId.Trim(), request.ToVolunteerId.Trim(), StringComparison.OrdinalIgnoreCase))
            return (null, "Cannot transfer a ticket to yourself.");

        return await repository.TransferTicketAsync(
            ticketId,
            request.FromVolunteerId.Trim(),
            request.ToVolunteerId.Trim(),
            request.ReimbursementConsent,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public Task<(SnackTicket? Ticket, string? Error)> SimulateAutoPickAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default) =>
        repository.SimulateAutoPickAsync(ticketId, DateTimeOffset.UtcNow, cancellationToken);

    public Task<(SnackTicket? Ticket, string? Error)> SimulateExpiryAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default) =>
        repository.SimulateExpiryAsync(ticketId, DateTimeOffset.UtcNow, cancellationToken);

    public Task<(SnackTicket? Ticket, string? Error)> ApproveBudgetAsync(
        Guid ticketId,
        BudgetApprovalRequest request,
        CancellationToken cancellationToken = default) =>
        repository.ApproveBudgetAsync(ticketId, request.Approved, request.AdminNotes, DateTimeOffset.UtcNow, cancellationToken);

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

    public async Task<(SnackTicket? Ticket, ReimbursementClaim? Claim, string? Error)> ReportAlreadyRefilledAsync(
        AlreadyRefilledRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ContainerId) || string.IsNullOrWhiteSpace(request.OfficeId))
            return (null, null, "Container and office are required.");

        if (request.Amount <= 0)
            return (null, null, "Actual purchase amount must be positive.");

        return await repository.ReportAlreadyRefilledAsync(
            request,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public Task<int> RetryPendingClaimsAsync(CancellationToken cancellationToken = default) =>
        repository.RetryPendingClaimsAsync(DateTimeOffset.UtcNow, cancellationToken);

    public Task<int> RunNightlySyncAsync(CancellationToken cancellationToken = default) =>
        repository.RunNightlySyncAsync(DateTimeOffset.UtcNow, cancellationToken);

    public Task ResetDemoDataAsync(CancellationToken cancellationToken = default) =>
        repository.ResetDemoDataAsync(cancellationToken);

    public async Task<MonthlyReport> GetMonthlyReportAsync(
        DateOnly month,
        CancellationToken cancellationToken = default)
    {
        var monthStart = new DateTimeOffset(month.Year, month.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var nextMonth = monthStart.AddMonths(1);

        var tickets = (await repository.GetTicketsAsync(cancellationToken))
            .Where(ticket => ticket.CreatedAt >= monthStart && ticket.CreatedAt < nextMonth)
            .ToArray();

        var offices = await repository.GetOfficesAsync(cancellationToken);

        var summaries = offices.Select(office =>
        {
            var officeTickets = tickets.Where(ticket => ticket.OfficeId == office.Id).ToArray();
            var openStatuses = new[] { TicketStatus.Open, TicketStatus.Acknowledged };
            var closedStatuses = new[] { TicketStatus.Submitted, TicketStatus.Closed };

            return new OfficeSummary(
                office.Id,
                office.Name,
                office.Currency,
                office.MonthlyBudget,
                officeTickets.Where(ticket => openStatuses.Contains(ticket.Status)).Sum(ticket => ticket.ReservedBudget),
                officeTickets.Sum(ticket => ticket.SpentAmount ?? 0),
                officeTickets.Count(ticket => openStatuses.Contains(ticket.Status)),
                officeTickets.Count(ticket => closedStatuses.Contains(ticket.Status)));
        }).ToArray();

        var nokSummary = summaries.FirstOrDefault(s => s.Currency == "NOK");
        var inrSummary = summaries.FirstOrDefault(s => s.Currency == "INR");

        var nokSpent = nokSummary?.Spent ?? 0m;
        var inrSpent = inrSummary?.Spent ?? 0m;

        // Consolidated NOK total using locked monthly rate
        var consolidatedNok = nokSpent + (inrSpent * InrToNokRate);

        var topSnacks = tickets
            .Where(t => t.SpentAmount.HasValue && t.SpentAmount.Value > 0)
            .SelectMany(ticket => ticket.Items.Select(item => new { item, ticket.SpentAmount, ticket.Currency, ticket.Quantity }))
            .GroupBy(item => new { item.item, item.Currency })
            .Select(group => new SnackSummary(
                group.Key.item,
                group.Sum(item => item.Quantity > 0 ? item.Quantity : 1),
                group.Sum(item => item.SpentAmount ?? 0),
                group.Key.Currency))
            .OrderByDescending(item => item.Spend)
            .ThenByDescending(item => item.Quantity)
            .Take(5)
            .ToArray();

        return new MonthlyReport(
            month.ToString("yyyy-MM"),
            consolidatedNok,
            InrToNokRate,
            summaries,
            topSnacks);
    }
}

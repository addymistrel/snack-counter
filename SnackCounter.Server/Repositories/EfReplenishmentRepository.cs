using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Server.Application;
using SnackCounter.Server.Infrastructure.Database;
using SnackCounter.Server.Infrastructure.Database.Models;
using SnackCounter.Server.Models;

namespace SnackCounter.Server.Repositories;

public sealed class EfReplenishmentRepository(SnackCounterDbContext db) : IReplenishmentRepository
{
    private static readonly InMemoryReplenishmentRepository Fallback = new();

    public async Task<IReadOnlyCollection<Office>> GetOfficesAsync(
        CancellationToken cancellationToken = default)
    {
        var offices = await db.Offices.AsNoTracking().OrderBy(office => office.Name)
            .ToListAsync(cancellationToken);
        return offices.Select(ToOffice).ToArray();
    }

    public Task<IReadOnlyCollection<ContainerCatalogItem>> GetContainersAsync(CancellationToken cancellationToken = default) =>
        Fallback.GetContainersAsync(cancellationToken);

    public Task<IReadOnlyCollection<Volunteer>> GetVolunteersAsync(CancellationToken cancellationToken = default) =>
        Fallback.GetVolunteersAsync(cancellationToken);

    public async Task<IReadOnlyCollection<SnackTicket>> GetTicketsAsync(
        CancellationToken cancellationToken = default)
    {
        var tickets = await db.SnackTickets.AsNoTracking().Include(ticket => ticket.Office)
            .OrderByDescending(ticket => ticket.CreatedAt).ToListAsync(cancellationToken);
        return tickets.Select(ToTicket).ToArray();
    }

    public Task<IReadOnlyCollection<NotificationMessage>> GetNotificationsAsync(CancellationToken cancellationToken = default) =>
        Fallback.GetNotificationsAsync(cancellationToken);

    public Task<IReadOnlyCollection<ReimbursementClaim>> GetClaimsAsync(CancellationToken cancellationToken = default) =>
        Fallback.GetClaimsAsync(cancellationToken);

    public async Task<ReimbursementClaim?> GetClaimAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var claim = await db.ReimbursementClaims.AsNoTracking()
            .SingleOrDefaultAsync(claim => claim.TicketId == ticketId, cancellationToken);
        return claim is null ? null : ToClaim(claim);
    }

    public Task<bool> IsHRDownAsync(CancellationToken cancellationToken = default) =>
        Fallback.IsHRDownAsync(cancellationToken);

    public Task<bool> ToggleHRDownAsync(CancellationToken cancellationToken = default) =>
        Fallback.ToggleHRDownAsync(cancellationToken);

    public async Task<(SnackTicket? Ticket, string? Error)> CreateTicketAsync(
        Office office,
        ReportContainerRequest request,
        decimal estimatedCost,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var existing = await db.SnackTickets.AsNoTracking().Include(ticket => ticket.Office)
            .SingleOrDefaultAsync(ticket =>
                ticket.ContainerId == request.ContainerId &&
                ticket.Status != nameof(TicketStatus.Closed) &&
                ticket.Status != nameof(TicketStatus.Cancelled),
                cancellationToken);
        if (existing is not null)
            return (ToTicket(existing), "Refill already in progress.");

        var period = db.OfficeBudgetPeriods.SingleOrDefault(item =>
            item.OfficeId == office.Id && item.Year == now.Year && item.Month == now.Month);
        if (period is null)
        {
            period = new OfficeBudgetPeriodEntity
            {
                OfficeId = office.Id,
                Year = now.Year,
                Month = now.Month,
                LimitAmount = office.MonthlyBudget,
                Currency = office.Currency
            };
            db.OfficeBudgetPeriods.Add(period);
            await db.SaveChangesAsync(cancellationToken);
        }

        var exceedsBudget = (period.Spent + period.Reserved + estimatedCost) > period.LimitAmount;
        var status = exceedsBudget ? nameof(TicketStatus.AwaitingBudgetApproval) : nameof(TicketStatus.Open);
        var reserved = exceedsBudget ? 0m : estimatedCost;

        if (!exceedsBudget)
        {
            period.Reserved += estimatedCost;
        }

        var entity = new SnackTicketEntity
        {
            Id = Guid.NewGuid(),
            ContainerId = request.ContainerId.Trim(),
            SnackName = request.SnackName.Trim(),
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            OfficeId = office.Id,
            Currency = office.Currency,
            ReservedBudget = estimatedCost,
            Status = status,
            CreatedAt = now,
            AcknowledgementDeadline = now.AddHours(4),
            ItemsJson = "[]"
        };
        db.SnackTickets.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (ToTicket(entity), null);
    }

    public async Task<(SnackTicket? Ticket, string? Error)> AcknowledgeAsync(
        Guid ticketId,
        string volunteerId,
        bool reimbursementConsent,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var ticket = await db.SnackTickets.Include(item => item.Office)
            .SingleOrDefaultAsync(item => item.Id == ticketId, cancellationToken);
        if (ticket is null)
            return (null, "Ticket not found.");
        if (ticket.Status != nameof(TicketStatus.Open))
            return (null, "This ticket is no longer available.");
        if (ticket.AcknowledgementDeadline <= now)
            return (null, "The acknowledgement window has expired.");

        var openCount = await db.SnackTickets.CountAsync(item =>
            item.VolunteerId == volunteerId &&
            item.Status == nameof(TicketStatus.Acknowledged), cancellationToken);
        if (openCount >= 2)
            return (null, "Volunteers can have at most two open tickets.");
        if (openCount >= 1 && !reimbursementConsent)
            return (null, "Reimbursement consent is required: Your earlier claims are still being reimbursed. Another purchase will tie up more of your money. Continue?");

        ticket.Status = nameof(TicketStatus.Acknowledged);
        ticket.VolunteerId = volunteerId;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (ToTicket(ticket), null);
    }

    public Task<(SnackTicket? Ticket, string? Error)> TransferTicketAsync(
        Guid ticketId,
        string fromVolunteerId,
        string toVolunteerId,
        bool reimbursementConsent,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        Fallback.TransferTicketAsync(ticketId, fromVolunteerId, toVolunteerId, reimbursementConsent, now, cancellationToken);

    public Task<(SnackTicket? Ticket, string? Error)> SimulateAutoPickAsync(
        Guid ticketId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        Fallback.SimulateAutoPickAsync(ticketId, now, cancellationToken);

    public Task<(SnackTicket? Ticket, string? Error)> SimulateExpiryAsync(
        Guid ticketId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        Fallback.SimulateExpiryAsync(ticketId, now, cancellationToken);

    public Task<(SnackTicket? Ticket, string? Error)> ApproveBudgetAsync(
        Guid ticketId,
        bool approved,
        string? adminNotes,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        Fallback.ApproveBudgetAsync(ticketId, approved, adminNotes, now, cancellationToken);

    public async Task<(SnackTicket? Ticket, string? Error)> SubmitProofAsync(
        Guid ticketId,
        ProofRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var ticket = await db.SnackTickets.Include(item => item.Office)
            .SingleOrDefaultAsync(item => item.Id == ticketId, cancellationToken);
        if (ticket is null)
            return (null, "Ticket not found.");
        if (ticket.Status != nameof(TicketStatus.Acknowledged) || ticket.VolunteerId != request.VolunteerId)
            return (null, "Only the assigned volunteer can submit proof for an acknowledged ticket.");

        var period = await db.OfficeBudgetPeriods.SingleAsync(item =>
            item.OfficeId == ticket.OfficeId && item.Year == ticket.CreatedAt.Year &&
            item.Month == ticket.CreatedAt.Month, cancellationToken);
        period.Reserved -= ticket.ReservedBudget;
        period.Spent += request.Amount;
        ticket.Status = nameof(TicketStatus.Submitted);
        ticket.SpentAmount = request.Amount;
        ticket.ItemsJson = JsonSerializer.Serialize(request.Items);
        ticket.ProofExceedsEstimate = request.Amount > ticket.ReservedBudget;
        db.ReimbursementClaims.Add(new ReimbursementClaimEntity
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ReferenceId = $"SNACK-{ticket.Id:N}",
            VolunteerId = request.VolunteerId,
            Amount = request.Amount,
            Currency = ticket.Currency,
            Status = nameof(ClaimStatus.Submitted),
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (ToTicket(ticket), null);
    }

    public Task<(SnackTicket? Ticket, ReimbursementClaim? Claim, string? Error)> ReportAlreadyRefilledAsync(
        AlreadyRefilledRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        Fallback.ReportAlreadyRefilledAsync(request, now, cancellationToken);

    public Task<int> RetryPendingClaimsAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        Fallback.RetryPendingClaimsAsync(now, cancellationToken);

    public Task<int> RunNightlySyncAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        Fallback.RunNightlySyncAsync(now, cancellationToken);

    public Task ResetDemoDataAsync(CancellationToken cancellationToken = default) =>
        Fallback.ResetDemoDataAsync(cancellationToken);

    private static Office ToOffice(OfficeEntity office) =>
        new(office.Id, office.Name, office.Currency, office.MonthlyBudget);

    private static SnackTicket ToTicket(SnackTicketEntity ticket) =>
        new(ticket.Id, ticket.ContainerId, "Main Pantry", ticket.SnackName, ticket.Quantity, ticket.UnitPrice,
            ticket.OfficeId, ticket.Office?.Name ?? string.Empty, ticket.Currency, ticket.ReservedBudget,
            Enum.Parse<TicketStatus>(ticket.Status), ticket.VolunteerId, ticket.VolunteerId, ticket.CreatedAt,
            ticket.AcknowledgementDeadline, null, ticket.SpentAmount,
            JsonSerializer.Deserialize<IReadOnlyList<string>>(ticket.ItemsJson) ?? Array.Empty<string>(),
            null, null,
            ticket.ProofExceedsEstimate);

    private static ReimbursementClaim ToClaim(ReimbursementClaimEntity claim) =>
        new(claim.Id, claim.TicketId, claim.ReferenceId, claim.VolunteerId, claim.VolunteerId, claim.Amount,
            claim.Currency, Enum.Parse<ClaimStatus>(claim.Status), claim.CreatedAt);
}

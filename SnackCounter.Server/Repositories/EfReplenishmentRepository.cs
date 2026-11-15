using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Server.Application;
using SnackCounter.Server.Infrastructure.Database;
using SnackCounter.Server.Infrastructure.Database.Models;
using SnackCounter.Server.Models;

namespace SnackCounter.Server.Repositories;

public sealed class EfReplenishmentRepository(SnackCounterDbContext db) : IReplenishmentRepository
{
    public async Task<IReadOnlyCollection<Office>> GetOfficesAsync(
        CancellationToken cancellationToken = default)
    {
        var offices = await db.Offices.AsNoTracking().OrderBy(office => office.Name)
            .ToListAsync(cancellationToken);
        return offices.Select(ToOffice).ToArray();
    }

    public async Task<IReadOnlyCollection<SnackTicket>> GetTicketsAsync(
        CancellationToken cancellationToken = default)
    {
        var tickets = await db.SnackTickets.AsNoTracking().Include(ticket => ticket.Office)
            .OrderByDescending(ticket => ticket.CreatedAt).ToListAsync(cancellationToken);
        return tickets.Select(ToTicket).ToArray();
    }

    public async Task<ReimbursementClaim?> GetClaimAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var claim = await db.ReimbursementClaims.AsNoTracking()
            .SingleOrDefaultAsync(claim => claim.TicketId == ticketId, cancellationToken);
        return claim is null ? null : ToClaim(claim);
    }

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
                ticket.ContainerId == request.ContainerId && ticket.Status != nameof(TicketStatus.Closed),
                cancellationToken);
        if (existing is not null)
            return (ToTicket(existing), "This container already has an active ticket.");

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

        if (period.Spent + period.Reserved + estimatedCost > period.LimitAmount)
            return (null, $"The {office.Name} monthly budget has no room for another reservation.");

        period.Reserved += estimatedCost;
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
            Status = nameof(TicketStatus.Open),
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
            return (null, "Reimbursement consent is required when you have two or more unpaid claims.");

        ticket.Status = nameof(TicketStatus.Acknowledged);
        ticket.VolunteerId = volunteerId;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (ToTicket(ticket), null);
    }

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

    private static Office ToOffice(OfficeEntity office) =>
        new(office.Id, office.Name, office.Currency, office.MonthlyBudget);

    private static SnackTicket ToTicket(SnackTicketEntity ticket) =>
        new(ticket.Id, ticket.ContainerId, ticket.SnackName, ticket.Quantity, ticket.UnitPrice,
            ticket.OfficeId, ticket.Office?.Name ?? string.Empty, ticket.Currency, ticket.ReservedBudget,
            Enum.Parse<TicketStatus>(ticket.Status), ticket.VolunteerId, ticket.CreatedAt,
            ticket.AcknowledgementDeadline, ticket.SpentAmount,
            JsonSerializer.Deserialize<IReadOnlyList<string>>(ticket.ItemsJson) ?? Array.Empty<string>(),
            ticket.ProofExceedsEstimate);

    private static ReimbursementClaim ToClaim(ReimbursementClaimEntity claim) =>
        new(claim.Id, claim.TicketId, claim.ReferenceId, claim.VolunteerId, claim.Amount,
            claim.Currency, Enum.Parse<ClaimStatus>(claim.Status), claim.CreatedAt);
}

using System.Collections.Concurrent;
using SnackCounter.Server.Application;
using SnackCounter.Server.Models;

namespace SnackCounter.Server.Repositories;

public sealed class InMemoryReplenishmentRepository : IReplenishmentRepository
{
    private readonly ConcurrentDictionary<Guid, SnackTicket> _tickets = new();
    private readonly ConcurrentDictionary<Guid, ReimbursementClaim> _claims = new();
    private readonly ConcurrentDictionary<string, Volunteer> _volunteers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<NotificationMessage> _notifications = new();
    private readonly Dictionary<string, decimal> _monthlyReserved = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, decimal> _monthlySpent = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _stateGate = new();
    private bool _isHRDown;

    private readonly IReadOnlyDictionary<string, Office> _offices = new Dictionary<string, Office>(StringComparer.OrdinalIgnoreCase)
    {
        ["oslo"] = new("oslo", "Oslo", "NOK", 6000m),
        ["bengaluru"] = new("bengaluru", "Bengaluru", "INR", 15000m)
    };

    private readonly IReadOnlyList<ContainerCatalogItem> _containers = new List<ContainerCatalogItem>
    {
        new("pantry-oslo-01", "oslo", "Floor 3 Main Pantry", "Salted Roasted Almonds", 10, 45m),
        new("pantry-oslo-02", "oslo", "Floor 3 Main Pantry", "Dark Chocolate 70%", 15, 30m),
        new("pantry-oslo-03", "oslo", "Floor 4 Tech Hub", "Protein Oat Bars", 12, 35m),
        new("pantry-oslo-04", "oslo", "Floor 4 Tech Hub", "Sparkling Berry Water", 20, 20m),
        new("pantry-blr-01", "bengaluru", "Ground Floor Cafeteria", "Masala Chai Biscuits", 25, 40m),
        new("pantry-blr-02", "bengaluru", "Ground Floor Cafeteria", "Roasted Spiced Peanuts", 20, 60m),
        new("pantry-blr-03", "bengaluru", "2nd Floor Snack Bar", "Energy Granola Bars", 20, 80m),
        new("pantry-blr-04", "bengaluru", "2nd Floor Snack Bar", "Kerala Banana Chips", 15, 70m),
    };

    public InMemoryReplenishmentRepository()
    {
        SeedInitialState();
    }

    private void SeedInitialState()
    {
        lock (_stateGate)
        {
            _tickets.Clear();
            _claims.Clear();
            _volunteers.Clear();
            _notifications.Clear();
            _monthlyReserved.Clear();
            _monthlySpent.Clear();
            _isHRDown = false;

            // Seed Volunteers (accounting for hybrid, on leave, visiting other office, debt status)
            var initialVolunteers = new List<Volunteer>
            {
                // Oslo volunteers (~15 in office)
                new("astrid", "Astrid Lind", "astrid@company.com", "oslo", "oslo", "NOK", true, false, 0, 2, null),
                new("lars", "Lars Holm", "lars@company.com", "oslo", "oslo", "NOK", true, false, 0, 0, DateTimeOffset.UtcNow.AddDays(-5)),
                new("ingrid", "Ingrid Solberg", "ingrid@company.com", "oslo", "oslo", "NOK", true, false, 0, 1, DateTimeOffset.UtcNow.AddDays(-2)),
                new("sven", "Sven Hansen", "sven@company.com", "oslo", "oslo", "NOK", true, false, 0, 0, DateTimeOffset.UtcNow.AddDays(-7)),
                new("freja", "Freja Bakke", "freja@company.com", "oslo", "oslo", "NOK", true, true, 0, 0, null), // On leave
                new("henrik", "Henrik Lie", "henrik@company.com", "oslo", "bengaluru", "NOK", true, false, 0, 0, null), // Visiting BLR!

                // Bengaluru volunteers (~20 in office)
                new("priya", "Priya Sharma", "priya@company.com", "bengaluru", "bengaluru", "INR", true, false, 0, 0, DateTimeOffset.UtcNow.AddDays(-4)),
                new("rahul", "Rahul Verma", "rahul@company.com", "bengaluru", "bengaluru", "INR", true, false, 0, 2, null),
                new("ananya", "Ananya Iyer", "ananya@company.com", "bengaluru", "bengaluru", "INR", true, false, 0, 0, DateTimeOffset.UtcNow.AddDays(-6)),
                new("vikram", "Vikram Patel", "vikram@company.com", "bengaluru", "bengaluru", "INR", true, false, 0, 1, DateTimeOffset.UtcNow.AddDays(-1)),
                new("sneha", "Sneha Reddy", "sneha@company.com", "bengaluru", "bengaluru", "INR", false, false, 0, 0, null), // Paused
                new("arjun", "Arjun Rao", "arjun@company.com", "bengaluru", "oslo", "INR", true, false, 0, 0, null), // Visiting Oslo!
            };

            foreach (var v in initialVolunteers)
            {
                _volunteers[v.Id] = v;
            }
        }
    }

    public Task<IReadOnlyCollection<Office>> GetOfficesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Office>>(_offices.Values.ToArray());

    public Task<IReadOnlyCollection<ContainerCatalogItem>> GetContainersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<ContainerCatalogItem>>(_containers);

    public Task<IReadOnlyCollection<Volunteer>> GetVolunteersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Volunteer>>(_volunteers.Values.OrderBy(v => v.Name).ToArray());

    public Task<IReadOnlyCollection<SnackTicket>> GetTicketsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<SnackTicket>>(
            _tickets.Values.OrderByDescending(ticket => ticket.CreatedAt).ToArray());

    public Task<IReadOnlyCollection<NotificationMessage>> GetNotificationsAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            return Task.FromResult<IReadOnlyCollection<NotificationMessage>>(_notifications.OrderByDescending(n => n.Timestamp).ToList());
        }
    }

    public Task<IReadOnlyCollection<ReimbursementClaim>> GetClaimsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<ReimbursementClaim>>(
            _claims.Values.OrderByDescending(c => c.CreatedAt).ToArray());

    public Task<ReimbursementClaim?> GetClaimAsync(Guid ticketId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_claims.Values.FirstOrDefault(c => c.TicketId == ticketId));

    public Task<bool> IsHRDownAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_isHRDown);

    public Task<bool> ToggleHRDownAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            _isHRDown = !_isHRDown;
            AddNotification(
                "system",
                "Internal Chat",
                "admin",
                "Office Admin",
                _isHRDown ? "HR System Simulation: OFFLINE" : "HR System Simulation: ONLINE",
                _isHRDown ? "HR claim creation will queue as PendingRetry until retry is triggered." : "HR system is operational.",
                "/hr",
                DateTimeOffset.UtcNow);
            return Task.FromResult(_isHRDown);
        }
    }

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
                ticket.Status != TicketStatus.Closed &&
                ticket.Status != TicketStatus.Cancelled);

            if (existing is not null)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>(
                    (existing, "Refill already in progress."));

            var budgetKey = GetBudgetKey(office.Id, now);
            var reserved = _monthlyReserved.GetValueOrDefault(budgetKey);
            var spent = _monthlySpent.GetValueOrDefault(budgetKey);

            var needsBudgetApproval = (spent + reserved + estimatedCost) > office.MonthlyBudget;

            var ticketId = Guid.NewGuid();
            var status = needsBudgetApproval ? TicketStatus.AwaitingBudgetApproval : TicketStatus.Open;
            var reservedAmount = needsBudgetApproval ? 0m : estimatedCost;

            if (!needsBudgetApproval)
            {
                _monthlyReserved[budgetKey] = reserved + estimatedCost;
            }

            var ticket = new SnackTicket(
                ticketId,
                request.ContainerId.Trim(),
                request.CounterName.Trim(),
                request.SnackName.Trim(),
                request.Quantity,
                request.UnitPrice,
                office.Id,
                office.Name,
                office.Currency,
                reservedAmount,
                status,
                null,
                null,
                now,
                now.AddHours(4),
                null,
                null,
                Array.Empty<string>(),
                null,
                null,
                false,
                false,
                null,
                needsBudgetApproval ? "Awaiting admin budget approval" : null,
                request.ReporterId,
                request.ReporterName);

            _tickets[ticket.Id] = ticket;

            if (needsBudgetApproval)
            {
                AddNotification(
                    office.Id,
                    "Internal Chat",
                    "admin",
                    "Office Admin",
                    $"Budget Approval Required: {ticket.SnackName} in {office.Name}",
                    $"Container {ticket.ContainerId} estimated cost {estimatedCost} {office.Currency} exceeds remaining monthly budget. Approval required.",
                    $"/tickets/{ticket.Id}/budget-approval",
                    now);
            }
            else
            {
                NotifyEligibleVolunteers(office.Id, ticket, now);
            }

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

            if (!_volunteers.TryGetValue(volunteerId, out var volunteer))
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Volunteer not found."));

            if (!volunteer.IsActive || volunteer.IsOnLeave)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Volunteer is not eligible to take tickets."));

            if (volunteer.OpenTicketsCount >= 2)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Volunteers can have at most two open tickets."));

            if (volunteer.UnpaidClaimsCount >= 2 && !reimbursementConsent)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null,
                    "Reimbursement consent is required: Your earlier claims are still being reimbursed. Another purchase will tie up more of your money. Continue?"));

            var acknowledged = ticket with
            {
                Status = TicketStatus.Acknowledged,
                VolunteerId = volunteer.Id,
                VolunteerName = volunteer.Name,
                CompletionDeadline = now.AddHours(48)
            };

            _tickets[ticketId] = acknowledged;
            _volunteers[volunteerId] = volunteer with
            {
                OpenTicketsCount = volunteer.OpenTicketsCount + 1,
                LastAssignedAt = now
            };

            AddNotification(
                ticket.OfficeId,
                "Internal Chat",
                volunteer.Id,
                volunteer.Name,
                $"Ticket Assigned: {ticket.SnackName} ({ticket.ContainerId})",
                $"You are confirmed as the refill owner. You have 48 hours to complete the refill and upload proof.",
                $"/tickets/{ticket.Id}",
                now);

            return Task.FromResult<(SnackTicket? Ticket, string? Error)>((acknowledged, null));
        }
    }

    public Task<(SnackTicket? Ticket, string? Error)> TransferTicketAsync(
        Guid ticketId,
        string fromVolunteerId,
        string toVolunteerId,
        bool reimbursementConsent,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            if (!_tickets.TryGetValue(ticketId, out var ticket))
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Ticket not found."));

            if (ticket.Status != TicketStatus.Acknowledged || ticket.VolunteerId != fromVolunteerId)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Only the assigned owner can transfer this ticket."));

            if (!_volunteers.TryGetValue(toVolunteerId, out var target))
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Target volunteer not found."));

            if (!target.IsActive || target.IsOnLeave || target.CurrentOfficeId != ticket.OfficeId)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Target volunteer is not eligible in this office."));

            if (target.OpenTicketsCount >= 2)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Target volunteer already has two open tickets."));

            if (target.UnpaidClaimsCount >= 2 && !reimbursementConsent)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null,
                    "Reimbursement consent is required: Target volunteer's earlier claims are still being reimbursed. Another purchase will tie up more of their money. Continue?"));

            if (_volunteers.TryGetValue(fromVolunteerId, out var sender))
            {
                _volunteers[fromVolunteerId] = sender with
                {
                    OpenTicketsCount = Math.Max(0, sender.OpenTicketsCount - 1)
                };
            }

            _volunteers[toVolunteerId] = target with
            {
                OpenTicketsCount = target.OpenTicketsCount + 1,
                LastAssignedAt = now
            };

            var transferred = ticket with
            {
                VolunteerId = target.Id,
                VolunteerName = target.Name
                // Deadline stays the same per rule!
            };

            _tickets[ticketId] = transferred;

            AddNotification(
                ticket.OfficeId,
                "Internal Chat",
                target.Id,
                target.Name,
                $"Ticket Transferred: {ticket.SnackName} from {ticket.VolunteerName}",
                $"You have accepted the transfer for container {ticket.ContainerId}. The original deadline remains {ticket.CompletionDeadline:MMM dd HH:mm}.",
                $"/tickets/{ticket.Id}",
                now);

            return Task.FromResult<(SnackTicket? Ticket, string? Error)>((transferred, null));
        }
    }

    public Task<(SnackTicket? Ticket, string? Error)> SimulateAutoPickAsync(
        Guid ticketId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            if (!_tickets.TryGetValue(ticketId, out var ticket))
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Ticket not found."));

            if (ticket.Status != TicketStatus.Open)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Auto-pick can only be run for open tickets."));

            // Filter eligible volunteers: active, not on leave, in office, < 2 open tickets, not expired on this ticket
            var eligible = _volunteers.Values
                .Where(v => v.CurrentOfficeId == ticket.OfficeId &&
                            v.IsActive &&
                            !v.IsOnLeave &&
                            v.OpenTicketsCount < 2 &&
                            !string.Equals(v.Id, ticket.ExpiredVolunteerId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(v => v.OpenTicketsCount)
                .ThenBy(v => v.LastAssignedAt ?? DateTimeOffset.MinValue)
                .ThenBy(v => v.Id)
                .ToList();

            if (eligible.Count == 0)
            {
                AddNotification(
                    ticket.OfficeId,
                    "Email",
                    "admin",
                    "Office Admin",
                    $"ALERT: No volunteers available for auto-pick ({ticket.OfficeName})",
                    $"Auto-pick ran for ticket {ticket.Id} ({ticket.ContainerId}) but no eligible volunteers were available. Ticket remains open.",
                    $"/tickets/{ticket.Id}",
                    now);
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((ticket, "No eligible volunteers available for auto-pick. Admin alerted."));
            }

            // Pick candidate: if 2+ unpaid claims, requires consent
            var pick = eligible.FirstOrDefault(v => v.UnpaidClaimsCount < 2) ?? eligible.First();

            var assigned = ticket with
            {
                Status = TicketStatus.Acknowledged,
                VolunteerId = pick.Id,
                VolunteerName = pick.Name,
                IsAutoAssigned = true,
                CompletionDeadline = now.AddHours(48)
            };

            _tickets[ticketId] = assigned;
            _volunteers[pick.Id] = pick with
            {
                OpenTicketsCount = pick.OpenTicketsCount + 1,
                LastAssignedAt = now
            };

            AddNotification(
                ticket.OfficeId,
                "Internal Chat",
                pick.Id,
                pick.Name,
                $"Auto-Pick Assigned: {ticket.SnackName} ({ticket.ContainerId})",
                $"You were auto-selected to refill container {ticket.ContainerId}. You have 48 hours to complete.",
                $"/tickets/{ticket.Id}",
                now);

            AddNotification(
                ticket.OfficeId,
                "Email",
                pick.Id,
                pick.Name,
                $"Snack Counter Refill Assignment - {ticket.SnackName}",
                $"Hi {pick.Name}, no volunteer claimed ticket {ticket.Id} within 4 hours. You have been assigned based on fair rotation. Refill by {now.AddHours(48):MMM dd HH:mm}.",
                $"/tickets/{ticket.Id}",
                now);

            return Task.FromResult<(SnackTicket? Ticket, string? Error)>((assigned, null));
        }
    }

    public Task<(SnackTicket? Ticket, string? Error)> SimulateExpiryAsync(
        Guid ticketId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            if (!_tickets.TryGetValue(ticketId, out var ticket))
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Ticket not found."));

            if (ticket.Status != TicketStatus.Acknowledged)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Only acknowledged in-progress tickets can expire."));

            var expiredVolunteerId = ticket.VolunteerId;
            if (expiredVolunteerId is not null && _volunteers.TryGetValue(expiredVolunteerId, out var volunteer))
            {
                _volunteers[expiredVolunteerId] = volunteer with
                {
                    OpenTicketsCount = Math.Max(0, volunteer.OpenTicketsCount - 1)
                };

                AddNotification(
                    ticket.OfficeId,
                    "Internal Chat",
                    volunteer.Id,
                    volunteer.Name,
                    $"48h Deadline Expired: {ticket.SnackName}",
                    $"Your 48-hour refill window for container {ticket.ContainerId} has expired. The ticket has been reassigned.",
                    $"/tickets/{ticket.Id}",
                    now);
            }

            var resetTicket = ticket with
            {
                Status = TicketStatus.Open,
                VolunteerId = null,
                VolunteerName = null,
                ExpiredVolunteerId = expiredVolunteerId,
                AcknowledgementDeadline = now.AddHours(4),
                CompletionDeadline = null
            };

            _tickets[ticketId] = resetTicket;

            // Automatically run pick for the next volunteer with fresh 48-hour deadline
            return SimulateAutoPickAsync(ticketId, now, cancellationToken);
        }
    }

    public Task<(SnackTicket? Ticket, string? Error)> ApproveBudgetAsync(
        Guid ticketId,
        bool approved,
        string? adminNotes,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            if (!_tickets.TryGetValue(ticketId, out var ticket))
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Ticket not found."));

            if (ticket.Status != TicketStatus.AwaitingBudgetApproval)
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((null, "Ticket is not awaiting budget approval."));

            var office = _offices[ticket.OfficeId];
            var budgetKey = GetBudgetKey(office.Id, ticket.CreatedAt);

            if (approved)
            {
                var estimatedCost = ticket.Quantity * ticket.UnitPrice;
                _monthlyReserved[budgetKey] = _monthlyReserved.GetValueOrDefault(budgetKey) + estimatedCost;
                var opened = ticket with
                {
                    Status = TicketStatus.Open,
                    ReservedBudget = estimatedCost,
                    AcknowledgementDeadline = now.AddHours(4),
                    ClosedReason = null
                };
                _tickets[ticketId] = opened;
                NotifyEligibleVolunteers(office.Id, opened, now);
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((opened, null));
            }
            else
            {
                var cancelled = ticket with
                {
                    Status = TicketStatus.Cancelled,
                    ClosedReason = $"Declined by Admin: {adminNotes ?? "Budget limit reached"}"
                };
                _tickets[ticketId] = cancelled;
                return Task.FromResult<(SnackTicket? Ticket, string? Error)>((cancelled, null));
            }
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
                ReceiptFileName = request.ReceiptFileName,
                CounterPhotoFileName = request.CounterPhotoFileName,
                ProofExceedsEstimate = request.Amount > ticket.ReservedBudget
            };

            _tickets[ticketId] = submitted;

            var budgetKey = GetBudgetKey(ticket.OfficeId, ticket.CreatedAt);
            _monthlyReserved[budgetKey] = Math.Max(0, _monthlyReserved.GetValueOrDefault(budgetKey) - ticket.ReservedBudget);
            _monthlySpent[budgetKey] = _monthlySpent.GetValueOrDefault(budgetKey) + request.Amount;

            if (_volunteers.TryGetValue(request.VolunteerId, out var volunteer))
            {
                _volunteers[request.VolunteerId] = volunteer with
                {
                    OpenTicketsCount = Math.Max(0, volunteer.OpenTicketsCount - 1),
                    UnpaidClaimsCount = volunteer.UnpaidClaimsCount + 1
                };
            }

            var claimStatus = _isHRDown ? ClaimStatus.PendingRetry : ClaimStatus.Submitted;
            var referenceId = $"SNACK-{ticket.Id:N}";

            var claim = new ReimbursementClaim(
                Guid.NewGuid(),
                ticketId,
                referenceId,
                request.VolunteerId,
                volunteer?.Name ?? request.VolunteerId,
                request.Amount,
                ticket.Currency,
                claimStatus,
                now);

            _claims[ticketId] = claim;

            AddNotification(
                ticket.OfficeId,
                "Email",
                request.VolunteerId,
                volunteer?.Name ?? request.VolunteerId,
                $"Proof Accepted & HR Claim Created ({referenceId})",
                _isHRDown
                    ? $"Proof received ({request.Amount} {ticket.Currency}). HR is temporarily unreachable; claim queued as PendingRetry with ID {referenceId}."
                    : $"Proof received ({request.Amount} {ticket.Currency}). Claim {referenceId} submitted to HR for payroll reimbursement.",
                $"/tickets/{ticket.Id}/claim",
                now);

            return Task.FromResult<(SnackTicket? Ticket, string? Error)>((submitted, null));
        }
    }

    public Task<(SnackTicket? Ticket, ReimbursementClaim? Claim, string? Error)> ReportAlreadyRefilledAsync(
        AlreadyRefilledRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            var office = _offices[request.OfficeId];
            var budgetKey = GetBudgetKey(office.Id, now);

            // Find existing active ticket for this container, if any
            var existing = _tickets.Values.FirstOrDefault(t =>
                t.ContainerId.Equals(request.ContainerId, StringComparison.OrdinalIgnoreCase) &&
                t.Status != TicketStatus.Closed &&
                t.Status != TicketStatus.Cancelled);

            if (existing is not null)
            {
                // Release reservation so budget is accurate
                _monthlyReserved[budgetKey] = Math.Max(0, _monthlyReserved.GetValueOrDefault(budgetKey) - existing.ReservedBudget);

                // If a volunteer was assigned, release their ticket slot, but NEVER credit them with the purchase
                if (existing.VolunteerId is not null && _volunteers.TryGetValue(existing.VolunteerId, out var assignedVolunteer))
                {
                    _volunteers[existing.VolunteerId] = assignedVolunteer with
                    {
                        OpenTicketsCount = Math.Max(0, assignedVolunteer.OpenTicketsCount - 1)
                    };

                    AddNotification(
                        existing.OfficeId,
                        "Internal Chat",
                        assignedVolunteer.Id,
                        assignedVolunteer.Name,
                        $"Ticket Closed: {existing.SnackName} already refilled",
                        $"Container {existing.ContainerId} was already refilled by {request.ReporterName}. No shopping is needed.",
                        $"/tickets/{existing.Id}",
                        now);
                }

                // Close the ticket
                var closedExisting = existing with
                {
                    Status = TicketStatus.Closed,
                    ClosedReason = $"Already refilled by {request.ReporterName} without a ticket",
                    SpentAmount = 0m // Not credited to assigned volunteer
                };
                _tickets[existing.Id] = closedExisting;
            }

            // Real spend from the person who bought it
            _monthlySpent[budgetKey] = _monthlySpent.GetValueOrDefault(budgetKey) + request.Amount;

            var claimId = Guid.NewGuid();
            var refId = $"EXC-{claimId:N}";
            var claim = new ReimbursementClaim(
                claimId,
                existing?.Id ?? Guid.NewGuid(),
                refId,
                request.ReporterId,
                request.ReporterName,
                request.Amount,
                office.Currency,
                ClaimStatus.Submitted,
                now,
                null,
                null,
                true,
                request.ReporterName);

            _claims[claim.TicketId] = claim;

            AddNotification(
                office.Id,
                "Email",
                "admin",
                "Office Admin",
                $"EXCEPTION CLAIM: Already refilled by {request.ReporterName}",
                $"{request.ReporterName} refilled container {request.ContainerId} without a ticket. Claim {refId} for {request.Amount} {office.Currency} submitted to Admin.",
                $"/admin/exceptions/{claimId}",
                now);

            return Task.FromResult<(SnackTicket? Ticket, ReimbursementClaim? Claim, string? Error)>(
                (existing, claim, null));
        }
    }

    public Task<int> RetryPendingClaimsAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            if (_isHRDown)
                return Task.FromResult(0);

            var retriedCount = 0;
            var pendingClaims = _claims.Values.Where(c => c.Status == ClaimStatus.PendingRetry).ToList();

            foreach (var claim in pendingClaims)
            {
                // Retry with the exact same ReferenceId to guarantee idempotency!
                var updated = claim with
                {
                    Status = ClaimStatus.Submitted
                };
                _claims[claim.TicketId] = updated;
                retriedCount++;

                AddNotification(
                    "system",
                    "Email",
                    claim.VolunteerId,
                    claim.VolunteerName,
                    $"HR Claim Retried Successfully ({claim.ReferenceId})",
                    $"Your pending claim {claim.ReferenceId} has been successfully submitted to HR with the same reference ID.",
                    $"/claims/{claim.Id}",
                    now);
            }

            return Task.FromResult(retriedCount);
        }
    }

    public Task<int> RunNightlySyncAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        lock (_stateGate)
        {
            var processedCount = 0;
            var submittedClaims = _claims.Values.Where(c => c.Status == ClaimStatus.Submitted).ToList();

            foreach (var claim in submittedClaims)
            {
                // In simulation, HR processes claims and marks them Paid (or occasionally Approved)
                var settled = claim with
                {
                    Status = ClaimStatus.Paid,
                    SettledAt = now
                };
                _claims[claim.TicketId] = settled;
                processedCount++;

                // Close corresponding ticket
                if (_tickets.TryGetValue(claim.TicketId, out var ticket))
                {
                    _tickets[claim.TicketId] = ticket with
                    {
                        Status = TicketStatus.Closed,
                        ClosedReason = "Reimbursed by HR"
                    };
                }

                // Decrement volunteer's unpaid claims count
                if (_volunteers.TryGetValue(claim.VolunteerId, out var volunteer))
                {
                    _volunteers[claim.VolunteerId] = volunteer with
                    {
                        UnpaidClaimsCount = Math.Max(0, volunteer.UnpaidClaimsCount - 1)
                    };
                }

                AddNotification(
                    ticket?.OfficeId ?? "system",
                    "Email",
                    claim.VolunteerId,
                    claim.VolunteerName,
                    $"Reimbursement Completed ({claim.ReferenceId})",
                    $"Good news! HR has approved and paid {claim.Amount} {claim.Currency} for ticket {claim.TicketId}. The ticket is now closed.",
                    $"/claims/{claim.Id}",
                    now);
            }

            return Task.FromResult(processedCount);
        }
    }

    public Task ResetDemoDataAsync(CancellationToken cancellationToken = default)
    {
        SeedInitialState();
        return Task.CompletedTask;
    }

    private void NotifyEligibleVolunteers(string officeId, SnackTicket ticket, DateTimeOffset now)
    {
        var eligible = _volunteers.Values
            .Where(v => v.CurrentOfficeId == officeId && v.IsActive && !v.IsOnLeave && v.OpenTicketsCount < 2)
            .ToList();

        foreach (var v in eligible)
        {
            AddNotification(
                officeId,
                "Internal Chat",
                v.Id,
                v.Name,
                $"Refill needed: {ticket.SnackName} ({ticket.ContainerId})",
                $"Container {ticket.ContainerId} at {ticket.CounterName} is empty. Quantity: {ticket.Quantity}. Est: {ticket.ReservedBudget} {ticket.Currency}. First to accept gets it!",
                $"/tickets/{ticket.Id}/accept",
                now);

            AddNotification(
                officeId,
                "Email",
                v.Id,
                v.Name,
                $"Snack Counter Refill Alert - {ticket.SnackName}",
                $"Hi {v.Name}, {ticket.SnackName} is empty at {ticket.CounterName}. Click the link below to accept this refill ticket.",
                $"/tickets/{ticket.Id}/accept",
                now);
        }
    }

    private void AddNotification(
        string officeId,
        string channel,
        string recipientId,
        string recipientName,
        string title,
        string body,
        string actionUrl,
        DateTimeOffset timestamp)
    {
        _notifications.Add(new NotificationMessage(
            Guid.NewGuid(),
            officeId,
            channel,
            recipientId,
            recipientName,
            title,
            body,
            actionUrl,
            timestamp));
    }

    private static string GetBudgetKey(string officeId, DateTimeOffset date) =>
        $"{officeId}:{date.Year:D4}-{date.Month:D2}";
}

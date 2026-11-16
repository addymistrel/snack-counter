using SnackCounter.Server.Models;
using SnackCounter.Server.Repositories;
using SnackCounter.Server.Services;

namespace SnackCounter.Server.Tests;

public sealed class ReplenishmentServiceTests
{
    [Fact]
    public async Task Rule1_DuplicateContainerReportReturnsExistingTicket()
    {
        var service = CreateService();
        var request = Report("pantry-oslo-01");

        var first = await service.ReportContainerAsync(request);
        var second = await service.ReportContainerAsync(request);

        Assert.Null(first.Error);
        Assert.NotNull(first.Ticket);
        Assert.Equal("Refill already in progress.", second.Error);
        Assert.Equal(first.Ticket!.Id, second.Ticket!.Id);
    }

    [Fact]
    public async Task Rule2_TwoPeopleCannotBothCommitToSameRefill()
    {
        var service = CreateService();
        var ticket = (await service.ReportContainerAsync(Report("pantry-oslo-02"))).Ticket!;

        // Volunteer Lars acknowledges first
        var winner = await service.AcknowledgeAsync(ticket.Id, new AcknowledgeRequest("lars", false));
        // Volunteer Sven tries afterwards
        var loser = await service.AcknowledgeAsync(ticket.Id, new AcknowledgeRequest("sven", false));

        Assert.Null(winner.Error);
        Assert.Equal("lars", winner.Ticket!.VolunteerId);
        Assert.Equal(TicketStatus.Acknowledged, winner.Ticket.Status);
        Assert.Equal("This ticket is no longer available.", loser.Error);
        Assert.Null(loser.Ticket);
    }

    [Fact]
    public async Task Rule3_OverBudgetReportRequiresAdminApproval()
    {
        var service = CreateService();
        // Oslo budget is 6000 NOK. Reporting 70 units @ 100 NOK = 7000 NOK (exceeds budget)
        var result = await service.ReportContainerAsync(Report("pantry-oslo-big", quantity: 70, unitPrice: 100m));

        Assert.Null(result.Error);
        Assert.NotNull(result.Ticket);
        Assert.Equal(TicketStatus.AwaitingBudgetApproval, result.Ticket!.Status);
        Assert.Equal(0m, result.Ticket.ReservedBudget); // Not reserved until approved!

        // Admin approves over-budget ticket
        var approved = await service.ApproveBudgetAsync(result.Ticket.Id, new BudgetApprovalRequest(true, "Approved for team offsite"));
        Assert.Null(approved.Error);
        Assert.Equal(TicketStatus.Open, approved.Ticket!.Status);
        Assert.Equal(7000m, approved.Ticket.ReservedBudget);
    }

    [Fact]
    public async Task Rule4_VolunteerDebtProtectionEnforcesConsentWhenUnpaidClaimsGte2()
    {
        var service = CreateService();
        var ticket = (await service.ReportContainerAsync(Report("pantry-oslo-debt"))).Ticket!;

        // Astrid Lind has 2 unpaid claims in initial seed
        // 1. Without consent: rejected with prompt
        var decline = await service.AcknowledgeAsync(ticket.Id, new AcknowledgeRequest("astrid", false));
        Assert.Contains("Reimbursement consent is required", decline.Error);
        Assert.Null(decline.Ticket);

        // Ticket remains Open for anyone else
        var current = (await service.GetTicketsAsync()).Single(t => t.Id == ticket.Id);
        Assert.Equal(TicketStatus.Open, current.Status);

        // 2. With consent: succeeds
        var accept = await service.AcknowledgeAsync(ticket.Id, new AcknowledgeRequest("astrid", true));
        Assert.Null(accept.Error);
        Assert.Equal("astrid", accept.Ticket!.VolunteerId);
        Assert.Equal(TicketStatus.Acknowledged, accept.Ticket.Status);
    }

    [Fact]
    public async Task Rule5_AutoPickSelectsFairVolunteerAndExcludesIneligible()
    {
        var service = CreateService();
        var ticket = (await service.ReportContainerAsync(Report("pantry-oslo-autopick"))).Ticket!;

        // Auto-pick checks eligible volunteers in Oslo (active, not on leave, < 2 open tickets)
        // Arjun Rao is in Oslo (visiting) with 0 tickets and never assigned (LastAssignedAt = null)
        // Freja is on leave (ineligible), Henrik is away visiting BLR (ineligible in Oslo)
        var autoPick = await service.SimulateAutoPickAsync(ticket.Id);

        Assert.Null(autoPick.Error);
        Assert.NotNull(autoPick.Ticket);
        Assert.True(autoPick.Ticket!.IsAutoAssigned);
        Assert.Equal("arjun", autoPick.Ticket.VolunteerId); // Fairest rotation: 0 tickets, longest since last assigned
        Assert.Equal(TicketStatus.Acknowledged, autoPick.Ticket.Status);
    }

    [Fact]
    public async Task Rule6_DeadlineExpiryReassignsToNextVolunteerWithFreshDeadline()
    {
        var service = CreateService();
        var ticket = (await service.ReportContainerAsync(Report("pantry-oslo-expiry"))).Ticket!;

        // Manually assign to Lars
        await service.AcknowledgeAsync(ticket.Id, new AcknowledgeRequest("lars", false));

        // Simulate 48h deadline expiration
        var expiredResult = await service.SimulateExpiryAsync(ticket.Id);

        Assert.Null(expiredResult.Error);
        Assert.NotNull(expiredResult.Ticket);
        Assert.Equal(TicketStatus.Acknowledged, expiredResult.Ticket!.Status);
        // Must be reassigned to someone other than Lars (e.g. Sven)
        Assert.NotEqual("lars", expiredResult.Ticket.VolunteerId);
        Assert.Equal("lars", expiredResult.Ticket.ExpiredVolunteerId);
    }

    [Fact]
    public async Task Rule7_TicketTransferHandsOffTicketWhilePreservingDeadline()
    {
        var service = CreateService();
        var ticket = (await service.ReportContainerAsync(Report("pantry-oslo-transfer"))).Ticket!;

        var ack = await service.AcknowledgeAsync(ticket.Id, new AcknowledgeRequest("lars", false));
        var initialDeadline = ack.Ticket!.CompletionDeadline;

        // Lars transfers to Sven
        var transfer = await service.TransferTicketAsync(ticket.Id, new TransferTicketRequest("lars", "sven", false));

        Assert.Null(transfer.Error);
        Assert.NotNull(transfer.Ticket);
        Assert.Equal("sven", transfer.Ticket!.VolunteerId);
        Assert.Equal(initialDeadline, transfer.Ticket.CompletionDeadline); // Deadline preserved!
    }

    [Fact]
    public async Task Rule8_ProofSubmissionFlagsExcessSpendWithoutBlocking()
    {
        var service = CreateService();
        // Reserved budget = 500 NOK (5 x 100 NOK)
        var ticket = (await service.ReportContainerAsync(Report("pantry-oslo-proof", quantity: 5, unitPrice: 100m))).Ticket!;
        await service.AcknowledgeAsync(ticket.Id, new AcknowledgeRequest("lars", false));

        // Actual spend is 620 NOK (exceeds 500 NOK estimate)
        var proof = await service.SubmitProofAsync(ticket.Id, new ProofRequest(
            "lars",
            620m,
            ["Salted Almonds x10", "Artisan Chocolate"],
            "receipt-123.jpg",
            "counter-123.jpg"));

        Assert.Null(proof.Error);
        Assert.NotNull(proof.Ticket);
        Assert.Equal(TicketStatus.Submitted, proof.Ticket!.Status);
        Assert.Equal(620m, proof.Ticket.SpentAmount);
        Assert.True(proof.Ticket.ProofExceedsEstimate); // Flagged, but NOT blocked!
    }

    [Fact]
    public async Task Rule9_SimulatedHRDowntimeAndIdempotentRetry()
    {
        var service = CreateService();
        var ticket = (await service.ReportContainerAsync(Report("pantry-oslo-hr"))).Ticket!;
        await service.AcknowledgeAsync(ticket.Id, new AcknowledgeRequest("lars", false));

        // 1. Simulate HR is down
        await service.ToggleHRDownAsync();

        // 2. Submit proof
        await service.SubmitProofAsync(ticket.Id, new ProofRequest("lars", 450m, ["Almonds"], "r.jpg", "c.jpg"));
        var claim = await service.GetClaimAsync(ticket.Id);

        Assert.NotNull(claim);
        Assert.Equal(ClaimStatus.PendingRetry, claim!.Status);
        var expectedRefId = "SNACK-" + ticket.Id.ToString("N");
        Assert.Equal(expectedRefId, claim.ReferenceId);

        // 3. HR comes back up and retry is triggered
        await service.ToggleHRDownAsync();
        var retriedCount = await service.RetryPendingClaimsAsync();
        var updatedClaim = await service.GetClaimAsync(ticket.Id);

        Assert.Equal(1, retriedCount);
        Assert.Equal(ClaimStatus.Submitted, updatedClaim!.Status);
        Assert.Equal(expectedRefId, updatedClaim.ReferenceId); // Proves idempotency with SAME ID!
    }

    [Fact]
    public async Task Rule10_NightlyBatchSyncSettlesPaidClaimsAndClosesTickets()
    {
        var service = CreateService();
        var ticket = (await service.ReportContainerAsync(Report("pantry-oslo-sync"))).Ticket!;
        await service.AcknowledgeAsync(ticket.Id, new AcknowledgeRequest("lars", false));
        await service.SubmitProofAsync(ticket.Id, new ProofRequest("lars", 400m, ["Granola"], "r.jpg", "c.jpg"));

        // Run batch nightly sync
        var processedCount = await service.RunNightlySyncAsync();
        var updatedTicket = (await service.GetTicketsAsync()).Single(t => t.Id == ticket.Id);
        var claim = await service.GetClaimAsync(ticket.Id);

        Assert.True(processedCount >= 1);
        Assert.Equal(ClaimStatus.Paid, claim!.Status);
        Assert.Equal(TicketStatus.Closed, updatedTicket.Status);
        Assert.Equal("Reimbursed by HR", updatedTicket.ClosedReason);
    }

    [Fact]
    public async Task Rule11_SidePathAlreadyRefilledClosesTicketAndFilesExceptionClaim()
    {
        var service = CreateService();
        var ticket = (await service.ReportContainerAsync(Report("pantry-oslo-sidepath"))).Ticket!;
        // Lars was assigned
        await service.AcknowledgeAsync(ticket.Id, new AcknowledgeRequest("lars", false));

        // Employee Kari walks by and taps "Already refilled"
        var result = await service.ReportAlreadyRefilledAsync(new AlreadyRefilledRequest(
            "pantry-oslo-sidepath",
            "oslo",
            "kari",
            "Kari Nordmann",
            320m,
            ["Cookies", "Coffee"],
            "kari-receipt.jpg"));

        Assert.Null(result.Error);
        Assert.NotNull(result.Claim);
        Assert.True(result.Claim!.IsException);
        Assert.Equal("kari", result.Claim.VolunteerId);
        Assert.Equal("Kari Nordmann", result.Claim.ExceptionReporterName);
        Assert.Equal(320m, result.Claim.Amount);

        // The in-flight ticket was closed so nobody buys again
        var updatedTicket = (await service.GetTicketsAsync()).Single(t => t.Id == ticket.Id);
        Assert.Equal(TicketStatus.Closed, updatedTicket.Status);
        Assert.Contains("Already refilled by Kari Nordmann", updatedTicket.ClosedReason);
        // Explicitly NOT credited to Lars
        Assert.Equal(0m, updatedTicket.SpentAmount);
    }

    [Fact]
    public async Task Rule12_MonthlyReportConsolidatesCurrenciesWithLockedFxRate()
    {
        var service = CreateService();

        // Oslo purchase: 1000 NOK
        var t1 = (await service.ReportContainerAsync(Report("pantry-oslo-rep", quantity: 10, unitPrice: 100m))).Ticket!;
        await service.AcknowledgeAsync(t1.Id, new AcknowledgeRequest("lars", false));
        await service.SubmitProofAsync(t1.Id, new ProofRequest("lars", 1000m, ["Dark Chocolate"], "r.jpg", "c.jpg"));

        // Bengaluru purchase: 2000 INR
        var t2 = (await service.ReportContainerAsync(new ReportContainerRequest(
            "pantry-blr-rep",
            "bengaluru",
            true, true, true,
            "Ground Floor",
            "Chai Biscuits",
            20,
            100m,
            "emp-blr",
            "Bengaluru Staff"))).Ticket!;
        await service.AcknowledgeAsync(t2.Id, new AcknowledgeRequest("priya", false));
        await service.SubmitProofAsync(t2.Id, new ProofRequest("priya", 2000m, ["Chai Biscuits"], "r.jpg", "c.jpg"));

        var report = await service.GetMonthlyReportAsync(DateOnly.FromDateTime(DateTime.UtcNow));

        var oslo = report.Offices.Single(o => o.OfficeId == "oslo");
        var blr = report.Offices.Single(o => o.OfficeId == "bengaluru");

        Assert.Equal(1000m, oslo.Spent);
        Assert.Equal(2000m, blr.Spent);
        // Consolidated NOK: 1000 NOK + (2000 INR * 0.13) = 1000 + 260 = 1260 NOK
        Assert.Equal(1260m, report.ConsolidatedNokTotal);
        Assert.Equal(0.13m, report.LockedExchangeRateInrToNok);
        Assert.NotEmpty(report.TopSnacks);
    }

    private static ReportContainerRequest Report(string containerId, int quantity = 5, decimal unitPrice = 100m) =>
        new(containerId, "oslo", true, true, true, "Floor 3 Main Pantry", "Assorted snacks", quantity, unitPrice, "emp-test", "Test Reporter");

    private static SnackCounterService CreateService() =>
        new(new InMemoryReplenishmentRepository());
}

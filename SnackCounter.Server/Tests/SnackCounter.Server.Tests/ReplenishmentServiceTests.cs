using SnackCounter.Server.Models;
using SnackCounter.Server.Repositories;
using SnackCounter.Server.Services;

namespace SnackCounter.Server.Tests;

public sealed class ReplenishmentServiceTests
{
    [Fact]
    public async Task DuplicateContainerReportReturnsExistingTicket()
    {
        var service = CreateService();
        var request = Report("pantry-1");

        var first = await service.ReportContainerAsync(request);
        var second = await service.ReportContainerAsync(request);

        Assert.Null(first.Error);
        Assert.Equal("This container already has an active ticket.", second.Error);
        Assert.Equal(first.Ticket!.Id, second.Ticket!.Id);
    }

    [Fact]
    public async Task ReportIsRejectedWhenBudgetDoesNotFit()
    {
        var service = CreateService();

        var result = await service.ReportContainerAsync(Report("pantry-over-budget", quantity: 61));

        Assert.Null(result.Ticket);
        Assert.Contains("monthly budget", result.Error);
    }

    [Fact]
    public async Task AcknowledgementAssignsVolunteerAndSecondVolunteerLoses()
    {
        var service = CreateService();
        var ticket = (await service.ReportContainerAsync(Report("pantry-2"))).Ticket!;

        var winner = await service.AcknowledgeAsync(ticket.Id, new AcknowledgeRequest("volunteer-a", false));
        var loser = await service.AcknowledgeAsync(ticket.Id, new AcknowledgeRequest("volunteer-b", false));

        Assert.Equal("volunteer-a", winner.Ticket!.VolunteerId);
        Assert.Equal("This ticket is no longer available.", loser.Error);
    }

    [Fact]
    public async Task ProofCreatesClaimAndSettlesTicket()
    {
        var service = CreateService();
        var ticket = (await service.ReportContainerAsync(Report("pantry-3"))).Ticket!;
        await service.AcknowledgeAsync(ticket.Id, new AcknowledgeRequest("volunteer-a", false));

        var proof = await service.SubmitProofAsync(ticket.Id, new ProofRequest(
            "volunteer-a",
            125m,
            ["Granola", "Fruit"],
            "receipt.jpg",
            "counter.jpg"));
        var claim = await service.GetClaimAsync(ticket.Id);

        Assert.Equal(TicketStatus.Submitted, proof.Ticket!.Status);
        Assert.Equal(125m, proof.Ticket.SpentAmount);
        Assert.Equal(ClaimStatus.Submitted, claim!.Status);
        Assert.Equal("SNACK-" + ticket.Id.ToString("N"), claim.ReferenceId);
    }

    private static ReportContainerRequest Report(string containerId, int quantity = 5) =>
        new(containerId, "oslo", true, true, true, "Assorted snacks", quantity, 100m);

    private static SnackCounterService CreateService() =>
        new(new InMemoryReplenishmentRepository());
}

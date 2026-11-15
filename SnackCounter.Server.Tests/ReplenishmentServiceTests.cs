using SnackCounter.Server.Models;
using SnackCounter.Server.Services;

namespace SnackCounter.Server.Tests;

public sealed class ReplenishmentServiceTests
{
    [Fact]
    public void DuplicateContainerReportReturnsExistingTicket()
    {
        var service = new SnackCounterService();
        var request = Report("pantry-1");

        var first = service.ReportContainer(request);
        var second = service.ReportContainer(request);

        Assert.Null(first.Error);
        Assert.Equal("This container already has an active ticket.", second.Error);
        Assert.Equal(first.Ticket!.Id, second.Ticket!.Id);
    }

    [Fact]
    public void ReportIsRejectedWhenBudgetDoesNotFit()
    {
        var service = new SnackCounterService();

        var result = service.ReportContainer(Report("pantry-over-budget", quantity: 61));

        Assert.Null(result.Ticket);
        Assert.Contains("monthly budget", result.Error);
    }

    [Fact]
    public void AcknowledgementAssignsVolunteerAndSecondVolunteerLoses()
    {
        var service = new SnackCounterService();
        var ticket = service.ReportContainer(Report("pantry-2")).Ticket!;

        var winner = service.Acknowledge(ticket.Id, new AcknowledgeRequest("volunteer-a", false));
        var loser = service.Acknowledge(ticket.Id, new AcknowledgeRequest("volunteer-b", false));

        Assert.Equal("volunteer-a", winner.Ticket!.VolunteerId);
        Assert.Equal("This ticket is no longer available.", loser.Error);
    }

    [Fact]
    public void ProofCreatesClaimAndSettlesTicket()
    {
        var service = new SnackCounterService();
        var ticket = service.ReportContainer(Report("pantry-3")).Ticket!;
        service.Acknowledge(ticket.Id, new AcknowledgeRequest("volunteer-a", false));

        var proof = service.SubmitProof(ticket.Id, new ProofRequest(
            "volunteer-a",
            125m,
            ["Granola", "Fruit"],
            "receipt.jpg",
            "counter.jpg"));
        var claim = service.GetClaim(ticket.Id);

        Assert.Equal(TicketStatus.Submitted, proof.Ticket!.Status);
        Assert.Equal(125m, proof.Ticket.SpentAmount);
        Assert.Equal(ClaimStatus.Submitted, claim!.Status);
        Assert.Equal("SNACK-" + ticket.Id.ToString("N"), claim.ReferenceId);
    }

    private static ReportContainerRequest Report(string containerId, int quantity = 5) =>
        new(containerId, "oslo", true, true, true, "Assorted snacks", quantity, 100m);
}

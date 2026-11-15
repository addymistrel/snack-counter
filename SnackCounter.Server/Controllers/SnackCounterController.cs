using Microsoft.AspNetCore.Mvc;
using SnackCounter.Server.Application;
using SnackCounter.Server.Models;

namespace SnackCounter.Server.Controllers;

[ApiController]
[Route("api/snack-counter")]
public sealed class SnackCounterController(IReplenishmentService service) : ControllerBase
{
    [HttpGet("dashboard")]
    public IActionResult Dashboard() => Ok(new { offices = service.Offices, tickets = service.GetTickets() });

    [HttpPost("containers/report")]
    public IActionResult ReportContainer(ReportContainerRequest request)
    {
        var result = service.ReportContainer(request);
        return result.Error is null ? Ok(result.Ticket) : BadRequest(new { error = result.Error, ticket = result.Ticket });
    }

    [HttpPost("tickets/{ticketId:guid}/acknowledge")]
    public IActionResult Acknowledge(Guid ticketId, AcknowledgeRequest request)
    {
        var result = service.Acknowledge(ticketId, request);
        return result.Error is null ? Ok(result.Ticket) : BadRequest(new { error = result.Error });
    }

    [HttpPost("tickets/{ticketId:guid}/proof")]
    public IActionResult SubmitProof(Guid ticketId, ProofRequest request)
    {
        var result = service.SubmitProof(ticketId, request);
        return result.Error is null ? Ok(result.Ticket) : BadRequest(new { error = result.Error });
    }

    [HttpGet("reports/monthly")]
    public IActionResult MonthlyReport([FromQuery] DateOnly? month = null) =>
        Ok(service.GetMonthlyReport(month ?? DateOnly.FromDateTime(DateTime.UtcNow)));
}

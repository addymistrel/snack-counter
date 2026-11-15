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
        if (result.Error is null)
            return Ok(result.Ticket);
        return result.Ticket is not null
            ? Conflict(new { error = result.Error, ticket = result.Ticket })
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("tickets/{ticketId:guid}/acknowledge")]
    public IActionResult Acknowledge(Guid ticketId, AcknowledgeRequest request)
    {
        var result = service.Acknowledge(ticketId, request);
        if (result.Error is null)
            return Ok(result.Ticket);
        return result.Error is "This ticket is no longer available."
            or "The acknowledgement window has expired."
            or "Volunteers can have at most two open tickets."
            or "Reimbursement consent is required when you have two or more unpaid claims."
            ? Conflict(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("tickets/{ticketId:guid}/proof")]
    public IActionResult SubmitProof(Guid ticketId, ProofRequest request)
    {
        var result = service.SubmitProof(ticketId, request);
        if (result.Error is null)
            return Ok(result.Ticket);
        return result.Error is "Only the assigned volunteer can submit proof for an acknowledged ticket."
            ? Conflict(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("reports/monthly")]
    public IActionResult MonthlyReport([FromQuery] DateOnly? month = null) =>
        Ok(service.GetMonthlyReport(month ?? DateOnly.FromDateTime(DateTime.UtcNow)));

    [HttpGet("tickets/{ticketId:guid}/claim")]
    public IActionResult Claim(Guid ticketId)
    {
        var claim = service.GetClaim(ticketId);
        return claim is null ? NotFound(new { error = "Claim not found." }) : Ok(claim);
    }
}

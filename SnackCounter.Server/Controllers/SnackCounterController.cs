using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using SnackCounter.Server.Application;
using SnackCounter.Server.Models;

namespace SnackCounter.Server.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/snack-counter")]
public sealed class SnackCounterController(IReplenishmentService service) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken) =>
        Ok(new
        {
            offices = await service.GetOfficesAsync(cancellationToken),
            tickets = await service.GetTicketsAsync(cancellationToken)
        });

    [HttpPost("containers/report")]
    public async Task<IActionResult> ReportContainer(
        ReportContainerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ReportContainerAsync(request, cancellationToken);
        if (result.Error is null)
            return Ok(result.Ticket);
        return result.Ticket is not null
            ? Conflict(new { error = result.Error, ticket = result.Ticket })
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("tickets/{ticketId:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(
        Guid ticketId,
        AcknowledgeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.AcknowledgeAsync(ticketId, request, cancellationToken);
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
    public async Task<IActionResult> SubmitProof(
        Guid ticketId,
        ProofRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.SubmitProofAsync(ticketId, request, cancellationToken);
        if (result.Error is null)
            return Ok(result.Ticket);
        return result.Error is "Only the assigned volunteer can submit proof for an acknowledged ticket."
            ? Conflict(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("reports/monthly")]
    public async Task<IActionResult> MonthlyReport(
        [FromQuery] DateOnly? month = null,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetMonthlyReportAsync(
            month ?? DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken));

    [HttpGet("tickets/{ticketId:guid}/claim")]
    public async Task<IActionResult> Claim(Guid ticketId, CancellationToken cancellationToken)
    {
        var claim = await service.GetClaimAsync(ticketId, cancellationToken);
        return claim is null ? NotFound(new { error = "Claim not found." }) : Ok(claim);
    }
}

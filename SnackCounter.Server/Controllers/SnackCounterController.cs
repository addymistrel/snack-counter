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
        Ok(await service.GetDashboardDataAsync(cancellationToken));

    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken) =>
        Ok(new
        {
            offices = await service.GetOfficesAsync(cancellationToken),
            containers = await service.GetContainersAsync(cancellationToken)
        });

    [HttpGet("volunteers")]
    public async Task<IActionResult> Volunteers(CancellationToken cancellationToken) =>
        Ok(await service.GetVolunteersAsync(cancellationToken));

    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications(CancellationToken cancellationToken) =>
        Ok(await service.GetNotificationsAsync(cancellationToken));

    [HttpGet("claims")]
    public async Task<IActionResult> Claims(CancellationToken cancellationToken) =>
        Ok(await service.GetClaimsAsync(cancellationToken));

    [HttpGet("tickets/{ticketId:guid}/claim")]
    public async Task<IActionResult> Claim(Guid ticketId, CancellationToken cancellationToken)
    {
        var claim = await service.GetClaimAsync(ticketId, cancellationToken);
        return claim is null ? NotFound(new { error = "Claim not found." }) : Ok(claim);
    }

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

    [HttpPost("containers/already-refilled")]
    public async Task<IActionResult> AlreadyRefilled(
        AlreadyRefilledRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ReportAlreadyRefilledAsync(request, cancellationToken);
        if (result.Error is null)
            return Ok(new { ticket = result.Ticket, claim = result.Claim });

        return BadRequest(new { error = result.Error });
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

        if (result.Error.StartsWith("Reimbursement consent is required"))
        {
            return StatusCode(StatusCodes.Status428PreconditionRequired, new
            {
                error = result.Error,
                requiresConsent = true,
                message = "Your earlier claims are still being reimbursed. Another purchase will tie up more of your money. Continue?"
            });
        }

        return result.Error is "This ticket is no longer available."
            or "The acknowledgement window has expired."
            or "Volunteers can have at most two open tickets."
            ? Conflict(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("tickets/{ticketId:guid}/transfer")]
    public async Task<IActionResult> Transfer(
        Guid ticketId,
        TransferTicketRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.TransferTicketAsync(ticketId, request, cancellationToken);
        if (result.Error is null)
            return Ok(result.Ticket);

        if (result.Error.StartsWith("Reimbursement consent is required"))
        {
            return StatusCode(StatusCodes.Status428PreconditionRequired, new
            {
                error = result.Error,
                requiresConsent = true
            });
        }

        return BadRequest(new { error = result.Error });
    }

    [HttpPost("tickets/{ticketId:guid}/simulate-auto-pick")]
    public async Task<IActionResult> SimulateAutoPick(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var result = await service.SimulateAutoPickAsync(ticketId, cancellationToken);
        if (result.Error is null)
            return Ok(result.Ticket);

        return BadRequest(new { error = result.Error, ticket = result.Ticket });
    }

    [HttpPost("tickets/{ticketId:guid}/simulate-expiry")]
    public async Task<IActionResult> SimulateExpiry(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var result = await service.SimulateExpiryAsync(ticketId, cancellationToken);
        if (result.Error is null)
            return Ok(result.Ticket);

        return BadRequest(new { error = result.Error });
    }

    [HttpPost("tickets/{ticketId:guid}/budget-approval")]
    public async Task<IActionResult> BudgetApproval(
        Guid ticketId,
        BudgetApprovalRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ApproveBudgetAsync(ticketId, request, cancellationToken);
        if (result.Error is null)
            return Ok(result.Ticket);

        return BadRequest(new { error = result.Error });
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

    [HttpPost("hr/toggle-down")]
    public async Task<IActionResult> ToggleHRDown(CancellationToken cancellationToken)
    {
        var isDown = await service.ToggleHRDownAsync(cancellationToken);
        return Ok(new { isDown });
    }

    [HttpPost("hr/retry")]
    public async Task<IActionResult> RetryPendingClaims(CancellationToken cancellationToken)
    {
        var retriedCount = await service.RetryPendingClaimsAsync(cancellationToken);
        return Ok(new { retriedCount });
    }

    [HttpPost("hr/nightly-sync")]
    public async Task<IActionResult> NightlySync(CancellationToken cancellationToken)
    {
        var processedCount = await service.RunNightlySyncAsync(cancellationToken);
        return Ok(new { processedCount });
    }

    [HttpPost("reset-demo")]
    public async Task<IActionResult> ResetDemo(CancellationToken cancellationToken)
    {
        await service.ResetDemoDataAsync(cancellationToken);
        return Ok(new { message = "Demo data has been reset to initial state." });
    }

    [HttpGet("reports/monthly")]
    public async Task<IActionResult> MonthlyReport(
        [FromQuery] DateOnly? month = null,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetMonthlyReportAsync(
            month ?? DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken));
}

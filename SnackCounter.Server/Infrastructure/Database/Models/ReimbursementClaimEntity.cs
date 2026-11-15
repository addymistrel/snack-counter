namespace SnackCounter.Server.Infrastructure.Database.Models;

public sealed class ReimbursementClaimEntity
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public SnackTicketEntity Ticket { get; set; } = null!;
    public string ReferenceId { get; set; } = string.Empty;
    public string VolunteerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

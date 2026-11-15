namespace SnackCounter.Server.Infrastructure.Database.Models;

public sealed class SnackTicketEntity
{
    public Guid Id { get; set; }
    public string ContainerId { get; set; } = string.Empty;
    public string SnackName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string OfficeId { get; set; } = string.Empty;
    public OfficeEntity Office { get; set; } = null!;
    public string Currency { get; set; } = string.Empty;
    public decimal ReservedBudget { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? VolunteerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset AcknowledgementDeadline { get; set; }
    public decimal? SpentAmount { get; set; }
}

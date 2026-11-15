namespace SnackCounter.Server.Infrastructure.Database.Models;

public sealed class OfficeEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal MonthlyBudget { get; set; }
    public ICollection<SnackTicketEntity> Tickets { get; set; } = [];
}

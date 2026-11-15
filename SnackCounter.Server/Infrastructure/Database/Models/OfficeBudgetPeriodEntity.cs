namespace SnackCounter.Server.Infrastructure.Database.Models;

public sealed class OfficeBudgetPeriodEntity
{
    public string OfficeId { get; set; } = string.Empty;
    public OfficeEntity Office { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal LimitAmount { get; set; }
    public decimal Reserved { get; set; }
    public decimal Spent { get; set; }
    public string Currency { get; set; } = string.Empty;
}

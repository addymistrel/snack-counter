using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnackCounter.Server.Infrastructure.Database.Models;

namespace SnackCounter.Server.Infrastructure.Database.Configuration;

public sealed class OfficeBudgetPeriodConfiguration : IEntityTypeConfiguration<OfficeBudgetPeriodEntity>
{
    public void Configure(EntityTypeBuilder<OfficeBudgetPeriodEntity> builder)
    {
        builder.HasKey(period => new { period.OfficeId, period.Year, period.Month });
        builder.Property(period => period.Currency).HasMaxLength(3).IsRequired();
        builder.Property(period => period.LimitAmount).HasPrecision(18, 2);
        builder.Property(period => period.Reserved).HasPrecision(18, 2);
        builder.Property(period => period.Spent).HasPrecision(18, 2);
        builder.HasCheckConstraint("ck_budget_period_month", "\"Month\" BETWEEN 1 AND 12");
        builder.HasOne(period => period.Office)
            .WithMany()
            .HasForeignKey(period => period.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

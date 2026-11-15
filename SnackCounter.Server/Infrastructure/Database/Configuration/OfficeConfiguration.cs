using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnackCounter.Server.Infrastructure.Database.Models;

namespace SnackCounter.Server.Infrastructure.Database.Configuration;

public sealed class OfficeConfiguration : IEntityTypeConfiguration<OfficeEntity>
{
    public void Configure(EntityTypeBuilder<OfficeEntity> builder)
    {
        builder.HasKey(office => office.Id);
        builder.Property(office => office.Name).HasMaxLength(100).IsRequired();
        builder.Property(office => office.Currency).HasMaxLength(3).IsRequired();
        builder.Property(office => office.MonthlyBudget).HasPrecision(18, 2);
        builder.HasData(
            new OfficeEntity { Id = "oslo", Name = "Oslo", Currency = "NOK", MonthlyBudget = 6000m },
            new OfficeEntity { Id = "bengaluru", Name = "Bengaluru", Currency = "INR", MonthlyBudget = 15000m });
    }
}

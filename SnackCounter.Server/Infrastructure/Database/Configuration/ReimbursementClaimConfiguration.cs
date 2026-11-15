using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnackCounter.Server.Infrastructure.Database.Models;

namespace SnackCounter.Server.Infrastructure.Database.Configuration;

public sealed class ReimbursementClaimConfiguration : IEntityTypeConfiguration<ReimbursementClaimEntity>
{
    public void Configure(EntityTypeBuilder<ReimbursementClaimEntity> builder)
    {
        builder.HasKey(claim => claim.Id);
        builder.HasIndex(claim => claim.TicketId).IsUnique();
        builder.HasIndex(claim => claim.ReferenceId).IsUnique();
        builder.Property(claim => claim.ReferenceId).HasMaxLength(80).IsRequired();
        builder.Property(claim => claim.VolunteerId).HasMaxLength(100).IsRequired();
        builder.Property(claim => claim.Currency).HasMaxLength(3).IsRequired();
        builder.Property(claim => claim.Status).HasMaxLength(32).IsRequired();
        builder.Property(claim => claim.Amount).HasPrecision(18, 2);
        builder.HasOne(claim => claim.Ticket)
            .WithMany()
            .HasForeignKey(claim => claim.TicketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

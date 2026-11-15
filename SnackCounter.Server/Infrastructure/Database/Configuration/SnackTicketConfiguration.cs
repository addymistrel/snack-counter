using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnackCounter.Server.Infrastructure.Database.Models;

namespace SnackCounter.Server.Infrastructure.Database.Configuration;

public sealed class SnackTicketConfiguration : IEntityTypeConfiguration<SnackTicketEntity>
{
    public void Configure(EntityTypeBuilder<SnackTicketEntity> builder)
    {
        builder.HasKey(ticket => ticket.Id);
        builder.HasIndex(ticket => new { ticket.ContainerId, ticket.Status });
        builder.HasIndex(ticket => ticket.ContainerId)
            .HasDatabaseName("ux_tickets_active_container")
            .IsUnique()
            .HasFilter("\"Status\" IN ('Open', 'Acknowledged')");
        builder.Property(ticket => ticket.ContainerId).HasMaxLength(100).IsRequired();
        builder.Property(ticket => ticket.Currency).HasMaxLength(3).IsRequired();
        builder.Property(ticket => ticket.Status).HasMaxLength(32).IsRequired();
        builder.Property(ticket => ticket.ReservedBudget).HasPrecision(18, 2);
        builder.Property(ticket => ticket.SpentAmount).HasPrecision(18, 2);
        builder.HasOne(ticket => ticket.Office)
            .WithMany(office => office.Tickets)
            .HasForeignKey(ticket => ticket.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

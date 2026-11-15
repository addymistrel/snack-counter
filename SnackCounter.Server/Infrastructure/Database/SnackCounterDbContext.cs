using Microsoft.EntityFrameworkCore;
using SnackCounter.Server.Infrastructure.Database.Configuration;
using SnackCounter.Server.Infrastructure.Database.Models;

namespace SnackCounter.Server.Infrastructure.Database;

public sealed class SnackCounterDbContext(DbContextOptions<SnackCounterDbContext> options) : DbContext(options)
{
    public DbSet<OfficeEntity> Offices => Set<OfficeEntity>();
    public DbSet<SnackTicketEntity> SnackTickets => Set<SnackTicketEntity>();
    public DbSet<OfficeBudgetPeriodEntity> OfficeBudgetPeriods => Set<OfficeBudgetPeriodEntity>();
    public DbSet<ReimbursementClaimEntity> ReimbursementClaims => Set<ReimbursementClaimEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OfficeConfiguration());
        modelBuilder.ApplyConfiguration(new OfficeBudgetPeriodConfiguration());
        modelBuilder.ApplyConfiguration(new ReimbursementClaimConfiguration());
        modelBuilder.ApplyConfiguration(new SnackTicketConfiguration());
    }
}

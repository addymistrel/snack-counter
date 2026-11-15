using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SnackCounter.Server.Infrastructure.Database;

public sealed class SnackCounterDbContextFactory : IDesignTimeDbContextFactory<SnackCounterDbContext>
{
    public SnackCounterDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Set DATABASE_URL or ConnectionStrings__DefaultConnection before running EF database commands.");

        var options = new DbContextOptionsBuilder<SnackCounterDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new SnackCounterDbContext(options);
    }
}

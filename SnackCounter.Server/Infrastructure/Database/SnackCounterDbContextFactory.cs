using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SnackCounter.Server.Infrastructure.Database;

public sealed class SnackCounterDbContextFactory : IDesignTimeDbContextFactory<SnackCounterDbContext>
{
    public SnackCounterDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? LoadApplicationConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Configure ConnectionStrings:DefaultConnection in appsettings or set DATABASE_URL before running EF database commands.");

        var options = new DbContextOptionsBuilder<SnackCounterDbContext>()
            .UseNpgsql(DatabaseConnectionString.Normalize(connectionString))
            .Options;
        return new SnackCounterDbContext(options);
    }

    private static string? LoadApplicationConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();
        return configuration.GetConnectionString("DefaultConnection");
    }
}

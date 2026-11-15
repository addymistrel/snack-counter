using Microsoft.EntityFrameworkCore;

namespace SnackCounter.Server.Infrastructure.Persistence;

public sealed class SnackCounterDbContext(DbContextOptions<SnackCounterDbContext> options) : DbContext(options);

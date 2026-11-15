using Microsoft.EntityFrameworkCore;
using SnackCounter.Server.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1.0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"];
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<SnackCounterDbContext>(options =>
        options.UseNpgsql(DatabaseConnectionString.Normalize(connectionString)));
}
builder.Services.AddScoped<SnackCounter.Server.Application.IReplenishmentRepository,
    SnackCounter.Server.Repositories.EfReplenishmentRepository>();
builder.Services.AddScoped<SnackCounter.Server.Application.IReplenishmentService,
    SnackCounter.Server.Services.SnackCounterService>();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgresql");
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.MapFallbackToFile("/index.html");

app.Run();

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using SnackCounter.Server.Application;
using SnackCounter.Server.Infrastructure.Database;
using SnackCounter.Server.Repositories;
using SnackCounter.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1.0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"];
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemory", true);

if (!useInMemory && !string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<SnackCounterDbContext>(options =>
        options.UseNpgsql(DatabaseConnectionString.Normalize(connectionString)));
    builder.Services.AddScoped<IReplenishmentRepository, EfReplenishmentRepository>();
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("postgresql");
}
else
{
    // High-performance concurrency-safe singleton for demo, test, and standalone execution
    builder.Services.AddSingleton<IReplenishmentRepository, InMemoryReplenishmentRepository>();
    builder.Services.AddHealthChecks();
}

builder.Services.AddScoped<IReplenishmentService, SnackCounterService>();

// Configure OpenAPI / Swagger
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

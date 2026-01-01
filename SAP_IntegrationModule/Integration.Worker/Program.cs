using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Integration.Application.Services;
using Integration.Infrastructure.Clients;
using Integration.Infrastructure.Data;
using Integration.Infrastructure.Repositories;
using Integration.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// Database contexts - SAME AS API PROJECT
builder.Services.AddDbContext<GlobalDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("GlobalDatabase");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("GlobalDatabase connection string is not configured");
    }

    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(300);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            new[] { 1205, 4060 });
    });
});

// BU DbContext factory - SAME AS API PROJECT
builder.Services.AddScoped<Func<string, BuDbContext>>(provider => buCode =>
{
    // Connection strings are named "BU1", "BU2" in appsettings
    var connectionString = builder.Configuration.GetConnectionString(buCode);

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException($"No connection string found for business unit: {buCode}");
    }

    var optionsBuilder = new DbContextOptionsBuilder<BuDbContext>();
    optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(300);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            new[] { 1205, 4060 });
    });

    return new BuDbContext(optionsBuilder.Options, buCode);
});

// SAP HTTP Client - SAME AS API PROJECT
builder.Services.AddHttpClient<ISapClient, SapApiClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = config["SapApi:BaseUrl"];
    var username = config["SapApi:Username"];
    var password = config["SapApi:Password"];

    if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
    {
        throw new InvalidOperationException("SAP API configuration is incomplete. Check appsettings.json");
    }

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(config.GetValue("SapApi:TimeoutSeconds", 120));

    // Basic authentication
    var authToken = Convert.ToBase64String(
        System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

    // Add default headers
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
});

builder.Services.AddScoped<CustomerMappingHelper>();
builder.Services.AddScoped<IRetailerRepository, RetailerRepository>();
builder.Services.AddScoped<ICustomerSyncService, CustomerSyncService>();
builder.Services.AddScoped<MaterialMappingHelper>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IMaterialSyncService, MaterialSyncService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Worker starting up...");
logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);

var configuration = host.Services.GetRequiredService<IConfiguration>();
var sapBaseUrl = configuration["SapApi:BaseUrl"];
var globalConnString = configuration.GetConnectionString("GlobalDatabase");

if (string.IsNullOrEmpty(sapBaseUrl))
{
    logger.LogError("SAP API BaseUrl is not configured!");
    throw new InvalidOperationException("SAP API BaseUrl is required");
}

if (string.IsNullOrEmpty(globalConnString))
{
    logger.LogError("GlobalDatabase connection string is not configured!");
    throw new InvalidOperationException("GlobalDatabase connection string is required");
}

logger.LogInformation("Worker service registered and ready to start");

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Worker terminated unexpectedly");
    throw;
}

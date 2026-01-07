using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Integration.Application.Services;
using Integration.Infrastructure.Clients;
using Integration.Infrastructure.Data;
using Integration.Infrastructure.Repositories;
using Integration.Worker;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Configure Serilog early using the configuration from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(Host.CreateApplicationBuilder(args).Configuration)
    .CreateBootstrapLogger(); // Use bootstrap logger initially

Log.Information("Starting up Worker Service...");

try
{

    // Configure Serilog *before* building the host
    var initialBuilder = Host.CreateApplicationBuilder(args);
    var configuration = initialBuilder.Configuration;

    // Apply Serilog configuration using the loaded configuration object
    Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();

    // --- create the host builder and configure services ---
    var builder = Host.CreateApplicationBuilder(args);

    // --- System Database contexts ---
    builder.Services.AddDbContext<SystemDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("SystemDB");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("SystemDB connection string is not configured");
        }

        options.UseSqlServer(
            connectionString,
            sqlOptions =>
            {
                sqlOptions.CommandTimeout(300);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    new[] { 1205, 4060 }
                ); //we need to add other eror codes if needed
            }
        );
    });

    // --- BU DbContext factory ---
    builder.Services.AddScoped<Func<string, BuDbContext>>(provider => buCode =>
    {
        var buHelper = provider.GetRequiredService<BusinessUnitResolveHelper>();
        var connectionString = buHelper.BuildConnectionString(buCode); 

        var optionsBuilder = new DbContextOptionsBuilder<BuDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.CommandTimeout(300);
            sqlOptions.EnableRetryOnFailure( 
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                new[] { 1205, 4060 }); //we need to add other eror codes if needed
        });

        return new BuDbContext(optionsBuilder.Options, buCode);
    });

    // --- SAP HTTP Client ---
    builder.Services.AddHttpClient<ISapClient, SapApiClient>(
        (serviceProvider, client) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var baseUrl = config["SapApi:BaseUrl"];
            var username = config["SapApi:Username"];
            var password = config["SapApi:Password"];

            if (
                string.IsNullOrEmpty(baseUrl)
                || string.IsNullOrEmpty(username)
                || string.IsNullOrEmpty(password)
            )
            {
                throw new InvalidOperationException("SAP API configuration is incomplete.");
            }

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(config.GetValue("SapApi:TimeoutSeconds", 120));

            // Basic authentication
            var authToken = Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes($"{username}:{password}")
            );
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

            // Add default headers
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
            );
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }
    );

    // --- Add Helpers ---
    builder.Services.AddScoped<BusinessUnitResolveHelper>();
    builder.Services.AddScoped<CustomerMappingHelper>();
    builder.Services.AddScoped<MaterialMappingHelper>();

    // --- Add Repositories ---
    builder.Services.AddScoped<IRetailerRepository, RetailerRepository>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IBusinessUnitRepository, BusinessUnitRepository>();
    builder.Services.AddScoped<ILogRepository, LogRepository>();

    // --- Add Services ---
    builder.Services.AddScoped<ICustomerSyncService, CustomerSyncService>();
    builder.Services.AddScoped<IMaterialSyncService, MaterialSyncService>();

    // --- Add Workers ---
    builder.Services.AddHostedService<ResilientBackgroundService>();
    builder.Services.AddHostedService<MaterialSyncBackgroundService>();
    builder.Services.AddHostedService<CustomerSyncBackgroundService>();

    // Register options
    builder.Services.Configure<BackgroundServiceOptions>(
    nameof(CustomerSyncBackgroundService),
    builder.Configuration.GetSection("BackgroundServices:CustomerSyncBackgroundService"));

    builder.Services.Configure<BackgroundServiceOptions>(
        nameof(MaterialSyncBackgroundService),
        builder.Configuration.GetSection("BackgroundServices:MaterialSyncBackgroundService"));

    var host = builder.Build();

    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Worker starting up...");
    logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);

    var configurationService = host.Services.GetRequiredService<IConfiguration>();
    var sapBaseUrl = configurationService["SapApi:BaseUrl"];
    var globalConnString = configurationService.GetConnectionString("GlobalDatabase");

    if (string.IsNullOrWhiteSpace(sapBaseUrl))
    {
        logger.LogError("SAP API BaseUrl is not configured!");
        throw new InvalidOperationException("SAP API BaseUrl is required");
    }

    if (string.IsNullOrWhiteSpace(globalConnString))
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
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred while starting the Worker Service");
}
finally { }

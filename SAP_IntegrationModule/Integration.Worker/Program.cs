using System.Net.Http.Headers;
using System.Text;
using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Integration.Application.Services;
using Integration.Infrastructure.Clients;
using Integration.Infrastructure.Data;
using Integration.Infrastructure.Mock;
using Integration.Infrastructure.Repositories;
using Integration.Worker;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

#region Serilog configuration
// Configure Serilog from appsettings.json
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();

// Replace default logging with Serilog
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(dispose: true);

#endregion

try
{
    Log.Information("Starting Worker Service...");

    #region Database contexts

    builder.Services.AddDbContext<SystemDbContext>(options =>
    {
        var connectionString =
            builder.Configuration.GetConnectionString("SystemDB")
            ?? throw new InvalidOperationException("SystemDB connection string is not configured");

        options.UseSqlServer(
            connectionString,
            sql =>
            {
                sql.CommandTimeout(300);
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: new[] { 1205, 4060 }
                );
            }
        );
    });

    builder.Services.AddDbContext<UserDbContext>(options =>
    {
        var connectionString =
            builder.Configuration.GetConnectionString("UserDB")
            ?? throw new InvalidOperationException("UserDB connection string is not configured");

        options.UseSqlServer(
            connectionString,
            sql =>
            {
                sql.CommandTimeout(300);
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: new[] { 1205, 4060 }
                );
            }
        );
    });

    #endregion

    #region SAP Client (Mock / Real)

    var sapMode = builder.Configuration["SapApi:Mode"];

    if (string.Equals(sapMode, "Mock", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddScoped<ISapClient, MockSapClient>();
    }
    else
    {
        builder.Services.AddHttpClient<ISapClient, SapApiClient>(
            (sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();

                var baseUrl = config["SapApi:BaseUrl"];
                var username = config["SapApi:Username"];
                var password = config["SapApi:Password"];

                if (
                    string.IsNullOrWhiteSpace(baseUrl)
                    || string.IsNullOrWhiteSpace(username)
                    || string.IsNullOrWhiteSpace(password)
                )
                {
                    throw new InvalidOperationException("SAP API configuration is incomplete");
                }

                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(
                    config.GetValue("SapApi:TimeoutSeconds", 120)
                );

                var authToken = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{username}:{password}")
                );

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    authToken
                );

                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json")
                );

                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            }
        );
    }

    #endregion

    #region Helpers

    builder.Services.AddScoped<BusinessUnitResolveHelper>();
    builder.Services.AddScoped<CustomerMappingHelper>();
    builder.Services.AddScoped<MaterialMappingHelper>();
    builder.Services.AddScoped<SalesMappingHelper>();

    #endregion

    #region Repositories

    builder.Services.AddScoped<IRetailerRepository, RetailerRepository>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<ISalesRepository, SalesRepository>();
    builder.Services.AddScoped<IBusinessUnitRepository, BusinessUnitRepository>();
    builder.Services.AddScoped<ILogRepository, LogRepository>();

    #endregion

    #region Services

    builder.Services.AddScoped<ICustomerSyncService, CustomerSyncService>();
    builder.Services.AddScoped<IMaterialSyncService, MaterialSyncService>();
    builder.Services.AddScoped<ISalesSyncService, SalesSyncService>();

    #endregion

    #region Background services

    builder.Services.AddHostedService<CustomerSyncBackgroundService>();
    builder.Services.AddHostedService<MaterialSyncBackgroundService>();
    builder.Services.AddHostedService<SalesSyncBackgroundService>();

    #endregion

    #region Background service options

    builder.Services.Configure<BackgroundServiceOptions>(
        nameof(CustomerSyncBackgroundService),
        builder.Configuration.GetSection("BackgroundServices:CustomerSyncBackgroundService")
    );

    builder.Services.Configure<BackgroundServiceOptions>(
        nameof(MaterialSyncBackgroundService),
        builder.Configuration.GetSection("BackgroundServices:MaterialSyncBackgroundService")
    );

    builder.Services.Configure<BackgroundServiceOptions>(
        nameof(SalesSyncBackgroundService),
        builder.Configuration.GetSection("BackgroundServices:SalesSyncBackgroundService")
    );

    #endregion

    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

    var host = builder.Build();

    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Worker starting");
    logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

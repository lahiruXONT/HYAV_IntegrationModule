using Integration.Api.Middleware;
using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Integration.Application.Services;
using Integration.Infrastructure.Clients;
using Integration.Infrastructure.Data;
using Integration.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

// --- create the application builder
var builder = WebApplication.CreateBuilder(args);

// Configure Serilog 
builder.Host.UseSerilog((context, configuration) =>   configuration.ReadFrom.Configuration(context.Configuration));


// --- add controllers and endpoints ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


// --- Add swagger documentation ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SAP Integration API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


// --- Global Database contexts ---
builder.Services.AddDbContext<GlobalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GlobalDatabase")));

// --- BU DbContext factory ---
builder.Services.AddScoped<Func<string, BuDbContext>>(provider => buCode =>
{
    var buHelper = provider.GetRequiredService<BusinessUnitResolveHelper>();
    var connectionString = buHelper.BuildConnectionString(buCode);

    var optionsBuilder = new DbContextOptionsBuilder<BuDbContext>();
    optionsBuilder.UseSqlServer(connectionString);

    return new BuDbContext(optionsBuilder.Options, buCode);
});

// --- JWT Authentication ---
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("JWT Key is not configured");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});
builder.Services.AddAuthorization();

// --- SAP HTTP Client ---
builder.Services.AddHttpClient<ISapClient, SapApiClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = config["SapApi:BaseUrl"];
    var username = config["SapApi:Username"];
    var password = config["SapApi:Password"];

    if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
    {
        throw new InvalidOperationException("SAP API configuration is incomplete.");
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

// --- Dependency Injection for Helpers ---
builder.Services.AddScoped<CustomerMappingHelper>();
builder.Services.AddScoped<MaterialMappingHelper>();
builder.Services.AddScoped<BusinessUnitResolveHelper>();
builder.Services.AddScoped<PasswordHashHelper>();

// --- Dependency Injection for repositories ---
builder.Services.AddScoped<IBusinessUnitRepository, BusinessUnitRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IRetailerRepository, RetailerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// --- Dependency Injection for services ---
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerSyncService, CustomerSyncService>();
builder.Services.AddScoped<IMaterialSyncService, MaterialSyncService>();

// --- Build the application ---
var app = builder.Build();

// --- Configure the HTTP request pipeline ---
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

// Enable Swagger in development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enforce HTTPS
app.UseHttpsRedirection();
// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();
// Map controller routes
app.MapControllers();
// Simple health check endpoint
app.MapGet("/", () => "SAP Integration API is running");
// Run the application
app.Run();
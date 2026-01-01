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

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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

builder.Services.AddDbContext<GlobalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GlobalDatabase")));

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

builder.Services.AddScoped<CustomerMappingHelper>();
builder.Services.AddScoped<MaterialMappingHelper>();
builder.Services.AddScoped<BusinessUnitResolveHelper>();
builder.Services.AddScoped<PasswordHashHelper>();
builder.Services.AddScoped<IBusinessUnitRepository, BusinessUnitRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRetailerRepository, RetailerRepository>();
builder.Services.AddScoped<ICustomerSyncService, CustomerSyncService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IMaterialSyncService, MaterialSyncService>();

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "SAP Integration API is running");

app.Run();
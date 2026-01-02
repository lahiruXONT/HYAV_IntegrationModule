using Integration.Application.Helpers;
using Integration.Application.Interfaces;
using Integration.Application.Services;
using Integration.Infrastructure.Clients;
using Integration.Infrastructure.Data;
using Integration.Infrastructure.Repositories;
using Integration.Worker;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

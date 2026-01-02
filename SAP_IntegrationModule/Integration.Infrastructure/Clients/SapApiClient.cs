using Azure.Core.Pipeline;
using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Integration.Infrastructure.Clients;

public class SapApiClient : ISapClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SapApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    public SapApiClient(HttpClient httpClient, ILogger<SapApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _retryPolicy = Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(r => (int)r.StatusCode >= 500)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                _logger.LogWarning(
                    "SAP API call failed. Retry {RetryAttempt} after {Delay}ms. Status: {StatusCode}",
                    retryAttempt, timespan.TotalMilliseconds,
                    outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message);
            });
    }

    public async Task<List<SapCustomerResponseDto>> GetCustomerChangesAsync(XontCustomerSyncRequestDto request)
    {
        try

        {
            var queryParams = new Dictionary<string, string>
            {
                ["$filter"] =
                             $"ChangedOn ge datetime'{request.Date}T00:00:00' " +
                             $"or CreatedOn ge datetime'{request.Date}T00:00:00'",
                ["$format"] = "json"
            };
            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            var endpoint = $"/sap/opu/odata/sap/ZCUSTOMER_MASTER_SRV/CustomerSet?{queryString}";
            var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(endpoint));

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var sapResponse = JsonSerializer.Deserialize<SapODataResponse<SapCustomerResponseDto>>(json, _jsonOptions);

            var customers = sapResponse?.D?.Results ?? new List<SapCustomerResponseDto>();

            return customers;


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer data from SAP");
            throw new SapApiExceptionDto($"SAP API call failed: {ex.Message}", ex);
        }
    }

    public async Task<List<SapMaterialResponseDto>> GetMaterialChangesAsync(XontMaterialSyncRequestDto request)
    {
        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["$filter"] = $"ChangedOn ge datetime'{request.Date}T00:00:00' " +
                             $"or CreatedOn ge datetime'{request.Date}T00:00:00'",
                ["$format"] = "json"
            };
            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            var endpoint = $"/sap/opu/odata/sap/ZMATERIAL_MASTER_SRV/MaterialSet?{queryString}";
            var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(endpoint));

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var sapResponse = JsonSerializer.Deserialize<SapODataResponse<SapMaterialResponseDto>>(json, _jsonOptions);

            var materials = sapResponse?.D?.Results ?? new List<SapMaterialResponseDto>();

            return materials;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching material data from SAP");
            throw new SapApiExceptionDto($"SAP Material API call failed: {ex.Message}", ex);
        }
    }
}
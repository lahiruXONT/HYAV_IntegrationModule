using System.Text;
using System.Text.Json;
using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;

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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => (int)r.StatusCode >= 500)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    _logger.LogWarning(
                        "SAP API call failed. Retry {RetryAttempt} after {Delay}ms. Status: {StatusCode}",
                        retryAttempt,
                        timespan.TotalMilliseconds,
                        outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.Message
                    );
                }
            );
    }

    public async Task<List<SapCustomerResponseDto>> GetCustomerChangesAsync(
        XontCustomerSyncRequestDto request
    )
    {
        try
        {
            //var queryParams = new Dictionary<string, string>
            //{
            //    ["$filter"] =
            //        $"ChangedOn ge datetime'{request.Date}T00:00:00' "
            //        + $"or CreatedOn ge datetime'{request.Date}T00:00:00'",
            //    ["$format"] = "json",
            //};
            //var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            //var endpoint = $"/sap/opu/odata/sap/ZCUSTOMER_MASTER_SRV/CustomerSet?{queryString}";

            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );
            var endpoint = $"/sap/opu/odata/sap/ZCUSTOMER_MASTER_SRV/CustomerSet";

            var response = await _retryPolicy.ExecuteAsync(() =>
                _httpClient.PostAsync(endpoint, content)
            );
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var sapResponse = JsonSerializer.Deserialize<SapODataResponse<SapCustomerResponseDto>>(
                json,
                _jsonOptions
            );

            var customers = sapResponse?.D?.Results ?? new List<SapCustomerResponseDto>();

            return customers;
        }
        catch (Exception ex)
        {
            throw new SapApiExceptionDto(
                $"SAP Customer API call failed"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    ),
                ex
            );
        }
    }

    public async Task<List<SapMaterialResponseDto>> GetMaterialChangesAsync(
        XontMaterialSyncRequestDto request
    )
    {
        try
        {
            //var queryParams = new Dictionary<string, string>
            //{
            //    ["$filter"] =
            //        $"ChangedOn ge datetime'{request.Date}T00:00:00' "
            //        + $"or CreatedOn ge datetime'{request.Date}T00:00:00'",
            //    ["$format"] = "json",
            //};
            //var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            //var endpoint = $"/sap/opu/odata/sap/ZMATERIAL_MASTER_SRV/MaterialSet?{queryString}";

            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );
            var endpoint = $"/sap/opu/odata/sap/ZMATERIAL_MASTER_SRV/MaterialSet";

            var response = await _retryPolicy.ExecuteAsync(() =>
                _httpClient.PostAsync(endpoint, content)
            );
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var sapResponse = JsonSerializer.Deserialize<SapODataResponse<SapMaterialResponseDto>>(
                json,
                _jsonOptions
            );

            var materials = sapResponse?.D?.Results ?? new List<SapMaterialResponseDto>();

            return materials;
        }
        catch (Exception ex)
        {
            throw new SapApiExceptionDto(
                $"SAP Material API call failed"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    ),
                ex
            );
        }
    }

    public async Task<SapSalesOrderResponseDto> SendSalesOrderAsync(SalesOrderRequestDto dto)
    {
        var endpoint = "/sap/opu/odata/sap/ZSALES_ORDER_SRV/SalesOrderSet";
        var content = new StringContent(
            JsonSerializer.Serialize(dto, _jsonOptions),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _retryPolicy.ExecuteAsync(() =>
            _httpClient.PostAsync(endpoint, content)
        );
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SapSalesOrderResponseDto>(json, _jsonOptions);
    }

    public async Task<StockOutSapResponseDto> GetStockOutTransactionDetails(
        StockOutSapRequestDto dto
    )
    {
        var endpoint = "/sap/opu/odata/sap/StockOutSet";
        var content = new StringContent(
            JsonSerializer.Serialize(dto, _jsonOptions),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _retryPolicy.ExecuteAsync(() =>
            _httpClient.PostAsync(endpoint, content)
        );
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<StockOutSapResponseDto>(json, _jsonOptions);
    }

    public async Task<List<GetMaterialStockFromSapResponseDto>> GetLocationStockDetails(
        GetMaterialStockFromSapRequestDto dto
    )
    {
        var endpoint = "/sap/opu/odata/sap/MaterialStock";
        var content = new StringContent(
            JsonSerializer.Serialize(dto, _jsonOptions),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _retryPolicy.ExecuteAsync(() =>
            _httpClient.PostAsync(endpoint, content)
        );
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<GetMaterialStockFromSapResponseDto>>(
            json,
            _jsonOptions
        );
    }

    public async Task<SAPReceiptResponseDto> SendReceiptAsync(ReceiptRequestDto request)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );
            var endpoint = $"/sap/opu/odata/sap/ZReceipt/ReceiptSet";

            var response = await _retryPolicy.ExecuteAsync(() =>
                _httpClient.PostAsync(endpoint, content)
            );
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var sapResponse = JsonSerializer.Deserialize<SapODataResponse<SAPReceiptResponseDto>>(
                json,
                _jsonOptions
            );

            return sapResponse.D.Results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            throw new SapApiExceptionDto(
                $"SAP receipt API call failed"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    ),
                ex
            );
        }
    }

    public async Task<SapMaterialStockSyncResponseDto> GetMaterialStockAsync(
        XontMaterialStockSyncRequestDto request
    )
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );
            var endpoint = $"/sap/opu/odata/sap/ZMATERIAL_STOCK/MaterialStockSet";

            var response = await _retryPolicy.ExecuteAsync(() =>
                _httpClient.PostAsync(endpoint, content)
            );
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var sapResponse = JsonSerializer.Deserialize<SapMaterialStockSyncResponseDto>(
                json,
                _jsonOptions
            );

            return sapResponse;
        }
        catch (Exception ex)
        {
            throw new SapApiExceptionDto(
                $"SAP Material stock API call failed"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    ),
                ex
            );
        }
    }

    public async Task<SapInvoiceResponseDto> GetInvoiceDataAsync(SAPInvoiceSyncRequestDto request)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );
            var endpoint = $"/sap/opu/odata/sap/ZMATERIAL_STOCK/InvoiceSet";

            var response = await _retryPolicy.ExecuteAsync(() =>
                _httpClient.PostAsync(endpoint, content)
            );
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var sapResponse = JsonSerializer.Deserialize<SapInvoiceResponseDto>(json, _jsonOptions);

            return sapResponse;
        }
        catch (Exception ex)
        {
            throw new SapApiExceptionDto(
                $"SAP Invoice API call failed"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    ),
                ex
            );
        }
    }

    public async Task<StockInSapResponseDto> SendStockInAsync(StockInSapRequestDto request)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json"
            );
            var endpoint = $"/sap/opu/odata/sap/ZStockIn/StockInSet";

            var response = await _retryPolicy.ExecuteAsync(() =>
                _httpClient.PostAsync(endpoint, content)
            );
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var sapResponse = JsonSerializer.Deserialize<SapODataResponse<StockInSapResponseDto>>(
                json,
                _jsonOptions
            );

            return sapResponse.D.Results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            throw new SapApiExceptionDto(
                $"SAP stock in API call failed"
                    + (string.IsNullOrWhiteSpace(ex.Message) ? "" : $": {ex.Message}")
                    + (
                        !string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                            ? $"; {ex.InnerException.Message}"
                            : ""
                    ),
                ex
            );
        }
    }
}

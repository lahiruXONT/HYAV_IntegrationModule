using System.ComponentModel.DataAnnotations;
using Integration.Application.DTOs;
using Integration.Application.Interfaces;
using Integration.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Integration.Application.Helpers;

public sealed class CustomerMappingHelper
{
    private readonly BusinessUnitResolveHelper _businessUnitResolver;
    private readonly ILogger<CustomerMappingHelper> _logger;
    private readonly IRetailerRepository _customerRepository;

    public CustomerMappingHelper(
        ILogger<CustomerMappingHelper> logger,
        BusinessUnitResolveHelper businessUnitResolver,
        IRetailerRepository retailerRepository
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _businessUnitResolver =
            businessUnitResolver ?? throw new ArgumentNullException(nameof(businessUnitResolver));
        _customerRepository =
            retailerRepository ?? throw new ArgumentNullException(nameof(retailerRepository));
    }

    public async Task<Retailer> MapSapToXontCustomerAsync(SapCustomerResponseDto sapCustomer)
    {
        await ValidateSapCustomerAsync(sapCustomer);

        try
        {
            var territory = await _customerRepository.GetTerritoryCodeAsync(
                sapCustomer.PostalCode ?? string.Empty
            );

            var businessUnit = await _businessUnitResolver.ResolveBusinessUnitAsync(
                sapCustomer.SalesOrganization ?? string.Empty,
                sapCustomer.Division ?? string.Empty
            );

            var retailer = new Retailer
            {
                RetailerCode = sapCustomer?.Customer?.Trim() ?? string.Empty,
                RetailerName = sapCustomer?.CustomerName?.Trim() ?? string.Empty,
                AddressLine1 = sapCustomer?.HouseNo?.Trim() ?? string.Empty,
                AddressLine2 = sapCustomer?.Street?.Trim() ?? string.Empty,
                AddressLine3 = sapCustomer?.Street2?.Trim() ?? string.Empty,
                AddressLine4 = sapCustomer?.Street3?.Trim() ?? string.Empty,
                AddressLine5 = sapCustomer?.City?.Trim() ?? string.Empty,
                TelephoneNumber = sapCustomer?.Telephone?.Trim() ?? string.Empty,
                FaxNumber = sapCustomer?.Fax?.Trim() ?? string.Empty,
                EmailAddress = sapCustomer?.Email?.Trim() ?? string.Empty,
                SettlementTermsCode = sapCustomer?.PaymentTerm?.Trim() ?? string.Empty,
                CreditLimit = sapCustomer?.CreditLimit ?? 0m,
                VatRegistrationNo = sapCustomer?.VATRegistrationNumber?.Trim() ?? string.Empty,
                BusinessUnit = businessUnit ?? string.Empty,
                TerritoryCode = territory?.TerritoryCode?.Trim() ?? string.Empty,

                //Division =  sapCustomer.Division?.Trim(),
                //SalesOrganization = sapCustomer.SalesOrganization?.Trim(),
                DistributionChannel = sapCustomer?.Distributionchannel ?? string.Empty,

                // Default values
                PricingMethod = string.Empty,
                PriceGroup = string.Empty,
                TradeSchemeGroup = string.Empty,
                SalesOperationType = string.Empty,

                TelephoneNumberSys = string.Empty,
                ContactName = string.Empty,
                PaymentMethodCode = "CA",
                OnStopFlag = "0",
                VatCode = string.IsNullOrWhiteSpace(sapCustomer?.VATRegistrationNumber)
                    ? string.Empty
                    : "V1",
                VatStatus = string.IsNullOrWhiteSpace(sapCustomer?.VATRegistrationNumber)
                    ? string.Empty
                    : "1",
                PostCode = "0000",
                CurrencyCode = "LKR",
                CurrencyProcessingRequired = "1",
                Status = "1",

                RetailerTypeCode = !string.IsNullOrEmpty(sapCustomer?.CustomerGroup1)
                    ? sapCustomer.CustomerGroup1.Trim()
                    : "",
                RetailerClassCode = !string.IsNullOrEmpty(sapCustomer?.CustomerGroup2)
                    ? sapCustomer.CustomerGroup2.Trim()
                    : "",
                RetailerCategoryCode = !string.IsNullOrEmpty(sapCustomer?.CustomerGroup3)
                    ? sapCustomer.CustomerGroup3.Trim()
                    : "",

                // Audit fields
                CreatedOn = DateTime.Now,
                UpdatedOn = ParseSapDate(sapCustomer?.TodaysDate),
                CreatedBy = "SAP_SYNC",
                UpdatedBy = "SAP_SYNC",
            };

            ValidateRetailer(retailer);
            return retailer;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to map SAP customer {Code}. SalesOrg: {SalesOrg}, Division: {Division}",
                sapCustomer.Customer,
                sapCustomer.SalesOrganization,
                sapCustomer.Division
            );
            throw;
        }
    }

    private async Task ValidateSapCustomerAsync(SapCustomerResponseDto sapCustomer)
    {
        if (sapCustomer == null)
            throw new ValidationExceptionDto("SAP customer data cannot be null");

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(sapCustomer.Customer))
            errors.Add("Customer code is required");

        if (sapCustomer.Customer?.Length > 15)
            errors.Add($"Customer code exceeds 15 characters: {sapCustomer.Customer}");

        if (string.IsNullOrWhiteSpace(sapCustomer.CustomerName))
            errors.Add("Customer name is required");

        if (sapCustomer.CustomerName?.Length > 75)
            errors.Add($"Customer name exceeds 75 characters: {sapCustomer.CustomerName}");

        if (string.IsNullOrWhiteSpace(sapCustomer.SalesOrganization))
            errors.Add("Sales organization is required");

        if (string.IsNullOrWhiteSpace(sapCustomer.Division))
            errors.Add("Division is required");

        if (
            !string.IsNullOrWhiteSpace(sapCustomer.Division)
            && !string.IsNullOrWhiteSpace(sapCustomer.SalesOrganization)
        )
        {
            var exists = await _businessUnitResolver.SalesOrgDivisionExistsAsync(
                sapCustomer.SalesOrganization,
                sapCustomer.Division
            );

            if (!exists)
            {
                errors.Add(
                    $"Business unit not found for SalesOrg: '{sapCustomer.SalesOrganization}' Division: '{sapCustomer.Division}'"
                );
            }
        }

        if (string.IsNullOrWhiteSpace(sapCustomer.PostalCode))
        {
            errors.Add("Postal Code is required");
        }
        else if (!await _customerRepository.PostalCodeTerritoryExistsAsync(sapCustomer.PostalCode))
        {
            var errorMessage =
                $"No territory found for postal code: '{sapCustomer.PostalCode}' for customer '{sapCustomer.Customer}'";
        }

        if (string.IsNullOrWhiteSpace(sapCustomer.HouseNo))
            errors.Add("House No is required");

        if (string.IsNullOrWhiteSpace(sapCustomer.PaymentTerm))
            errors.Add("Payment Term is required");

        if (errors.Any())
        {
            var errorMessage = string.Join("; ", errors);
            throw new ValidationExceptionDto(errorMessage);
        }
    }

    private void ValidateRetailer(Retailer retailer)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(retailer);

        if (!Validator.TryValidateObject(retailer, validationContext, validationResults, true))
        {
            var errors = validationResults.Select(r => r.ErrorMessage);
            throw new ValidationExceptionDto(
                $"Retailer validation failed: {string.Join("; ", errors)}"
            );
        }
    }

    public async Task<(bool retailerChanged, bool geoClassificationChanged)> HasRetailerChanges(
        Retailer existing,
        Retailer updated,
        string postalCode
    )
    {
        if (existing == null)
            throw new ArgumentNullException(nameof(existing));
        if (updated == null)
            throw new ArgumentNullException(nameof(updated));

        bool retailerChanged =
            !string.Equals(
                existing.RetailerCode?.Trim(),
                updated.RetailerCode?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.RetailerName?.Trim(),
                updated.RetailerName?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.AddressLine1?.Trim(),
                updated.AddressLine1?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.AddressLine2?.Trim(),
                updated.AddressLine2?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.AddressLine3?.Trim(),
                updated.AddressLine3?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.AddressLine4?.Trim(),
                updated.AddressLine4?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.AddressLine5?.Trim(),
                updated.AddressLine5?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.TelephoneNumber?.Trim(),
                updated.TelephoneNumber?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.FaxNumber?.Trim(),
                updated.FaxNumber?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.EmailAddress?.Trim(),
                updated.EmailAddress?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.SettlementTermsCode?.Trim(),
                updated.SettlementTermsCode?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || existing.CreditLimit != updated.CreditLimit
            || !string.Equals(
                existing.TerritoryCode?.Trim(),
                updated.TerritoryCode?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.DistributionChannel?.Trim(),
                updated.DistributionChannel?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.VatRegistrationNo?.Trim(),
                updated.VatRegistrationNo?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.VatCode?.Trim(),
                updated.VatCode?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.VatStatus?.Trim(),
                updated.VatStatus?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.RetailerTypeCode?.Trim(),
                updated.RetailerTypeCode?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.RetailerClassCode?.Trim(),
                updated.RetailerClassCode?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.RetailerCategoryCode?.Trim(),
                updated.RetailerCategoryCode?.Trim(),
                StringComparison.OrdinalIgnoreCase
            );

        string retailerTown =
            await _customerRepository.GetCurrentPostalCodeForRetailerAsync(
                existing.BusinessUnit ?? "",
                existing.RetailerCode ?? ""
            ) ?? string.Empty;

        bool geoClassificationChanged = !string.Equals(
            postalCode?.Trim(),
            retailerTown?.Trim(),
            StringComparison.OrdinalIgnoreCase
        );

        return (retailerChanged, geoClassificationChanged);
    }

    public void UpdateCustomer(Retailer existing, Retailer updated)
    {
        if (existing == null)
            throw new ArgumentNullException(nameof(existing));
        if (updated == null)
            throw new ArgumentNullException(nameof(updated));

        existing.RetailerName = updated.RetailerName;
        existing.AddressLine1 = updated.AddressLine1;
        existing.AddressLine2 = updated.AddressLine2;
        existing.AddressLine3 = updated.AddressLine3;
        existing.AddressLine4 = updated.AddressLine4;
        existing.AddressLine5 = updated.AddressLine5;
        existing.TelephoneNumber = updated.TelephoneNumber;
        existing.FaxNumber = updated.FaxNumber;
        existing.EmailAddress = updated.EmailAddress;
        existing.SettlementTermsCode = updated.SettlementTermsCode;
        existing.CreditLimit = updated.CreditLimit;
        existing.TerritoryCode = updated.TerritoryCode;
        existing.DistributionChannel = updated.DistributionChannel;
        existing.VatRegistrationNo = updated.VatRegistrationNo;
        existing.VatCode = updated.VatCode;
        existing.VatStatus = updated.VatStatus;
        existing.RetailerTypeCode = updated.RetailerTypeCode;
        existing.RetailerClassCode = updated.RetailerClassCode;
        existing.RetailerCategoryCode = updated.RetailerCategoryCode;

        existing.UpdatedOn = DateTime.Now;
        existing.UpdatedBy = "SAP_SYNC";
    }

    public async Task<GlobalRetailer> MapSapToXontGlobalCustomerAsync(
        SapCustomerResponseDto sapCustomer
    )
    {
        try
        {
            var businessUnit = await _businessUnitResolver.ResolveBusinessUnitAsync(
                sapCustomer.SalesOrganization ?? "",
                sapCustomer.Division ?? ""
            );

            return new GlobalRetailer
            {
                RetailerCode = sapCustomer?.Customer?.Trim() ?? string.Empty,
                RetailerName = sapCustomer?.CustomerName?.Trim() ?? string.Empty,
                AddressLine1 = sapCustomer?.HouseNo?.Trim() ?? string.Empty,
                AddressLine2 = sapCustomer?.Street?.Trim() ?? string.Empty,
                AddressLine3 = sapCustomer?.Street2?.Trim() ?? string.Empty,
                AddressLine4 = sapCustomer?.Street3?.Trim() ?? string.Empty,
                AddressLine5 = sapCustomer?.City?.Trim() ?? string.Empty,
                TelephoneNumber = sapCustomer?.Telephone?.Trim() ?? string.Empty,
                FaxNumber = sapCustomer?.Fax?.Trim() ?? string.Empty,
                EmailAddress = sapCustomer?.Email?.Trim() ?? string.Empty,
                // Default values

                TelephoneNumberSys = string.Empty,
                PostCode = "0000",
                CurrencyCode = "LKR",
                CurrencyProcessingRequired = "1",

                // Audit fields
                CreatedOn = DateTime.Now,
                UpdatedOn = ParseSapDate(sapCustomer?.TodaysDate),
                CreatedBy = "SAP_SYNC",
                UpdatedBy = "SAP_SYNC",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to map SAP customer {Code}. SalesOrg: {SalesOrg}, Division: {Division}",
                sapCustomer.Customer,
                sapCustomer.SalesOrganization,
                sapCustomer.Division
            );
            throw;
        }
    }

    public bool HasGlobalRetailerChanges(GlobalRetailer existing, GlobalRetailer updated)
    {
        if (existing == null)
            throw new ArgumentNullException(nameof(existing));
        if (updated == null)
            throw new ArgumentNullException(nameof(updated));

        return !string.Equals(
                existing.RetailerCode?.Trim(),
                updated.RetailerCode?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.RetailerName?.Trim(),
                updated.RetailerName?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.AddressLine1?.Trim(),
                updated.AddressLine1?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.AddressLine2?.Trim(),
                updated.AddressLine2?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.AddressLine3?.Trim(),
                updated.AddressLine3?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.AddressLine4?.Trim(),
                updated.AddressLine4?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.AddressLine5?.Trim(),
                updated.AddressLine5?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.TelephoneNumber?.Trim(),
                updated.TelephoneNumber?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.FaxNumber?.Trim(),
                updated.FaxNumber?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.EmailAddress?.Trim(),
                updated.EmailAddress?.Trim(),
                StringComparison.OrdinalIgnoreCase
            );
    }

    public void UpdateGlobalCustomer(GlobalRetailer existing, GlobalRetailer updated)
    {
        if (existing == null)
            throw new ArgumentNullException(nameof(existing));
        if (updated == null)
            throw new ArgumentNullException(nameof(updated));

        existing.RetailerName = updated.RetailerName;
        existing.AddressLine1 = updated.AddressLine1;
        existing.AddressLine2 = updated.AddressLine2;
        existing.AddressLine3 = updated.AddressLine3;
        existing.AddressLine4 = updated.AddressLine4;
        existing.AddressLine5 = updated.AddressLine5;
        existing.TelephoneNumber = updated.TelephoneNumber;
        existing.FaxNumber = updated.FaxNumber;
        existing.EmailAddress = updated.EmailAddress;

        existing.UpdatedOn = DateTime.Now;
        existing.UpdatedBy = "SAP_SYNC";
    }

    private DateTime ParseSapDate(string? sapDate)
    {
        if (string.IsNullOrEmpty(sapDate))
            return DateTime.Now;

        try
        {
            if (
                DateTime.TryParseExact(
                    sapDate,
                    "yyyyMMdd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var result
                )
            )
            {
                return result;
            }

            if (
                DateTime.TryParseExact(
                    sapDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out result
                )
            )
            {
                return result;
            }

            _logger.LogWarning("Failed to parse SAP date: {Date}, using current date", sapDate);
            return DateTime.Now;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing SAP date: {Date}, using current date", sapDate);
            return DateTime.Now;
        }
    }
}

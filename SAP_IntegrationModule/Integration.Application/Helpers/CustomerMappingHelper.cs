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
            var businessUnit = await _businessUnitResolver.ResolveBusinessUnitAsync(
                sapCustomer.SalesOrganization ?? string.Empty,
                sapCustomer.Division ?? string.Empty
            );

            var territory = await _customerRepository.GetTerritoryCodeAsync(
                sapCustomer.PostalCode ?? string.Empty
            );

            if (territory == null || string.IsNullOrWhiteSpace(territory.TerritoryCode))
            {
                var errorMessage =
                    $"No territory found for postal code: '{sapCustomer.PostalCode}' for customer '{sapCustomer.Customer}'";
                _logger.LogWarning(errorMessage);
            }

            var retailer = new Retailer
            {
                RetailerCode = sapCustomer.Customer.Trim(),
                RetailerName = sapCustomer.CustomerName.Trim(),
                AddressLine1 = sapCustomer.HouseNo.Trim(),
                AddressLine2 = sapCustomer.Street.Trim(),
                AddressLine3 = sapCustomer.Street2.Trim(),
                AddressLine4 = sapCustomer.Street3.Trim(),
                AddressLine5 = sapCustomer.City.Trim(),
                TelephoneNumber = sapCustomer.Telephone.Trim(),
                FaxNumber = sapCustomer.Fax.Trim(),
                EmailAddress = sapCustomer.Email.Trim(),
                SettlementTermsCode = sapCustomer.PaymentTerm.Trim(),
                CreditLimit = sapCustomer.CreditLimit,

                VatRegistrationNo = sapCustomer.VATRegistrationNumber?.Trim() ?? "",
                BusinessUnit = businessUnit,
                TerritoryCode = territory?.TerritoryCode ?? "",
                //Division =  sapCustomer.Division?.Trim(),
                //SalesOrganization = sapCustomer.SalesOrganization?.Trim(),
                //DistributionChannel = sapCustomer?.Distributionchannel ,

                // Default values
                //Province
                //District = sapCustomer.RegionCode?.Trim(),
                //Town = sapCustomer.PostalCode?.Trim(),
                TelephoneNumberSys = string.Empty,
                ContactName = string.Empty,
                PaymentMethodCode = "CA",
                OnStopFlag = "0",
                VatCode = string.IsNullOrWhiteSpace(sapCustomer.VATRegistrationNumber)
                    ? string.Empty
                    : "V1",
                VatStatus = string.IsNullOrWhiteSpace(sapCustomer.VATRegistrationNumber)
                    ? string.Empty
                    : "1",
                PostCode = "0000",
                CurrencyCode = "LKR",
                CurrencyProcessingRequired = "1",
                Status = "1",

                RetailerTypeCode = !string.IsNullOrEmpty(sapCustomer.CustomerGroup1)
                    ? sapCustomer.CustomerGroup1.Trim()
                    : "",
                RetailerClassCode = !string.IsNullOrEmpty(sapCustomer.CustomerGroup2)
                    ? sapCustomer.CustomerGroup2.Trim()
                    : "",
                RetailerCategoryCode = !string.IsNullOrEmpty(sapCustomer.CustomerGroup3)
                    ? sapCustomer.CustomerGroup3.Trim()
                    : "",

                // Audit fields
                CreatedOn = DateTime.Now,
                UpdatedOn = ParseSapDate(sapCustomer.TodaysDate),
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

    public bool HasRetailerChanges(Retailer existing, Retailer updated)
    {
        if (existing == null)
            throw new ArgumentNullException(nameof(existing));
        if (updated == null)
            throw new ArgumentNullException(nameof(updated));

        return existing.RetailerName != updated.RetailerName
            || existing.AddressLine1 != updated.AddressLine1
            || existing.AddressLine2 != updated.AddressLine2
            || existing.AddressLine3 != updated.AddressLine3
            || existing.AddressLine4 != updated.AddressLine4
            || existing.AddressLine5 != updated.AddressLine5
            || existing.TelephoneNumber != updated.TelephoneNumber
            || existing.FaxNumber != updated.FaxNumber
            || existing.EmailAddress != updated.EmailAddress
            || existing.SettlementTermsCode != updated.SettlementTermsCode
            || existing.CreditLimit != updated.CreditLimit
            || existing.TerritoryCode != updated.TerritoryCode
            //|| existing.SalesOrganization != updated.SalesOrganization ||
            //existing.Division != updated.Division
            || existing.VatRegistrationNo != updated.VatRegistrationNo
            || existing.RetailerTypeCode != updated.RetailerTypeCode
            || existing.RetailerClassCode != updated.RetailerClassCode
            || existing.RetailerCategoryCode != updated.RetailerCategoryCode
        //|| existing.District != updated.District ||
        //existing.Town != updated.Town
        ;
    }

    public void UpdateCustomer(Retailer existing, Retailer updated)
    {
        if (existing == null)
            throw new ArgumentNullException(nameof(existing));
        if (updated == null)
            throw new ArgumentNullException(nameof(updated));

        existing.TerritoryCode = updated.TerritoryCode;
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
        //existing.SalesOrganization = updated.SalesOrganization;
        //existing.Division = updated.Division;
        existing.VatRegistrationNo = updated.VatRegistrationNo;
        existing.RetailerTypeCode = updated.RetailerTypeCode;
        existing.RetailerClassCode = updated.RetailerClassCode;
        existing.RetailerCategoryCode = updated.RetailerCategoryCode;
        //existing.District = updated.District;
        //existing.Town = updated.Town;

        existing.UpdatedOn = DateTime.Now;
        existing.UpdatedBy = "SAP_SYNC";
    }

    public async Task<GlobalRetailer> MapSapToXontGlobalCustomerAsync(
        SapCustomerResponseDto sapCustomer
    )
    {
        await ValidateSapCustomerAsync(sapCustomer);

        try
        {
            var businessUnit = await _businessUnitResolver.ResolveBusinessUnitAsync(
                sapCustomer.SalesOrganization ?? "",
                sapCustomer.Division ?? ""
            );

            var territory = await _customerRepository.GetTerritoryCodeAsync(
                sapCustomer.PostalCode ?? ""
            );
            if (territory == null || string.IsNullOrWhiteSpace(territory.TerritoryCode))
            {
                var errorMessage =
                    $"No territory found for postal code: '{sapCustomer.PostalCode}'";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return new GlobalRetailer
            {
                RetailerCode = sapCustomer.Customer.Trim(),
                RetailerName = sapCustomer.CustomerName.Trim(),
                AddressLine1 = sapCustomer.HouseNo.Trim(),
                AddressLine2 = sapCustomer.Street.Trim(),
                AddressLine3 = sapCustomer.Street2.Trim(),
                AddressLine4 = sapCustomer.Street3.Trim(),
                AddressLine5 = sapCustomer.City.Trim(),
                TelephoneNumber = sapCustomer.Telephone.Trim(),
                FaxNumber = sapCustomer.Fax.Trim(),
                EmailAddress = sapCustomer.Email.Trim(),
                TerritoryCode = territory?.TerritoryCode ?? "",
                //Division =  sapCustomer.Division?.Trim(),
                //SalesOrganization = sapCustomer.SalesOrganization?.Trim(),
                //DistributionChannel = sapCustomer?.Distributionchannel ,

                // Default values
                //Province
                //District = sapCustomer.RegionCode?.Trim(),
                //Town = sapCustomer.PostalCode?.Trim(),

                TelephoneNumberSys = string.Empty,

                PostCode = "0000",
                CurrencyCode = "LKR",
                CurrencyProcessingRequired = "1",

                // Audit fields
                CreatedOn = DateTime.Now,
                UpdatedOn = ParseSapDate(sapCustomer.TodaysDate),
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

        return existing.RetailerName != updated.RetailerName
            || existing.AddressLine1 != updated.AddressLine1
            || existing.AddressLine2 != updated.AddressLine2
            || existing.AddressLine3 != updated.AddressLine3
            || existing.AddressLine4 != updated.AddressLine4
            || existing.AddressLine5 != updated.AddressLine5
            || existing.TelephoneNumber != updated.TelephoneNumber
            || existing.FaxNumber != updated.FaxNumber
            || existing.EmailAddress != updated.EmailAddress
            || existing.TerritoryCode != updated.TerritoryCode
        //|| existing.SalesOrganization != updated.SalesOrganization ||
        //existing.Division != updated.Division
        //|| existing.District != updated.District
        ;
    }

    public void UpdateGlobalCustomer(GlobalRetailer existing, GlobalRetailer updated)
    {
        if (existing == null)
            throw new ArgumentNullException(nameof(existing));
        if (updated == null)
            throw new ArgumentNullException(nameof(updated));

        existing.TerritoryCode = updated.TerritoryCode;
        existing.RetailerName = updated.RetailerName;
        existing.AddressLine1 = updated.AddressLine1;
        existing.AddressLine2 = updated.AddressLine2;
        existing.AddressLine3 = updated.AddressLine3;
        existing.AddressLine4 = updated.AddressLine4;
        existing.AddressLine5 = updated.AddressLine5;
        existing.TelephoneNumber = updated.TelephoneNumber;
        existing.FaxNumber = updated.FaxNumber;
        existing.EmailAddress = updated.EmailAddress;
        //existing.SalesOrganization = updated.SalesOrganization;
        //existing.Division = updated.Division;
        //existing.District = updated.District;

        existing.UpdatedOn = DateTime.Now;
        existing.UpdatedBy = "SAP_SYNC";
    }

    private string NormalizeString(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var trimmed = input.Trim();
        return trimmed.Length > maxLength ? trimmed.Substring(0, maxLength) : trimmed;
    }

    private DateTime ParseSapDate(string sapDate)
    {
        if (string.IsNullOrEmpty(sapDate))
            return DateTime.UtcNow;

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
            return DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing SAP date: {Date}, using current date", sapDate);
            return DateTime.UtcNow;
        }
    }

    // ... rest of the existing methods ...
}

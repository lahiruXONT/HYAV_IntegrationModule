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
        var businessUnit = await ValidateSapCustomerAndGetBusinessUnitAsync(sapCustomer);

        try
        {
            var settlementTerms = await _customerRepository.GetSettlementTermAsync(
                businessUnit,
                sapCustomer.PaymentTerm
            );
            var territoryDefault = await _customerRepository.GetTerritoryDefaultAsync(
                businessUnit,
                sapCustomer.SalesOffice
            );

            var retailer = new Retailer
            {
                RetailerCode = sapCustomer?.Customer?.Trim() ?? string.Empty,
                RetailerName = ApplySapValueSafe(sapCustomer.CustomerName, string.Empty),
                AddressLine1 = ApplySapValueSafe(sapCustomer.HouseNo, string.Empty),
                SAPAddressLine2 = ApplySapValueSafe(sapCustomer.Street, string.Empty),
                AddressLine3 = ApplySapValueSafe(sapCustomer.Street2, string.Empty),
                AddressLine4 = ApplySapValueSafe(sapCustomer.Street3, string.Empty),
                AddressLine5 = ApplySapValueSafe(sapCustomer.City, string.Empty),
                TelephoneNumber = ApplySapValueSafe(sapCustomer.Telephone, string.Empty),
                FaxNumber = ApplySapValueSafe(sapCustomer.Fax, string.Empty),
                SAPEmailAddress = ApplySapValueSafe(sapCustomer.Email, string.Empty),
                SAPSettlementTermsCode = ApplySapValueSafe(sapCustomer.PaymentTerm, string.Empty),
                SettlementTermsCode = ApplySapValueSafe(
                    settlementTerms?.SettlementTermsCode,
                    string.Empty
                ),
                CreditLimit = sapCustomer?.CreditLimit ?? 0m,
                SAPVatRegistrationNo = ApplySapValueSafe(
                    sapCustomer.VATRegistrationNumber,
                    string.Empty
                ),
                BusinessUnit = businessUnit,
                TerritoryCode = ApplySapValueSafe(sapCustomer.SalesOffice, string.Empty),

                // Default values
                PricingMethod = string.Empty,
                PriceGroup = territoryDefault?.PriceGroup ?? "",
                TradeSchemeGroup = territoryDefault?.TradeSchemeGroup ?? "",
                SalesOperationType = "2",

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

                RetailerTypeCode = ApplySapValueSafe(sapCustomer.CustomerGroup1, string.Empty),
                RetailerClassCode = ApplySapValueSafe(sapCustomer.CustomerGroup2, string.Empty),
                RetailerCategoryCode = ApplySapValueSafe(sapCustomer.CustomerGroup3, string.Empty),

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

    private async Task<string> ValidateSapCustomerAndGetBusinessUnitAsync(
        SapCustomerResponseDto sapCustomer
    )
    {
        if (sapCustomer == null)
            throw new ValidationExceptionDto("SAP customer data cannot be null");

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(sapCustomer.Customer))
            errors.Add("Customer code is required");
        else if (sapCustomer.Customer.Length > 15)
            errors.Add($"Customer code exceeds 15 characters: {sapCustomer.Customer}");

        if (string.IsNullOrWhiteSpace(sapCustomer.CustomerName))
            errors.Add("Customer name is required");
        else if (sapCustomer.CustomerName.Length > 75)
            errors.Add($"Customer name exceeds 75 characters: {sapCustomer.CustomerName}");

        var result = await _businessUnitResolver.TryResolveBusinessUnitAsync(
            sapCustomer.SalesOrganization,
            sapCustomer.Division
        );

        if (!result.IsValid)
        {
            errors.Add(result.Error);
            throw new ValidationExceptionDto(string.Join("; ", errors));
        }

        var businessUnit = result.BusinessUnit;

        if (string.IsNullOrWhiteSpace(sapCustomer.SalesOffice))
            errors.Add("Sales Office is required");
        else if (
            !await _customerRepository.TerritoryExistsAsync(businessUnit, sapCustomer.SalesOffice)
        )
        {
            errors.Add(
                $"No Territory '{sapCustomer.SalesOffice}' exists for Business Unit '{businessUnit}'"
            );
        }

        if (string.IsNullOrWhiteSpace(sapCustomer.PostalCode))
        {
            errors.Add("Postal Code is required");
        }
        else if (
            !await _customerRepository.PostalCodeExistsForTownAsync(
                businessUnit,
                sapCustomer.PostalCode
            )
        )
        {
            errors.Add(
                $"No postal code '{sapCustomer.PostalCode}' exists as TOWN for Business Unit '{businessUnit}'"
            );
        }

        if (string.IsNullOrWhiteSpace(sapCustomer.Distributionchannel))
        {
            errors.Add("Distribution channel is required");
        }
        else if (
            !await _customerRepository.DistributionChannelExistsAsync(
                businessUnit,
                sapCustomer.Distributionchannel
            )
        )
        {
            errors.Add(
                $"No Distribution Channel '{sapCustomer.Distributionchannel}' exists for Business Unit '{businessUnit}'"
            );
        }

        if (string.IsNullOrWhiteSpace(sapCustomer.HouseNo))
            errors.Add("House No is required");

        if (string.IsNullOrWhiteSpace(sapCustomer.PaymentTerm))
        {
            errors.Add("Payment Term is required");
        }
        else if (
            !await _customerRepository.SettlementTermExistsAsync(
                businessUnit,
                sapCustomer.PaymentTerm
            )
        )
        {
            errors.Add(
                $"No Settlement Term '{sapCustomer.PaymentTerm}' found for Business Unit '{businessUnit}'"
            );
        }

        if (errors.Any())
            throw new ValidationExceptionDto(string.Join("; ", errors));

        return businessUnit;
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

    public async Task<(
        bool retailerChanged,
        bool geoClassificationChanged,
        bool distChannelChanged
    )> HasRetailerChanges(
        Retailer existing,
        Retailer updated,
        string postalCode,
        string distChannel
    )
    {
        if (existing == null)
            throw new ArgumentNullException(nameof(existing));
        if (updated == null)
            throw new ArgumentNullException(nameof(updated));

        bool retailerChanged =
            !string.Equals(
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
                existing.SAPAddressLine2?.Trim(),
                updated.SAPAddressLine2?.Trim(),
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
                existing.SAPEmailAddress?.Trim(),
                updated.SAPEmailAddress?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.SAPSettlementTermsCode?.Trim(),
                updated.SAPSettlementTermsCode?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || existing.CreditLimit != updated.CreditLimit
            || !string.Equals(
                existing.TerritoryCode?.Trim(),
                updated.TerritoryCode?.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
            || !string.Equals(
                existing.SAPVatRegistrationNo?.Trim(),
                updated.SAPVatRegistrationNo?.Trim(),
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

        var geoClassificationChanged = existing.RetailerClassifications.Any(c =>
            c.MasterGroup == "TOWN" && c.MasterGroupValue != postalCode
        );
        var distChannelChanged = existing.RetailerClassifications.Any(c =>
            c.MasterGroup == "DISTCHNL" && c.MasterGroupValue != distChannel
        );

        return (retailerChanged, geoClassificationChanged, distChannelChanged);
    }

    public void UpdateCustomer(Retailer existing, Retailer updated)
    {
        if (existing == null)
            throw new ArgumentNullException(nameof(existing));
        if (updated == null)
            throw new ArgumentNullException(nameof(updated));

        existing.RetailerName = updated.RetailerName;
        existing.AddressLine1 = updated.AddressLine1;
        existing.SAPAddressLine2 = updated.SAPAddressLine2;
        existing.AddressLine3 = updated.AddressLine3;
        existing.AddressLine4 = updated.AddressLine4;
        existing.AddressLine5 = updated.AddressLine5;
        existing.TelephoneNumber = updated.TelephoneNumber;
        existing.FaxNumber = updated.FaxNumber;
        existing.SAPEmailAddress = updated.SAPEmailAddress;
        existing.SAPSettlementTermsCode = updated.SAPSettlementTermsCode;
        existing.CreditLimit = updated.CreditLimit;
        existing.TerritoryCode = updated.TerritoryCode;
        existing.SAPVatRegistrationNo = updated.SAPVatRegistrationNo;
        existing.VatCode = updated.VatCode;
        existing.VatStatus = updated.VatStatus;
        existing.RetailerTypeCode = updated.RetailerTypeCode;
        existing.RetailerClassCode = updated.RetailerClassCode;
        existing.RetailerCategoryCode = updated.RetailerCategoryCode;

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

    private string ApplySapValueSafe(string value, string defaultValue)
    {
        return !string.IsNullOrWhiteSpace(value) ? value.Trim() : defaultValue;
    }

    private string MapSapFlagSafe(string sapFlag)
    {
        return sapFlag?.Trim() == "1" ? "1" : "0";
    }

    #region Global customer mapping (commented out)
    //public async Task<GlobalRetailer> MapSapToXontGlobalCustomerAsync(
    //    SapCustomerResponseDto sapCustomer
    //)
    //{
    //    try
    //    {
    //        var businessUnit = await _businessUnitResolver.ResolveBusinessUnitAsync(
    //            sapCustomer.SalesOrganization ?? "",
    //            sapCustomer.Division ?? ""
    //        );

    //        return new GlobalRetailer
    //        {
    //            RetailerCode = sapCustomer?.Customer?.Trim() ?? string.Empty,
    //            RetailerName = sapCustomer?.CustomerName?.Trim() ?? string.Empty,
    //            AddressLine1 = sapCustomer?.HouseNo?.Trim() ?? string.Empty,
    //            SAPAddressLine2 = sapCustomer?.Street?.Trim() ?? string.Empty,
    //            AddressLine3 = sapCustomer?.Street2?.Trim() ?? string.Empty,
    //            AddressLine4 = sapCustomer?.Street3?.Trim() ?? string.Empty,
    //            AddressLine5 = sapCustomer?.City?.Trim() ?? string.Empty,
    //            TelephoneNumber = sapCustomer?.Telephone?.Trim() ?? string.Empty,
    //            FaxNumber = sapCustomer?.Fax?.Trim() ?? string.Empty,
    //            SAPEmailAddress = sapCustomer?.Email?.Trim() ?? string.Empty,

    //            // Default values
    //            TelephoneNumberSys = string.Empty,

    //            PostCode = "0000",
    //            CurrencyCode = "LKR",
    //            CurrencyProcessingRequired = "1",

    //            // Audit fields
    //            CreatedOn = DateTime.Now,
    //            UpdatedOn = ParseSapDate(sapCustomer?.TodaysDate),
    //            CreatedBy = "SAP_SYNC",
    //            UpdatedBy = "SAP_SYNC",
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(
    //            ex,
    //            "Failed to map SAP customer {Code}. SalesOrg: {SalesOrg}, Division: {Division}",
    //            sapCustomer.Customer,
    //            sapCustomer.SalesOrganization,
    //            sapCustomer.Division
    //        );
    //        throw;
    //    }
    //}

    //public bool HasGlobalRetailerChanges(GlobalRetailer existing, GlobalRetailer updated)
    //{
    //    if (existing == null)
    //        throw new ArgumentNullException(nameof(existing));
    //    if (updated == null)
    //        throw new ArgumentNullException(nameof(updated));

    //    return !string.Equals(
    //            existing.RetailerName?.Trim(),
    //            updated.RetailerName?.Trim(),
    //            StringComparison.OrdinalIgnoreCase
    //        )
    //        || !string.Equals(
    //            existing.AddressLine1?.Trim(),
    //            updated.AddressLine1?.Trim(),
    //            StringComparison.OrdinalIgnoreCase
    //        )
    //        || !string.Equals(
    //            existing.SAPAddressLine2?.Trim(),
    //            updated.SAPAddressLine2?.Trim(),
    //            StringComparison.OrdinalIgnoreCase
    //        )
    //        || !string.Equals(
    //            existing.AddressLine3?.Trim(),
    //            updated.AddressLine3?.Trim(),
    //            StringComparison.OrdinalIgnoreCase
    //        )
    //        || !string.Equals(
    //            existing.AddressLine4?.Trim(),
    //            updated.AddressLine4?.Trim(),
    //            StringComparison.OrdinalIgnoreCase
    //        )
    //        || !string.Equals(
    //            existing.AddressLine5?.Trim(),
    //            updated.AddressLine5?.Trim(),
    //            StringComparison.OrdinalIgnoreCase
    //        )
    //        || !string.Equals(
    //            existing.TelephoneNumber?.Trim(),
    //            updated.TelephoneNumber?.Trim(),
    //            StringComparison.OrdinalIgnoreCase
    //        )
    //        || !string.Equals(
    //            existing.FaxNumber?.Trim(),
    //            updated.FaxNumber?.Trim(),
    //            StringComparison.OrdinalIgnoreCase
    //        )
    //        || !string.Equals(
    //            existing.SAPEmailAddress?.Trim(),
    //            updated.SAPEmailAddress?.Trim(),
    //            StringComparison.OrdinalIgnoreCase
    //        );
    //}

    //public void UpdateGlobalCustomer(GlobalRetailer existing, GlobalRetailer updated)
    //{
    //    if (existing == null)
    //        throw new ArgumentNullException(nameof(existing));
    //    if (updated == null)
    //        throw new ArgumentNullException(nameof(updated));

    //    existing.RetailerName = updated.RetailerName;
    //    existing.AddressLine1 = updated.AddressLine1;
    //    existing.SAPAddressLine2 = updated.SAPAddressLine2;
    //    existing.AddressLine3 = updated.AddressLine3;
    //    existing.AddressLine4 = updated.AddressLine4;
    //    existing.AddressLine5 = updated.AddressLine5;
    //    existing.TelephoneNumber = updated.TelephoneNumber;
    //    existing.FaxNumber = updated.FaxNumber;
    //    existing.SAPEmailAddress = updated.SAPEmailAddress;

    //    existing.UpdatedOn = DateTime.Now;
    //    existing.UpdatedBy = "SAP_SYNC";
    //}
    #endregion
}

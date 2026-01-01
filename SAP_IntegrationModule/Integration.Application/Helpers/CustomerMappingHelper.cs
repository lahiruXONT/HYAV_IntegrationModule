using Integration.Application.DTOs;
using Integration.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Integration.Application.Helpers
{
    public class CustomerMappingHelper
    {
        private readonly BusinessUnitResolveHelper _businessUnitResolver;
        private readonly ILogger<CustomerMappingHelper> _logger;

        public CustomerMappingHelper( ILogger<CustomerMappingHelper> logger, BusinessUnitResolveHelper businessUnitResolver)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _businessUnitResolver = businessUnitResolver ?? throw new ArgumentNullException(nameof(businessUnitResolver));
        }

        public async Task<Retailer> MapSapToXontCustomerAsync(SapCustomerResponseDto sapCustomer)
        {
            await ValidateSapCustomerAsync(sapCustomer);

            try
            {
                var businessUnit = await _businessUnitResolver.ResolveAsync(sapCustomer.Division ?? "");

                var buConfig = await _businessUnitResolver.GetBusinessUnitConfigAsync(businessUnit);

                return new Retailer
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

                    VatRegistrationNo = sapCustomer.VATRegistrationNumber?.Trim(),

                    BusinessUnit = businessUnit,
                    Division =  sapCustomer.Division?.Trim(),
                    SalesOrganization = sapCustomer.SalesOrganization?.Trim(),
                    DistributionChannel = sapCustomer?.Distributionchannel ,


                    // Default values
                    //Province

                    TelephoneNumberSys = string.Empty,
                    ContactName = string.Empty,
                    PaymentMethodCode = "CA",
                    OnStopFlag = "0",
                    VatCode = string.IsNullOrWhiteSpace(sapCustomer.VATRegistrationNumber) ? string.Empty :  "V1",
                    VatStatus = string.IsNullOrWhiteSpace(sapCustomer.VATRegistrationNumber) ? string.Empty : "1",                  

                    PostCode = "0000",
                    CurrencyCode = "LKR",
                    CurrencyProcessingRequired = "1",
                    Status = "1",



                    RetailerTypeCode = !string.IsNullOrEmpty(sapCustomer.CustomerGroup1) ?
                        sapCustomer.CustomerGroup1.Trim() : "",
                    RetailerClassCode = !string.IsNullOrEmpty(sapCustomer.CustomerGroup2) ?
                        sapCustomer.CustomerGroup2.Trim() : "",
                    RetailerCategoryCode = !string.IsNullOrEmpty(sapCustomer.CustomerGroup3) ?
                        sapCustomer.CustomerGroup3.Trim() : "",
                    //District = sapCustomer.RegionCode?.Trim(),
                    //Town = sapCustomer.PostalCode?.Trim(),


                    // Audit fields
                    CreatedOn = DateTime.Now,
                    UpdatedOn = ParseSapDate(sapCustomer.TodaysDate),
                    CreatedBy = "SAP_SYNC",
                    UpdatedBy = "SAP_SYNC"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to map SAP customer {Code}. SalesOrg: {SalesOrg}, Division: {Division}", sapCustomer.Customer, sapCustomer.SalesOrganization, sapCustomer.Division);
                throw;
            }
        }

        public async Task ValidateSapCustomerAsync(SapCustomerResponseDto sapCustomer)
        {
            if (sapCustomer == null)
                throw new ArgumentNullException(nameof(sapCustomer));

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(sapCustomer.Customer))
                errors.Add("Customer code is required");

            if (string.IsNullOrWhiteSpace(sapCustomer.CustomerName))
                errors.Add("Customer name is required");

            if (string.IsNullOrWhiteSpace(sapCustomer.SalesOrganization))
                errors.Add("Sales organization is required");


            if (!string.IsNullOrWhiteSpace(sapCustomer.Division) && !await _businessUnitResolver.DivisionExistsAsync(sapCustomer.Division))
            {
                errors.Add($"Division '{sapCustomer.Division}' not found ");
            }
            if (errors.Any())
            {
                var errorMessage = string.Join("; ", errors);
                throw new ValidationExceptionDto(errorMessage);
            }

        }

        public bool HasChanges(Retailer existing, Retailer updated)
        {
            if (existing == null) throw new ArgumentNullException(nameof(existing));
            if (updated == null) throw new ArgumentNullException(nameof(updated));

            return existing.RetailerName != updated.RetailerName ||
                   existing.AddressLine1 != updated.AddressLine1 ||
                   existing.AddressLine2 != updated.AddressLine2 ||
                   existing.AddressLine3 != updated.AddressLine3 ||
                   existing.AddressLine4 != updated.AddressLine4 ||
                   existing.AddressLine5 != updated.AddressLine5 ||
                   existing.TelephoneNumber != updated.TelephoneNumber ||
                   existing.FaxNumber != updated.FaxNumber ||
                   existing.EmailAddress != updated.EmailAddress ||
                   existing.SettlementTermsCode != updated.SettlementTermsCode ||
                   existing.CreditLimit != updated.CreditLimit
                   || existing.SalesOrganization != updated.SalesOrganization ||
                   existing.Division != updated.Division
                   || existing.VatRegistrationNo != updated.VatRegistrationNo ||
                   existing.RetailerTypeCode != updated.RetailerTypeCode ||
                   existing.RetailerClassCode != updated.RetailerClassCode ||
                   existing.RetailerCategoryCode != updated.RetailerCategoryCode 
                   //|| existing.District != updated.District ||
                   //existing.Town != updated.Town
                   ;
        }

        public void UpdateCustomer(Retailer existing, Retailer updated)
        {
            if (existing == null) throw new ArgumentNullException(nameof(existing));
            if (updated == null) throw new ArgumentNullException(nameof(updated));


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
            existing.SalesOrganization = updated.SalesOrganization;
            existing.Division = updated.Division;
            existing.VatRegistrationNo = updated.VatRegistrationNo;
            existing.RetailerTypeCode = updated.RetailerTypeCode;
            existing.RetailerClassCode = updated.RetailerClassCode;
            existing.RetailerCategoryCode = updated.RetailerCategoryCode;
            //existing.District = updated.District;
            //existing.Town = updated.Town;

            existing.UpdatedOn = DateTime.Now;
            existing.UpdatedBy = "SAP_SYNC";

        }


        private DateTime ParseSapDate(string sapDate)
        {
            if (string.IsNullOrEmpty(sapDate))
                return DateTime.Now;

            try
            {
                if (DateTime.TryParseExact(sapDate, "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var result))
                {
                    return result;
                }

                if (DateTime.TryParseExact(sapDate, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out result))
                {
                    return result;
                }
                return DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing SAP date: {Date}, using current date", sapDate);
                return DateTime.Now;
            }
        }

    }
}
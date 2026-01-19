using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Integration.Domain.Entities;

public sealed class Retailer : BaseAuditableEntity
{
    public long RecordID { get; set; }
    public string RetailerCode { get; set; } = string.Empty;
    public string? RetailerName { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string SAPAddressLine2 { get; set; } = string.Empty;
    public string AddressLine3 { get; set; } = string.Empty;
    public string AddressLine4 { get; set; } = string.Empty;
    public string AddressLine5 { get; set; } = string.Empty;
    public string PostCode { get; set; } = string.Empty;
    public string TerritoryCode { get; set; } = string.Empty;
    public string TelephoneNumber { get; set; } = string.Empty;
    public string TelephoneNumberSys { get; set; } = string.Empty;
    public string FaxNumber { get; set; } = string.Empty;
    public string SAPEmailAddress { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyProcessingRequired { get; set; } = string.Empty;

    public string BusinessUnit { get; set; } = string.Empty;
    public string DistributionChannel { get; set; } = string.Empty;
    public string SAPSettlementTermsCode { get; set; } = string.Empty;
    public string PaymentMethodCode { get; set; } = string.Empty;

    public string? ContactName { get; set; }
    public string OnStopFlag { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }

    public string VatStatus { get; set; } = string.Empty;
    public string VatCode { get; set; } = string.Empty;
    public string SAPVatRegistrationNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public string PricingMethod { get; set; } = string.Empty;
    public string PriceGroup { get; set; } = string.Empty;
    public string TradeSchemeGroup { get; set; } = string.Empty;
    public string SalesOperationType { get; set; } = string.Empty;

    public string RetailerTypeCode { get; set; } = string.Empty;
    public string RetailerClassCode { get; set; } = string.Empty;
    public string RetailerCategoryCode { get; set; } = string.Empty;
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Integration.Domain.Entities;

public sealed class Retailer : BaseAuditableEntity
{
    public long RecordID { get; set; }
    public string RetailerCode { get; set; } = string.Empty;
    public string? RetailerName { get; set; }
    public string AlphaSearchCode { get; set; } = string.Empty;
    public string ShortAddress { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string AddressLine3 { get; set; } = string.Empty;
    public string AddressLine4 { get; set; } = string.Empty;
    public string AddressLine5 { get; set; } = string.Empty;
    public string PostCode { get; set; } = string.Empty;
    public string PostCodeSys { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public decimal Longitude { get; set; }
    public decimal Latitude { get; set; }
    public string TerritoryCode { get; set; } = string.Empty;
    public string TelephoneNumber { get; set; } = string.Empty;
    public string TelephoneNumberSys { get; set; } = string.Empty;
    public string FaxNumber { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string WebAddress { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyProcessingRequired { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string BusinessRegisterationNumber { get; set; } = string.Empty;
    public string? BusinessRegisterationName { get; set; }
    public DateTime? BusinessRegisterationDate { get; set; }
    public DateTime? BusinessRegisterationExpiryDate { get; set; }
    public string NicNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }

    //public string Salutation { get; set; } = string.Empty;
    //public string ContactName { get; set; } = string.Empty;
    //public string OnStopFlag { get; set; } = string.Empty;
    //public string OnStopReasonCode { get; set; } = string.Empty;
    //public string Status { get; set; } = string.Empty;
    //public string SalesOperationType = string.Empty;

    public string BusinessUnit { get; set; } = string.Empty;

    //public string SalesOrganization { get; set; } = string.Empty;
    //public string DistributionChannel { get; set; } = string.Empty;
    //public string Division { get; set; } = string.Empty;
    public string SettlementTermsCode { get; set; } = string.Empty;
    public string PaymentMethodCode { get; set; } = string.Empty;
    public decimal BankSortCode { get; set; }
    public string BankAccountNumber { get; set; } = string.Empty;
    public string NominalMaskCode { get; set; } = string.Empty;
    public string PriceCodeTable { get; set; } = string.Empty;
    public string FactorAccountCode { get; set; } = string.Empty;
    public string FactorAccountCodeSon { get; set; } = string.Empty;
    public string TradeDiscountCode { get; set; } = string.Empty;
    public string ProformaFlag { get; set; } = string.Empty;
    public string StatementRequiredFlag { get; set; } = string.Empty;
    public string ConsolidateStatementFlag { get; set; } = string.Empty;
    public string StatementCurrencyFlag { get; set; } = string.Empty;
    public string LetterRequiredFlag { get; set; } = string.Empty;
    public string Salutation { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string OnStopFlag { get; set; } = string.Empty;
    public string OnStopReasonCode { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal BackOrderFlag { get; set; }
    public string TextCode { get; set; } = string.Empty;
    public decimal OsOrdersPendingBase { get; set; }
    public decimal OsOrdersPendingCurr { get; set; }
    public string ElectronicTradingFlag { get; set; } = string.Empty;
    public string VatStatus { get; set; } = string.Empty;
    public string VatCode { get; set; } = string.Empty;
    public string VatRegistrationNo { get; set; } = string.Empty;
    public string MemberStateCode { get; set; } = string.Empty;
    public int TransactionNatureCode { get; set; }
    public string TermsOfDeliverCode { get; set; } = string.Empty;
    public string ModeOfTransportCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal TotalCreditsNotAllocated { get; set; }
    public decimal CreditLimitCurrency { get; set; }
    public decimal CurrentBalanceCurrency { get; set; }
    public decimal TotalCreditsNotAllocatedCurrency { get; set; }
    public decimal LastPaymentValueCurrency { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string DeliveryTermsCode { get; set; } = string.Empty;
    public string TransportModeCode { get; set; } = string.Empty;
    public string PrefOperatorName1 { get; set; } = string.Empty;
    public string PrefOperatorName2 { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseOwner { get; set; } = string.Empty;
    public DateTime? LicenseExpiryDate { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public string StaffId { get; set; } = string.Empty;
    public string RetailerTypeCode { get; set; } = string.Empty;
    public string RetailerClassCode { get; set; } = string.Empty;
    public string RetailerCategoryCode { get; set; } = string.Empty;
    public decimal TotalBalance { get; set; }
}

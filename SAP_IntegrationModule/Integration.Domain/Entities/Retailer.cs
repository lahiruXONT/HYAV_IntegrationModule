using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Integration.Domain.Entities;

public class Retailer : GlobalRetailer
{
    [StringLength(4)]
    public string BusinessUnit { get; set; } = string.Empty;
    //public string SalesOrganization { get; set; } = string.Empty;
    //public string DistributionChannel { get; set; } = string.Empty;
    //public string Division { get; set; } = string.Empty;

    [StringLength(3)]
    public string SettlementTermsCode { get; set; } = string.Empty;

    [StringLength(2)]
    public string PaymentMethodCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,0)")]
    public decimal BankSortCode { get; set; }

    [StringLength(15)]
    public string BankAccountNumber { get; set; } = string.Empty;

    [StringLength(5)]
    public string NominalMaskCode { get; set; } = string.Empty;

    [StringLength(4)]
    public string PriceCodeTable { get; set; } = string.Empty;

    [StringLength(8)]
    public string FactorAccountCode { get; set; } = string.Empty;

    [StringLength(4)]
    public string FactorAccountCodeSon { get; set; } = string.Empty;

    [StringLength(2)]
    public string TradeDiscountCode { get; set; } = string.Empty;

    [StringLength(1)]
    public string ProformaFlag { get; set; } = string.Empty;

    [StringLength(1)]
    public string StatementRequiredFlag { get; set; } = string.Empty;

    [StringLength(1)]
    public string ConsolidateStatementFlag { get; set; } = string.Empty;

    [StringLength(1)]
    public string StatementCurrencyFlag { get; set; } = string.Empty;

    [StringLength(1)]
    public string LetterRequiredFlag { get; set; } = string.Empty;

    [StringLength(20)]
    public string Salutation { get; set; } = string.Empty;

    [StringLength(30)]
    public string? ContactName { get; set; }

    [StringLength(1)]
    public string OnStopFlag { get; set; } = string.Empty;

    [StringLength(2)]
    public string OnStopReasonCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(15,4)")]
    public decimal CreditLimit { get; set; }

    [Column(TypeName = "decimal(1,0)")]
    public decimal BackOrderFlag { get; set; }

    [StringLength(4)]
    public string TextCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(15,4)")]
    public decimal OsOrdersPendingBase { get; set; }

    [Column(TypeName = "decimal(15,4)")]
    public decimal OsOrdersPendingCurr { get; set; }

    [StringLength(1)]
    public string ElectronicTradingFlag { get; set; } = string.Empty;

    [StringLength(1)]
    public string VatStatus { get; set; } = string.Empty;

    [StringLength(2)]
    public string VatCode { get; set; } = string.Empty;

    [StringLength(20)]
    public string VatRegistrationNo { get; set; } = string.Empty;

    [StringLength(3)]
    public string MemberStateCode { get; set; } = string.Empty;

    public int TransactionNatureCode { get; set; }

    [StringLength(3)]
    public string TermsOfDeliverCode { get; set; } = string.Empty;

    [StringLength(2)]
    public string ModeOfTransportCode { get; set; } = string.Empty;

    [StringLength(1)]
    public string Status { get; set; } = string.Empty;

    [Column(TypeName = "decimal(15,4)")]
    public decimal CurrentBalance { get; set; }

    [Column(TypeName = "decimal(15,4)")]
    public decimal TotalCreditsNotAllocated { get; set; }

    [Column(TypeName = "decimal(15,4)")]
    public decimal CreditLimitCurrency { get; set; }

    [Column(TypeName = "decimal(15,4)")]
    public decimal CurrentBalanceCurrency { get; set; }

    [Column(TypeName = "decimal(15,4)")]
    public decimal TotalCreditsNotAllocatedCurrency { get; set; }

    [Column(TypeName = "decimal(15,4)")]
    public decimal LastPaymentValueCurrency { get; set; }

    [StringLength(4)]
    public string WarehouseCode { get; set; } = string.Empty;

    [StringLength(4)]
    public string LocationCode { get; set; } = string.Empty;

    [StringLength(3)]
    public string DeliveryTermsCode { get; set; } = string.Empty;

    [StringLength(10)]
    public string TransportModeCode { get; set; } = string.Empty;

    [StringLength(50)]
    public string PrefOperatorName1 { get; set; } = string.Empty;

    [StringLength(50)]
    public string PrefOperatorName2 { get; set; } = string.Empty;

    [StringLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string LicenseOwner { get; set; } = string.Empty;

    public DateTime? LicenseExpiryDate { get; set; }

    [StringLength(1000)]
    public string Remarks { get; set; } = string.Empty;

    [StringLength(20)]
    public string StaffId { get; set; } = string.Empty;

    [StringLength(10)]
    public string RetailerTypeCode { get; set; } = string.Empty;

    [StringLength(10)]
    public string RetailerClassCode { get; set; } = string.Empty;

    [StringLength(10)]
    public string RetailerCategoryCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalBalance { get; set; }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Integration.Domain.Entities;

public class GlobalRetailer : BaseAuditableEntity
{
    public long RecordID { get; set; }

    [StringLength(15)]
    public string RetailerCode { get; set; } = string.Empty;

    [StringLength(75)]
    public string? RetailerName { get; set; }

    [StringLength(20)]
    public string AlphaSearchCode { get; set; } = string.Empty;

    [StringLength(15)]
    public string ShortAddress { get; set; } = string.Empty;

    [StringLength(50)]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(50)]
    public string AddressLine2 { get; set; } = string.Empty;

    [StringLength(50)]
    public string AddressLine3 { get; set; } = string.Empty;

    [StringLength(50)]
    public string AddressLine4 { get; set; } = string.Empty;

    [StringLength(50)]
    public string AddressLine5 { get; set; } = string.Empty;

    [StringLength(10)]
    public string PostCode { get; set; } = string.Empty;

    [StringLength(10)]
    public string PostCodeSys { get; set; } = string.Empty;

    [StringLength(2)]
    public string CountryCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(12,6)")]
    public decimal Longitude { get; set; }

    [Column(TypeName = "decimal(12,6)")]
    public decimal Latitude { get; set; }

    [StringLength(4)]
    public string TerritoryCode { get; set; } = string.Empty;

    [StringLength(20)]
    public string TelephoneNumber { get; set; } = string.Empty;

    [StringLength(20)]
    public string TelephoneNumberSys { get; set; } = string.Empty;

    [StringLength(20)]
    public string FaxNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string EmailAddress { get; set; } = string.Empty;

    [StringLength(50)]
    public string WebAddress { get; set; } = string.Empty;

    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    [StringLength(1)]
    public string CurrencyProcessingRequired { get; set; } = string.Empty;

    [StringLength(25)]
    public string BusinessType { get; set; } = string.Empty;

    [StringLength(20)]
    public string BusinessRegisterationNumber { get; set; } = string.Empty;

    [StringLength(75)]
    public string? BusinessRegisterationName { get; set; }

    public DateTime? BusinessRegisterationDate { get; set; }

    public DateTime? BusinessRegisterationExpiryDate { get; set; }

    [StringLength(12)]
    public string NicNumber { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }


    //public string Salutation { get; set; } = string.Empty;
    //public string ContactName { get; set; } = string.Empty;
    //public string OnStopFlag { get; set; } = string.Empty;
    //public string OnStopReasonCode { get; set; } = string.Empty;
    //public string Status { get; set; } = string.Empty;   
    //public string SalesOperationType = string.Empty;

}

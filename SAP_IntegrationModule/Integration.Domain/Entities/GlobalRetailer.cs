namespace Integration.Domain.Entities;
public class GlobalRetailer : BaseAuditableEntity
{
    public long RecId { get; set; }
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
   // public string TerritoryCode { get; set; } = string.Empty;
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

}

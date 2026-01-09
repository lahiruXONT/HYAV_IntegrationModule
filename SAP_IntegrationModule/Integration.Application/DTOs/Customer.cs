using System.ComponentModel.DataAnnotations;

namespace Integration.Application.DTOs;

public sealed class SapCustomerResponseDto
{
    [StringLength(4)]
    public string SalesOrganization { get; set; } = string.Empty;

    [StringLength(2)]
    public string Distributionchannel { get; set; } = string.Empty;

    [StringLength(2)]
    public string Division { get; set; } = string.Empty;

    [StringLength(15)]
    public string Customer { get; set; } = string.Empty;

    [StringLength(75)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(50)]
    public string HouseNo { get; set; } = string.Empty;

    [StringLength(50)]
    public string Street { get; set; } = string.Empty;

    [StringLength(50)]
    public string Street2 { get; set; } = string.Empty;

    [StringLength(50)]
    public string Street3 { get; set; } = string.Empty;

    [StringLength(50)]
    public string City { get; set; } = string.Empty;

    [StringLength(20)]
    public string Telephone { get; set; } = string.Empty;

    [StringLength(20)]
    public string Fax { get; set; } = string.Empty;

    [StringLength(50)]
    public string Email { get; set; } = string.Empty;

    [StringLength(3)]
    public string PaymentTerm { get; set; } = string.Empty;

    public decimal CreditLimit { get; set; }

    [StringLength(20)]
    public string VATRegistrationNumber { get; set; } = string.Empty;

    [StringLength(10)]
    public string CustomerGroup1 { get; set; } = string.Empty;

    [StringLength(10)]
    public string CustomerGroup2 { get; set; } = string.Empty;

    [StringLength(10)]
    public string CustomerGroup3 { get; set; } = string.Empty;

    [StringLength(10)]
    public string CustomerGroup4 { get; set; } = string.Empty;

    [StringLength(10)]
    public string CustomerGroup5 { get; set; } = string.Empty;

    [StringLength(4)]
    public string RegionCode { get; set; } = string.Empty;

    [StringLength(10)]
    public string PostalCode { get; set; } = string.Empty;

    [StringLength(8)]
    public string TodaysDate { get; set; } = string.Empty;
}

public sealed class XontCustomerSyncRequestDto
{
    [Required(ErrorMessage = "Date is required")]
    [StringLength(8)]
    public string Date { get; set; } = string.Empty;
}

public sealed class CustomerSyncResultDto
{
    public bool Success { get; set; }
    public int TotalRecords { get; set; }
    public int NewCustomers { get; set; }
    public int UpdatedCustomers { get; set; }
    public int SkippedCustomers { get; set; }
    public int FailedRecords { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SyncDate { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public List<string>? ValidationErrors { get; set; }
}

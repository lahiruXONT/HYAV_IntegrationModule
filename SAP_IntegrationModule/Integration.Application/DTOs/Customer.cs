namespace Integration.Application.DTOs
{
    public class SapCustomerResponseDto
    {
        public string SalesOrganization { get; set; } = string.Empty;
        public string Distributionchannel { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string HouseNo { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Street2 { get; set; } = string.Empty;
        public string Street3 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string Fax { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PaymentTerm { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public string VATRegistrationNumber { get; set; } = string.Empty;
        public string CustomerGroup1 { get; set; } = string.Empty;
        public string CustomerGroup2 { get; set; } = string.Empty;
        public string CustomerGroup3 { get; set; } = string.Empty;
        public string CustomerGroup4 { get; set; } = string.Empty;
        public string CustomerGroup5 { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string TodaysDate { get; set; } = string.Empty;
    }

    public class XontCustomerSyncRequestDto
    {
        public string Date { get; set; } = string.Empty;
    }

    public class CustomerSyncResultDto
    {
        public bool Success { get; set; }
        public int TotalRecords { get; set; }
        public int NewCustomers { get; set; }
        public int UpdatedCustomers { get; set; }
        public int SkippedCustomers { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SyncDate { get; set; }
    }
}
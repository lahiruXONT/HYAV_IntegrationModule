using Integration.Application.DTOs;
using Integration.Application.Interfaces;

namespace Integration.Infrastructure.Mock;

public sealed class MockSapClient : ISapClient
{
    public Task<List<SapCustomerResponseDto>> GetCustomerChangesAsync(
        XontCustomerSyncRequestDto request
    )
    {
        return Task.FromResult(
            new List<SapCustomerResponseDto>
            {
                new()
                {
                    SalesOrganization = "6070", // 4 chars max
                    Distributionchannel = "01", // 2 chars max
                    Division = "67", // 2 chars max
                    Customer = "CUST001", // 15 chars max
                    CustomerName = "Test Customer2", // 75 chars max
                    HouseNo = "123", // 50 chars max
                    Street = "Main St", // 50 chars max
                    Street2 = "Suite A", // 50 chars max
                    Street3 = "",
                    City = "Colombo", // 50 chars max
                    Telephone = "112345678", // 20 chars max
                    Fax = "112345679", // 20 chars max
                    Email = "test@ex.com", // 50 chars max
                    PaymentTerm = "NET", // 3 chars max - FIXED!
                    CreditLimit = 5000.00m,
                    VATRegistrationNumber = "VAT123", // 20 chars max
                    CustomerGroup1 = "GRP", // 10 chars max for RetailerTypeCode
                    CustomerGroup2 = "GRP", // 10 chars max for RetailerClassCode
                    CustomerGroup3 = "GRP", // 10 chars max for RetailerCategoryCode
                    CustomerGroup4 = "",
                    CustomerGroup5 = "",
                    RegionCode = "WE", // Used for TerritoryCode (4 chars max)
                    PostalCode = "CO01", // Used for TerritoryCode - might be too short, consider "HOM1"
                    TodaysDate = DateTime.Now.ToString("yyyyMMdd"),
                },
                new()
                {
                    SalesOrganization = "6070", // 4 chars max
                    Distributionchannel = "01", // 2 chars max
                    Division = "67", // 2 chars max
                    Customer = "CUST002",
                    CustomerName = "XYZ Traders",
                    HouseNo = "123", // 50 chars max
                    Street = "Main St", // 50 chars max
                    Street2 = "Suite A", // 50 chars max
                    Street3 = "",
                    City = "Colombo", // 50 chars max
                    Telephone = "112345678", // 20 chars max
                    Fax = "112345679", // 20 chars max
                    Email = "test@ex.com", // 50 chars max
                    PaymentTerm = "NET", // 3 chars max - FIXED!
                    CreditLimit = 5000.00m,
                    VATRegistrationNumber = "VAT123", // 20 chars max
                    CustomerGroup1 = "GRP", // 10 chars max for RetailerTypeCode
                    CustomerGroup2 = "GRP", // 10 chars max for RetailerClassCode
                    CustomerGroup3 = "GRP", // 10 chars max for RetailerCategoryCode
                    CustomerGroup4 = "",
                    CustomerGroup5 = "",
                    RegionCode = "WE", // Used for TerritoryCode (4 chars max)
                    PostalCode = "CO01", // Used for TerritoryCode - might be too short, consider "HOM1"
                    TodaysDate = DateTime.Now.ToString("yyyyMMdd"),
                },
            }
        );
    }

    public Task<List<SapMaterialResponseDto>> GetMaterialChangesAsync(
        XontMaterialSyncRequestDto request
    )
    {
        return Task.FromResult(
            new List<SapMaterialResponseDto>
            {
                new()
                {
                    SalesOrganization = "6070", // 4
                    Distributionchannel = "01", // 2
                    Division = "0067", // 4

                    Material = "MAT-0000000001", // <= 40
                    MaterialDescription = "1L Plastic Bottle",

                    MaterialGroup1 = "RAW", // 3
                    MaterialGroup2 = "PKG", // 3
                    MaterialGroup3 = "PLS", // 3
                    MaterialGroup4 = "",
                    MaterialGroup5 = "",

                    SalesUnit = "EA", // 3
                    BaseUnit = "EA", // 3
                    ConversionFactor = 1.0m,

                    BatchControlFlag = "Y", // 1
                    stockproductupdate = "N", // 1

                    TodaysDate = DateTime.UtcNow.ToString("yyyyMMdd"),
                },
                new()
                {
                    SalesOrganization = "6070",
                    Distributionchannel = "01",
                    Division = "0067",

                    Material = "MAT-0000000002",
                    MaterialDescription = "500ml Glass Bottle",

                    MaterialGroup1 = "RAW",
                    MaterialGroup2 = "PKG",
                    MaterialGroup3 = "GLS",
                    MaterialGroup4 = "",
                    MaterialGroup5 = "",

                    SalesUnit = "BOX",
                    BaseUnit = "EA",
                    ConversionFactor = 24.0m, // 1 BOX = 24 EA

                    BatchControlFlag = "N",
                    stockproductupdate = "Y",

                    TodaysDate = DateTime.UtcNow.ToString("yyyyMMdd"),
                },
            }
        );
    }
}

using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.Infrastructure.Data.EFConfigurations;

public class RetailerConfiguration
    : IEntityTypeConfiguration<Retailer>,
        IEntityTypeConfiguration<GlobalRetailer>,
        IEntityTypeConfiguration<RetailerClassification>
{
    public void Configure(EntityTypeBuilder<Retailer> entity)
    {
        entity.ToTable("Retailer", "RD");
        entity.Metadata.SetAnnotation("SqlServer:UseSqlOutputClause", false);
        entity.HasKey(e => e.RecordID).HasName($"PK_Retailer");

        entity
            .HasIndex(e => new { e.BusinessUnit, e.RetailerCode })
            .IsUnique()
            .HasDatabaseName($"IX_Retailer_BusinessUnit_RetailerCode");

        entity.Property(e => e.RecordID).ValueGeneratedOnAdd();

        entity.Property(e => e.BusinessUnit).HasMaxLength(4).IsRequired();

        entity.Property(e => e.RetailerCode).IsRequired().HasMaxLength(15);

        entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");

        entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");

        // BLOCK updates for Global-owned columns if needed
        //entity.Property(e => e.RetailerName)
        //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        //entity.Property(e => e.AddressLine1)
        //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        //entity.Property(e => e.AddressLine2)
        //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        //entity.Property(e => e.AddressLine3)
        //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        //entity.Property(e => e.AddressLine4)
        //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        //entity.Property(e => e.AddressLine5)
        //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        //entity.Property(e => e.TelephoneNumber)
        //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        //entity.Property(e => e.EmailAddress)
        //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        //entity.Property(e => e.TerritoryCode)
        //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
    }

    public void Configure(EntityTypeBuilder<GlobalRetailer> entity)
    {
        entity.ToTable("GlobalRetailer", "RD");
        entity.HasKey(e => e.RecordID).HasName($"PK_GlobalRetailer");
        entity.HasIndex(e => e.RetailerCode).IsUnique();
        entity.Property(e => e.RecordID).ValueGeneratedOnAdd();
        entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
        entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");
    }

    public void Configure(EntityTypeBuilder<RetailerClassification> entity)
    {
        entity.ToTable("RetailerClassification", "RD");
        entity
            .HasKey(e => new
            {
                e.BusinessUnit,
                e.RetailerCode,
                e.MasterGroup,
                e.MasterGroupValue,
            })
            .HasName("PK_RD_RetailerClassification");
    }
}

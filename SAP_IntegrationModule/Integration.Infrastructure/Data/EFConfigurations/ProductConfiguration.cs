using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.Infrastructure.Data.EFConfigurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
//,IEntityTypeConfiguration<GlobalProduct>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.ToTable("Product", "RD");
        entity.Metadata.SetAnnotation("SqlServer:UseSqlOutputClause", false);

        entity.HasKey(e => e.RecID).HasName($"PK_Product");

        entity
            .HasIndex(e => new { e.BusinessUnit, e.ProductCode })
            .IsUnique()
            .HasDatabaseName($"UK_Product");

        entity.Property(e => e.RecID).ValueGeneratedOnAdd();

        entity.Property(e => e.BusinessUnit).HasMaxLength(4).IsRequired();

        entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");

        entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");
        entity.Property(e => e.ConversionFactor).HasPrecision(30, 25);
        entity.Property(e => e.SortSequence).HasPrecision(9, 0);
        entity.Property(e => e.Weight).HasPrecision(13, 4);

        // BLOCK updates for Global-owned columns
        //entity.Property(e => e.ProductCode)
        //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        //entity.Property(e => e.Description)
        //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        //entity.Property(e => e.Description2)
        //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
    }

    //public void Configure(EntityTypeBuilder<GlobalProduct> entity)
    //{
    //    entity.ToTable("GlobalProduct", "RD");
    //    entity.HasKey(e => e.RecID).HasName($"PK_GlobalProduct");
    //    entity.HasIndex(e => e.ProductCode).IsUnique().HasDatabaseName($"UK_GlobalProduct");
    //    entity.Property(e => e.RecID).ValueGeneratedOnAdd();
    //    entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
    //    entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");
    //}
}

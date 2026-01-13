using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.Infrastructure.Data.EFConfigurations;

public class SalesConfiguration
    : IEntityTypeConfiguration<SalesOrderHeader>,
        IEntityTypeConfiguration<SalesOrderLine>,
        IEntityTypeConfiguration<SalesOrderDiscount>
{
    public void Configure(EntityTypeBuilder<SalesOrderHeader> entity)
    {
        entity.ToTable("SalesOrderHeader", "RD");
        entity.HasKey(e => e.RecID);
        //entity.HasIndex(e => e.OrderNo).IsUnique();

        entity.Property(e => e.BusinessUnit).HasMaxLength(4).IsRequired();
        entity.Property(e => e.RetailerCode).HasMaxLength(15).IsRequired();
        entity.Property(e => e.OrderComplete).HasMaxLength(1);
        entity.Property(e => e.IntegratedStatus).HasMaxLength(1);

        entity
            .HasMany(e => e.Lines)
            .WithOne(l => l.Header)
            .HasForeignKey(l => l.RecID)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasMany(e => e.Discounts)
            .WithOne(d => d.Header)
            .HasForeignKey(d => d.RecID)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<SalesOrderLine> entity)
    {
        entity.ToTable("SalesOrderLine", "RD");
        entity.HasKey(e => e.RecID);
        entity.Property(e => e.ProductCode).HasMaxLength(15).IsRequired();
        entity.Property(e => e.WarehouseCode).HasMaxLength(10);
        entity.Property(e => e.LocationCode).HasMaxLength(10);
    }

    public void Configure(EntityTypeBuilder<SalesOrderDiscount> entity)
    {
        entity.ToTable("SalesOrderDiscount", "RD");
        entity.HasKey(e => e.RecID);
        entity.Property(e => e.OrderComplete).HasMaxLength(1);
        entity.Property(e => e.DiscountReasonCode).HasMaxLength(10);
    }
}

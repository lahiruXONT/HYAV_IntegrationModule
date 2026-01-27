using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.Infrastructure.Data.EFConfigurations;

public class StockConfiguration
    : IEntityTypeConfiguration<StockTransaction>,
        IEntityTypeConfiguration<StockRecord>,
        IEntityTypeConfiguration<TerritoryProduct>,
        IEntityTypeConfiguration<CurrentSerialBatch>,
        IEntityTypeConfiguration<ReceivedSerialBatch>
{
    public void Configure(EntityTypeBuilder<StockTransaction> entity)
    {
        entity.ToTable("StockTransaction", "RD");
        entity.HasKey(e => e.RecId);

        entity
            .HasMany(e => e.ReceivedSerialBatches)
            .WithOne(l => l.Transaction)
            .HasForeignKey(l => new
            {
                l.BusinessUnit,
                l.TerritoryCode,
                l.WarehouseCode,
                l.LocationCode,
                l.ProductCode,
                l.TransactionCode,
                l.TRNTypeRefIN,
                l.TRNTypeHeaderNumberIN,
                l.TRNTypeDetailNumberIN,
            })
            .HasPrincipalKey(t => new
            {
                t.BusinessUnit,
                t.TerritoryCode,
                t.WarehouseCode,
                t.LocationCode,
                t.ProductCode,
                t.TransactionCode,
                t.TrnTypeRef,
                t.TrnTypeHeaderNumber,
                t.TrnTypeDetailNumber,
            })
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<StockRecord> entity)
    {
        entity.ToTable("StockRecord", "RD");
        entity.HasKey(e => e.RecId);
    }

    public void Configure(EntityTypeBuilder<TerritoryProduct> entity)
    {
        entity.ToTable("TerritoryProduct", "RD");
        entity.HasKey(e => e.RecId);
    }

    public void Configure(EntityTypeBuilder<CurrentSerialBatch> entity)
    {
        entity.ToTable("CurrentSerialBatch", "RD");
        entity.HasKey(e => e.RecId);
    }

    public void Configure(EntityTypeBuilder<ReceivedSerialBatch> entity)
    {
        entity.ToTable("ReceivedSerialBatch", "RD");
        entity.HasKey(e => e.RecID);
    }
}

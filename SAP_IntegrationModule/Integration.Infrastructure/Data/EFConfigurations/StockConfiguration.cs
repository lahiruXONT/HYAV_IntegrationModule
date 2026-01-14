using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.Infrastructure.Data.EFConfigurations;

public class StockConfiguration
    : IEntityTypeConfiguration<StockTransaction>,
        IEntityTypeConfiguration<StockRecord>,
        IEntityTypeConfiguration<TerritoryProduct>,
        IEntityTypeConfiguration<CurrentSerialBatch>
{
    public void Configure(EntityTypeBuilder<StockTransaction> entity)
    {
        entity.ToTable("StockTransaction", "RD");
        entity.HasKey(e => e.RecId);
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
}

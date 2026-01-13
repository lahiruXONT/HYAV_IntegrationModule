using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.Infrastructure.Data.EFConfigurations;

public class OtherConfiguration
    : IEntityTypeConfiguration<MasterDefinitionValue>,
        IEntityTypeConfiguration<MasterDefinition>,
        IEntityTypeConfiguration<TerritoryPostalCode>
{
    public void Configure(EntityTypeBuilder<MasterDefinitionValue> entity)
    {
        entity.ToTable("MasterDefinitionValue", "XA");
        entity.HasKey(e => e.RecordID).HasName($"PK_MasterDefinitionValue");
    }

    public void Configure(EntityTypeBuilder<MasterDefinition> entity)
    {
        entity.ToTable("MasterDefinition", "XA");
        entity.HasKey(e => e.RecordID).HasName($"PK_MasterDefinition");
    }

    public void Configure(EntityTypeBuilder<TerritoryPostalCode> entity)
    {
        entity.ToTable("TerritoryPostalCode", "RD");
        entity.HasKey(e => e.RecID);
        entity
            .HasIndex(e => new { e.PostalCode })
            .IsUnique()
            .HasDatabaseName("UQ_TerritoryPostalCode_PostalCode");

        entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(10);
    }
}

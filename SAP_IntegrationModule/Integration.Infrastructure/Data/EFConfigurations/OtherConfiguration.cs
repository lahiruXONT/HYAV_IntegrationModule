using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.Infrastructure.Data.EFConfigurations;

public class OtherConfiguration
    : IEntityTypeConfiguration<MasterDefinitionValue>,
        IEntityTypeConfiguration<MasterDefinition>,
        IEntityTypeConfiguration<SettlementTerm>
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

    public void Configure(EntityTypeBuilder<SettlementTerm> entity)
    {
        entity.ToTable("SettlementTerm", "XF");
        entity
            .HasKey(e => new
            {
                e.BusinessUnit,
                e.SourceModuleCode,
                e.SettlementTermsCode,
            })
            .HasName("PK_XF_SettlementTerm");
    }
}

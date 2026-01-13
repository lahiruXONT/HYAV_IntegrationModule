using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.Infrastructure.Data.EFConfigurations;

public class LogConfiguration
    : IEntityTypeConfiguration<RequestLog>,
        IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<RequestLog> entity)
    {
        entity.ToTable("RequestLog", "WA");
        entity.HasKey(e => e.RecID);
    }

    public void Configure(EntityTypeBuilder<ErrorLog> entity)
    {
        entity.ToTable("ErrorLog", "WA");
        entity.HasKey(e => e.RecID);
        entity
            .HasOne<RequestLog>()
            .WithMany()
            .HasForeignKey(e => e.RequestLogID)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

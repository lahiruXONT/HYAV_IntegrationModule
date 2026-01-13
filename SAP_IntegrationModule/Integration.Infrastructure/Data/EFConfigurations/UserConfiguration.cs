using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.Infrastructure.Data.EFConfigurations;

public class UserConfiguration
    : IEntityTypeConfiguration<User>,
        IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.ToTable("User", "WA");
        entity.HasKey(e => e.RecID);
        entity
            .HasIndex(e => new { e.BusinessUnit, e.UserName })
            .IsUnique()
            .HasDatabaseName("UK_WAUser");
    }

    public void Configure(EntityTypeBuilder<UserSession> entity)
    {
        entity.ToTable("UserSession", "WA");
        entity.HasKey(e => e.RecID);
        entity.HasIndex(e => e.UserID);
        entity
            .HasIndex(e => e.RefreshToken)
            .HasFilter("[RefreshToken] IS NOT NULL AND [RefreshToken] != ''");

        entity
            .HasOne(e => e.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(e => e.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Property(e => e.RefreshToken).HasMaxLength(500);
        entity.Property(e => e.DeviceInfo).HasMaxLength(500);
        entity.Property(e => e.IPAddress).HasMaxLength(50);
        entity.Property(e => e.IssuedAt).HasDefaultValueSql("GETDATE()");
    }
}

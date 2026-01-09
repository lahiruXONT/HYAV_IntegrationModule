using System;
using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;
using Microsoft.Extensions.Logging;

namespace Integration.Infrastructure.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<RequestLog> RequestLogs { get; set; }
    public DbSet<ErrorLog> ErrorLogs { get; set; }
    public DbSet<GlobalRetailer> GlobalRetailers { get; set; }
    public DbSet<GlobalProduct> GlobalProducts { get; set; }
    public DbSet<TerritoryPostalCode> TerritoryPostalCodes { get; set; }
    public DbSet<Retailer> Retailers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<MasterDefinitionValue> MasterDefinitionValues { get; set; }
    public DbSet<MasterDefinition> MasterDefinitions { get; set; }
    public DbSet<RetailerClassification> RetailerClassifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (modelBuilder == null)
            throw new ArgumentNullException(nameof(modelBuilder));
        modelBuilder.Entity<MasterDefinitionValue>(entity =>
        {
            entity.ToTable("MasterDefinitionValue", "XA");
            entity.HasKey(e => e.RecordID).HasName($"PK_MasterDefinitionValue");
        });
        modelBuilder.Entity<MasterDefinition>(entity =>
        {
            entity.ToTable("MasterDefinition", "XA");
            entity.HasKey(e => e.RecordID).HasName($"PK_MasterDefinition");
        });
        modelBuilder.Entity<RetailerClassification>(entity =>
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
        });

        modelBuilder.Entity<GlobalRetailer>(entity =>
        {
            entity.ToTable("GlobalRetailer", "RD");
            entity.HasKey(e => e.RecordID).HasName($"PK_GlobalRetailer");
            entity.HasIndex(e => e.RetailerCode).IsUnique();
            entity.Property(e => e.RecordID).ValueGeneratedOnAdd();

            entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");
        });
        modelBuilder.Entity<GlobalRetailer>(entity =>
        {
            entity.ToTable("GlobalRetailer", "RD");
            entity.HasKey(e => e.RecordID).HasName($"PK_GlobalRetailer");
            entity.HasIndex(e => e.RetailerCode).IsUnique();
            entity.Property(e => e.RecordID).ValueGeneratedOnAdd();

            entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<GlobalProduct>(entity =>
        {
            entity.ToTable("GlobalProduct", "RD");
            entity.HasKey(e => e.RecID).HasName($"PK_GlobalProduct");
            entity.HasIndex(e => e.ProductCode).IsUnique();

            entity.Property(e => e.RecID).ValueGeneratedOnAdd();

            entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");
        });
        modelBuilder.Entity<TerritoryPostalCode>(entity =>
        {
            entity.ToTable("TerritoryPostalCode", "RD");
            entity.HasKey(e => e.RecID);
            entity
                .HasIndex(e => new { e.PostalCode })
                .IsUnique()
                .HasDatabaseName("UQ_TerritoryPostalCode_PostalCode");
            entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(10);
        });
        modelBuilder.Entity<Retailer>(entity =>
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
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product", "RD");
            entity.Metadata.SetAnnotation("SqlServer:UseSqlOutputClause", false);

            entity.HasKey(e => e.RecID).HasName($"PK_Product");

            entity
                .HasIndex(e => new { e.BusinessUnit, e.ProductCode })
                .IsUnique()
                .HasDatabaseName($"IX_Product_BusinessUnit_ProductCode");

            entity.Property(e => e.RecID).ValueGeneratedOnAdd();

            entity.Property(e => e.BusinessUnit).HasMaxLength(4).IsRequired();

            entity.Property(e => e.ProductCode).IsRequired().HasMaxLength(15);

            entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");

            // BLOCK updates for Global-owned columns
            //entity.Property(e => e.ProductCode)
            //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            //entity.Property(e => e.Description)
            //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            //entity.Property(e => e.Description2)
            //      .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User", "WA");
            entity.HasKey(e => e.RecID);
            entity
                .HasIndex(e => new { e.BusinessUnit, e.UserName })
                .IsUnique()
                .HasDatabaseName("UK_WAUser");
        });

        modelBuilder.Entity<UserSession>(entity =>
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
        });

        modelBuilder.Entity<RequestLog>(entity =>
        {
            entity.ToTable("RequestLog", "WA");
            entity.HasKey(e => e.RecID);
        });

        modelBuilder.Entity<ErrorLog>(entity =>
        {
            entity.ToTable("ErrorLog", "WA");
            entity.HasKey(e => e.RecID);
            entity
                .HasOne<RequestLog>()
                .WithMany()
                .HasForeignKey(e => e.RequestLogID)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}

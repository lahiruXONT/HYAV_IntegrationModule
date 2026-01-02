using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System;

namespace Integration.Infrastructure.Data;

public class BuDbContext : DbContext
{
    private readonly string _buCode;

    public BuDbContext(DbContextOptions<BuDbContext> options, string buCode)
        : base(options)
    {
        _buCode = buCode?.Trim() ?? throw new ArgumentNullException(nameof(buCode));
    }


    public DbSet<Retailer> Retailers { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (modelBuilder == null)
            throw new ArgumentNullException(nameof(modelBuilder));

        modelBuilder.Entity<Retailer>(entity =>
        {
            entity.ToTable("Retailer", "RD");
            entity.Metadata.SetAnnotation( "SqlServer:UseSqlOutputClause",false);
            entity.HasKey(e => e.RecordID)
                .HasName($"PK_Retailer"); 

            entity.HasIndex(e => new { e.BusinessUnit, e.RetailerCode })
                  .IsUnique()
                  .HasDatabaseName($"IX_Retailer_BusinessUnit_RetailerCode");

            entity.Property(e => e.RecordID)
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.BusinessUnit)
                  .HasDefaultValue(_buCode)
                  .HasMaxLength(4)
                  .IsRequired();

            entity.Property(e => e.RetailerCode)
                  .IsRequired()
                  .HasMaxLength(15);

            entity.Property(e => e.CreatedOn)
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedOn)
                  .HasDefaultValueSql("GETDATE()");

            // BLOCK updates for Global-owned columns
            entity.Property(e => e.RetailerName)
                  .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            entity.Property(e => e.AddressLine1)
                  .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            entity.Property(e => e.AddressLine2)
                  .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            entity.Property(e => e.AddressLine3)
                  .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            entity.Property(e => e.AddressLine4)
                  .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            entity.Property(e => e.AddressLine5)
                  .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            entity.Property(e => e.TelephoneNumber)
                  .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            entity.Property(e => e.EmailAddress)
                  .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            entity.Property(e => e.TerritoryCode)
                  .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product", "RD");
            entity.Metadata.SetAnnotation("SqlServer:UseSqlOutputClause", false);

            entity.HasKey(e => e.RecID)
                .HasName($"PK_Product");

            entity.HasIndex(e => new { e.BusinessUnit, e.ProductCode })
                  .IsUnique()
                  .HasDatabaseName($"IX_Product_BusinessUnit_ProductCode");

            entity.Property(e => e.RecID)
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.BusinessUnit)
                  .HasDefaultValue(_buCode)
                  .HasMaxLength(4)
                  .IsRequired();

            entity.Property(e => e.ProductCode)
                  .IsRequired()
                  .HasMaxLength(15);


            entity.Property(e => e.CreatedOn)
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedOn)
                  .HasDefaultValueSql("GETDATE()");

            // BLOCK updates for Global-owned columns
            entity.Property(e => e.ProductCode)
                  .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            entity.Property(e => e.Description)
                  .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            entity.Property(e => e.Description2)
                  .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        });

        base.OnModelCreating(modelBuilder);

    }

    public override void Dispose()
    {
        base.Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
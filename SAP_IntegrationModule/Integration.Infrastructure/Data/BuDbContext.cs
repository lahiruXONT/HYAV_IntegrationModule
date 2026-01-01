using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace Integration.Infrastructure.Data
{
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
                entity.ToTable("Retailer");

                entity.HasKey(e => e.RecId)
                    .HasName($"PK_Retailer"); 

                entity.HasIndex(e => new { e.BusinessUnit, e.RetailerCode })
                      .IsUnique()
                      .HasDatabaseName($"IX_Retailer_BusinessUnit_RetailerCode");

                entity.Property(e => e.RecId)
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
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Product");

                entity.HasKey(e => e.RecId)
                    .HasName($"PK_Product");

                entity.HasIndex(e => new { e.BusinessUnit, e.ProductCode })
                      .IsUnique()
                      .HasDatabaseName($"IX_Product_BusinessUnit_ProductCode");

                entity.Property(e => e.RecId)
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
}
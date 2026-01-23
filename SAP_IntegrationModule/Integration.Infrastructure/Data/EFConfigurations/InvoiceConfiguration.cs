using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.Infrastructure.Data.EFConfigurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<ERPInvoicedOrderDetail>
{
    public void Configure(EntityTypeBuilder<ERPInvoicedOrderDetail> entity)
    {
        entity.ToTable("ERPInvoicedOrderDetails", "RD");

        entity.HasKey(e => e.RecID).HasName($"PK_ERPInvoicedOrderDetails");

        entity
            .HasIndex(e => new
            {
                e.BusinessUnit,
                e.TerritoryCode,
                e.ExecutiveCode,
                e.CustomerCode,
                e.OrderNo,
                e.InvoiceDate,
                e.TotalGoodsValue,
            })
            .IsUnique()
            .HasDatabaseName($"UK_RD_ERPInvoicedOrderDetails");

        entity.Property(e => e.RecID).ValueGeneratedOnAdd();

        entity.Property(e => e.BusinessUnit).HasMaxLength(4).IsRequired();

        entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");

        entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");
        entity.Property(e => e.TotalGoodsValue).HasPrecision(15, 4);
        entity.Property(e => e.TotalInvoiceValue).HasPrecision(15, 4);

        entity
            .Property(e => e.TimeStamp)
            .IsRowVersion()
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate();
    }
}

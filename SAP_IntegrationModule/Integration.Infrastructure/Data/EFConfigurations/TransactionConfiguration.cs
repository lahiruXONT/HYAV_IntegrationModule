using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.Infrastructure.Data.EFConfigurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> entity)
    {
        entity.ToTable("Transactions", "AR");

        entity.HasKey(e => e.RecID).HasName($"PK_Transactions");

        entity
            .HasIndex(e => new { e.BusinessUnit, e.DocumentNumberSystem })
            .IsUnique()
            .HasDatabaseName($"IX_Transactions_BusinessUnit_DocumentNumberSystem");

        entity.Property(e => e.RecID).ValueGeneratedOnAdd();
    }
}

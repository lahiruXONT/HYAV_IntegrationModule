using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Integration.Infrastructure.Data;

public class SystemDbContext : DbContext
{
    public SystemDbContext(DbContextOptions<SystemDbContext> options)
        : base(options) { }

    public DbSet<ZYBusinessUnit> BusinessUnits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ZYBusinessUnit>(entity =>
        {
            entity.ToTable("ZYBusinessUnit", "dbo");
            entity.HasKey(e => e.BusinessUnit).HasName("PK_ZYBusinessUnit");
        });
    }
}

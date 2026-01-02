using Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Integration.Infrastructure.Data;

public class GlobalDbContext : DbContext
{
    public GlobalDbContext(DbContextOptions<GlobalDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<RequestLog> RequestLogs { get; set; }
    public DbSet<ErrorLog> ErrorLogs { get; set; }
    public DbSet<GlobalRetailer> GlobalRetailers { get; set; }
    public DbSet<GlobalProduct> GlobalProducts { get; set; }

    public DbSet<BusinessUnitDBMAP> BusinessUnits { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BusinessUnitDBMAP>(entity =>
        {
            entity.ToTable("BusinessUnit", "RD");

            entity.HasKey(e => e.BusinessUnit);

            entity.HasIndex(e => e.DatabaseName)
                  .IsUnique()
                  .HasDatabaseName("UQ_BusinessUnit_DatabaseName");

            entity.HasIndex(e => new { e.Division })
                  .IsUnique()
                  .HasDatabaseName("UQ_BusinessUnit_Division")
                  .HasFilter("[Division] IS NOT NULL");

            entity.Property(e => e.BusinessUnit)
                  .IsRequired()
                  .HasMaxLength(4);


        });
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User", "WA");
            entity.HasKey(e => e.RecID);
            entity.HasIndex(e => new { e.BusinessUnit, e.UserName })
                  .IsUnique()
                  .HasDatabaseName("UK_WAUser");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("UserSession", "WA");

            entity.HasKey(e => e.RecID);

            entity.HasIndex(e => e.UserID);

            entity.HasIndex(e => e.RefreshToken)
                  .HasFilter("[RefreshToken] IS NOT NULL AND [RefreshToken] != ''");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Sessions)
                  .HasForeignKey(e => e.UserID)
                  .OnDelete(DeleteBehavior.Cascade);


            entity.Property(e => e.RefreshToken)
                  .HasMaxLength(500);

            entity.Property(e => e.DeviceInfo)
                  .HasMaxLength(500);

            entity.Property(e => e.IPAddress)
                  .HasMaxLength(50);

            entity.Property(e => e.IssuedAt)
                  .HasDefaultValueSql("GETDATE()");
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
            entity.HasOne<RequestLog>()
                  .WithMany()
                  .HasForeignKey(e => e.RequestLogID)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<GlobalRetailer>(entity =>
        {
            entity.ToTable("GlobalRetailer", "RD");

            entity.Metadata.SetAnnotation("SqlServer:UseSqlOutputClause", false);
            entity.HasKey(e => e.RecordID)
                .HasName($"PK_GlobalRetailer");
            entity.HasIndex(e => e.RetailerCode).IsUnique();
            entity.Property(e => e.RecordID)
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.CreatedOn)
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedOn)
                  .HasDefaultValueSql("GETDATE()");


        });

        modelBuilder.Entity<GlobalProduct>(entity =>
        {
            entity.ToTable("GlobalProduct", "RD");
            entity.Metadata.SetAnnotation("SqlServer:UseSqlOutputClause", false);
            entity.HasKey(e => e.RecID)
                .HasName($"PK_GlobalProduct");
            entity.HasIndex(e => e.ProductCode).IsUnique();

            entity.Property(e => e.RecID)
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.CreatedOn)
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedOn)
                  .HasDefaultValueSql("GETDATE()");

        });
    }
}
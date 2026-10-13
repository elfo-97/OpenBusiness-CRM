using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenBusiness.Crm.Domain;

namespace OpenBusiness.Crm.Api.Data;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Activity> Activities => Set<Activity>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Role).HasConversion<int>().HasDefaultValue(UserRole.Member);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(320);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Document).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique().HasFilter("\"Email\" IS NOT NULL");
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(320);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Role).HasMaxLength(100);
            entity.Property(e => e.IsPrimary).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Customer>().WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Stage).HasConversion<int>().HasDefaultValue(LeadStage.New);
            entity.Property(e => e.EstimatedValue).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Customer>().WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(5000);
            entity.Property(e => e.OccurredAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Customer>().WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Lead>().WithMany().HasForeignKey(e => e.LeadId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // Seed default roles
        modelBuilder.Entity<ApplicationRole>().HasData(
            new ApplicationRole { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Member", NormalizedName = "MEMBER", Description = "Regular CRM user", ConcurrencyStamp = Guid.NewGuid().ToString() },
            new ApplicationRole { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Admin", NormalizedName = "ADMIN", Description = "Tenant administrator", ConcurrencyStamp = Guid.NewGuid().ToString() },
            new ApplicationRole { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Owner", NormalizedName = "OWNER", Description = "Tenant owner", ConcurrencyStamp = Guid.NewGuid().ToString() }
        );
    }
}
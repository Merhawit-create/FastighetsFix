using FastighetsFix.Domain.Entities;
using FastighetsFix.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FastighetsFix.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Apartment> Apartments => Set<Apartment>();
    public DbSet<MaintenanceRequest> MaintenanceRequests
        => Set<MaintenanceRequest>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<StatusHistory> StatusHistoryEntries
        => Set<StatusHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Fastigheter
        builder.Entity<Property>(entity =>
        {
            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Address)
                .HasMaxLength(250)
                .IsRequired();
        });

        // Lägenheter
        builder.Entity<Apartment>(entity =>
        {
            entity.Property(x => x.ApartmentNumber)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.PropertyId,
                x.ApartmentNumber
            }).IsUnique();

            entity.HasOne(x => x.Property)
                .WithMany(x => x.Apartments)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Användare
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasOne(x => x.Apartment)
                .WithMany()
                .HasForeignKey(x => x.ApartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Felanmälningar
        builder.Entity<MaintenanceRequest>(entity =>
        {
            entity.Property(x => x.Title)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(x => x.Resolution)
                .HasMaxLength(4000);

            entity.HasOne(x => x.Apartment)
                .WithMany(x => x.MaintenanceRequests)
                .HasForeignKey(x => x.ApartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.CreatedById)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.AssignedTechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.CreatedAt)
                .HasConversion(
                    value => value.UtcDateTime,
                    value => new DateTimeOffset(
                        DateTime.SpecifyKind(value, DateTimeKind.Utc)));

            entity.Property(x => x.ClosedAt)
                .HasConversion(
                    value => value.HasValue
                        ? value.Value.UtcDateTime
                        : (DateTime?)null,
                    value => value.HasValue
                        ? new DateTimeOffset(
                            DateTime.SpecifyKind(
                                value.Value, DateTimeKind.Utc))
                        : (DateTimeOffset?)null);
        });

        // Kommentarer
        builder.Entity<Comment>(entity =>
        {
            entity.Property(x => x.Text)
                .HasMaxLength(2000)
                .IsRequired();

            entity.HasOne(x => x.Request)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.CreatedAt)
                .HasConversion(
                    value => value.UtcDateTime,
                    value => new DateTimeOffset(
                        DateTime.SpecifyKind(value, DateTimeKind.Utc)));
        });

        // Statushistorik
        builder.Entity<StatusHistory>(entity =>
        {
            entity.HasOne(x => x.Request)
                .WithMany(x => x.StatusHistoryEntries)
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ChangedById)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.ChangedAt)
                .HasConversion(
                    value => value.UtcDateTime,
                    value => new DateTimeOffset(
                        DateTime.SpecifyKind(value, DateTimeKind.Utc)));
        });
    }
}
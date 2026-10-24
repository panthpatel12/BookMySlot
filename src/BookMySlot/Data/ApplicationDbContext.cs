using BookMySlot.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookMySlot.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Service> Services => Set<Service>();
    public DbSet<ProviderProfile> ProviderProfiles => Set<ProviderProfile>();
    public DbSet<ProviderService> ProviderServices => Set<ProviderService>();
    public DbSet<AppointmentSlot> AppointmentSlots => Set<AppointmentSlot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<SlotReservation> SlotReservations => Set<SlotReservation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser configuration
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();

            entity.HasOne(u => u.ProviderProfile)
                  .WithOne(p => p.User)
                  .HasForeignKey<ProviderProfile>(p => p.ApplicationUserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.CustomerAppointments)
                  .WithOne(a => a.Customer)
                  .HasForeignKey(a => a.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Service configuration
        builder.Entity<Service>(entity =>
        {
            entity.Property(s => s.Name).HasMaxLength(120).IsRequired();
            entity.Property(s => s.Description).HasMaxLength(1000).IsRequired();
            entity.Property(s => s.Price).HasPrecision(18, 2);
            entity.HasIndex(s => s.IsActive);
        });

        // ProviderProfile configuration
        builder.Entity<ProviderProfile>(entity =>
        {
            entity.Property(p => p.Bio).HasMaxLength(1000);
            entity.HasIndex(p => p.ApplicationUserId).IsUnique();
            entity.HasIndex(p => p.IsActive);
        });

        // ProviderService composite key and relationships
        builder.Entity<ProviderService>(entity =>
        {
            entity.HasKey(ps => new { ps.ProviderProfileId, ps.ServiceId });

            entity.HasOne(ps => ps.ProviderProfile)
                  .WithMany(p => p.ProviderServices)
                  .HasForeignKey(ps => ps.ProviderProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ps => ps.Service)
                  .WithMany(s => s.ProviderServices)
                  .HasForeignKey(ps => ps.ServiceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // AppointmentSlot configuration
        builder.Entity<AppointmentSlot>(entity =>
        {
            entity.HasOne(slot => slot.ProviderProfile)
                  .WithMany(p => p.AppointmentSlots)
                  .HasForeignKey(slot => slot.ProviderProfileId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(slot => slot.Service)
                  .WithMany(s => s.AppointmentSlots)
                  .HasForeignKey(slot => slot.ServiceId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(slot => new { slot.ProviderProfileId, slot.StartUtc, slot.EndUtc });
            entity.HasIndex(slot => slot.StartUtc);
            entity.HasIndex(slot => slot.IsActive);
        });

        // Appointment configuration
        builder.Entity<Appointment>(entity =>
        {
            entity.Property(a => a.Notes).HasMaxLength(500);

            entity.HasOne(a => a.AppointmentSlot)
                  .WithMany(slot => slot.Appointments)
                  .HasForeignKey(a => a.AppointmentSlotId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => a.CustomerId);
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.BookedAtUtc);
        });

        // SlotReservation configuration: Primary Key is AppointmentSlotId, Unique Key is AppointmentId
        builder.Entity<SlotReservation>(entity =>
        {
            entity.HasKey(sr => sr.AppointmentSlotId);

            entity.HasOne(sr => sr.AppointmentSlot)
                  .WithOne(slot => slot.SlotReservation)
                  .HasForeignKey<SlotReservation>(sr => sr.AppointmentSlotId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sr => sr.Appointment)
                  .WithOne(a => a.SlotReservation)
                  .HasForeignKey<SlotReservation>(sr => sr.AppointmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(sr => sr.AppointmentId).IsUnique();
        });
    }
}

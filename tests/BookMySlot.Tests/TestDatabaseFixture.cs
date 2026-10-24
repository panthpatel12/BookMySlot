using BookMySlot.Data;
using BookMySlot.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookMySlot.Tests;

public class TestDatabaseFixture : IDisposable
{
    public SqliteConnection Connection { get; }
    public DbContextOptions<ApplicationDbContext> Options { get; }

    public TestDatabaseFixture()
    {
        // Open a shared SQLite in-memory connection that remains open for the test duration
        Connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        Connection.Open();

        Options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(Connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public ApplicationDbContext CreateContext()
    {
        return new ApplicationDbContext(Options);
    }

    public async Task<(ApplicationUser customer, ApplicationUser otherCustomer, ProviderProfile provider, Service service, AppointmentSlot slot)> SeedStandardDataAsync(ApplicationDbContext context)
    {
        var customer1 = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test.customer1@example.com",
            Email = "test.customer1@example.com",
            FullName = "Test Customer 1"
        };
        var customer2 = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test.customer2@example.com",
            Email = "test.customer2@example.com",
            FullName = "Test Customer 2"
        };
        var providerUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test.provider@example.com",
            Email = "test.provider@example.com",
            FullName = "Dr. Specialist"
        };

        context.Users.AddRange(customer1, customer2, providerUser);
        await context.SaveChangesAsync();

        var provider = new ProviderProfile
        {
            ApplicationUserId = providerUser.Id,
            Bio = "Expert Consultant",
            IsActive = true
        };
        context.ProviderProfiles.Add(provider);
        await context.SaveChangesAsync();

        var service = new Service
        {
            Name = "Initial Consultation",
            Description = "Standard 30 min session",
            DurationMinutes = 30,
            Price = 500m,
            IsActive = true
        };
        context.Services.Add(service);
        await context.SaveChangesAsync();

        context.ProviderServices.Add(new ProviderService
        {
            ProviderProfileId = provider.Id,
            ServiceId = service.Id
        });
        await context.SaveChangesAsync();

        var tomorrow = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var slot = new AppointmentSlot
        {
            ProviderProfileId = provider.Id,
            ServiceId = service.Id,
            StartUtc = tomorrow,
            EndUtc = tomorrow.AddMinutes(service.DurationMinutes),
            IsActive = true
        };
        context.AppointmentSlots.Add(slot);
        await context.SaveChangesAsync();

        return (customer1, customer2, provider, service, slot);
    }

    public void Dispose()
    {
        Connection.Close();
        Connection.Dispose();
    }
}

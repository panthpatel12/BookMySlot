using BookMySlot.Data;
using BookMySlot.Models;
using BookMySlot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BookMySlot.Tests;

public class BookingServiceTests : IDisposable
{
    private readonly TestDatabaseFixture _fixture;

    public BookingServiceTests()
    {
        _fixture = new TestDatabaseFixture();
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    [Fact]
    public async Task SuccessfulBooking_CreatesAppointment_AndSlotReservation()
    {
        using var context = _fixture.CreateContext();
        var (customer, _, _, _, slot) = await _fixture.SeedStandardDataAsync(context);
        var bookingService = new BookingService(context, NullLogger<BookingService>.Instance);

        // Act
        var result = await bookingService.BookAppointmentAsync(customer.Id, slot.Id, "Need guidance on career");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(AppointmentStatus.Booked, result.Data.Status);
        Assert.Equal(slot.Id, result.Data.AppointmentSlotId);
        Assert.Equal(customer.Id, result.Data.CustomerId);

        // Verify slot reservation exists in SQLite
        var reservation = await context.SlotReservations.FirstOrDefaultAsync(r => r.AppointmentSlotId == slot.Id);
        Assert.NotNull(reservation);
        Assert.Equal(result.Data.Id, reservation.AppointmentId);
    }

    [Fact]
    public async Task ConcurrentBooking_SameSlot_ExactlyOneSucceeds()
    {
        using var context1 = _fixture.CreateContext();
        var (customer1, customer2, _, _, slot) = await _fixture.SeedStandardDataAsync(context1);

        using var context2 = _fixture.CreateContext();
        var bookingService1 = new BookingService(context1, NullLogger<BookingService>.Instance);
        var bookingService2 = new BookingService(context2, NullLogger<BookingService>.Instance);

        // Run both bookings simultaneously against real SQLite
        var task1 = bookingService1.BookAppointmentAsync(customer1.Id, slot.Id, "Customer 1 Booking");
        var task2 = bookingService2.BookAppointmentAsync(customer2.Id, slot.Id, "Customer 2 Booking");

        var results = await Task.WhenAll(task1, task2);

        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => !r.IsSuccess);

        Assert.Equal(1, successCount);
        Assert.Equal(1, failureCount);

        // Verify in DB that exactly one reservation was written
        using var verifyContext = _fixture.CreateContext();
        var reservations = await verifyContext.SlotReservations.Where(r => r.AppointmentSlotId == slot.Id).ToListAsync();
        Assert.Single(reservations);
    }

    [Fact]
    public async Task CustomerAppointmentOverlap_IsRejected()
    {
        using var context = _fixture.CreateContext();
        var (customer, _, provider, service, slot1) = await _fixture.SeedStandardDataAsync(context);
        var bookingService = new BookingService(context, NullLogger<BookingService>.Instance);

        // Create a second overlapping slot with another provider
        var providerUser2 = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "provider2@example.com",
            FullName = "Dr. Another"
        };
        context.Users.Add(providerUser2);
        var provider2 = new ProviderProfile { ApplicationUserId = providerUser2.Id, Bio = "Another Specialist", IsActive = true };
        context.ProviderProfiles.Add(provider2);
        context.ProviderServices.Add(new ProviderService { ProviderProfile = provider2, Service = service });

        var slot2 = new AppointmentSlot
        {
            ProviderProfile = provider2,
            Service = service,
            StartUtc = slot1.StartUtc.AddMinutes(10), // Overlaps slot1!
            EndUtc = slot1.StartUtc.AddMinutes(40),
            IsActive = true
        };
        context.AppointmentSlots.Add(slot2);
        await context.SaveChangesAsync();

        // Customer books slot 1
        var book1Result = await bookingService.BookAppointmentAsync(customer.Id, slot1.Id, null);
        Assert.True(book1Result.IsSuccess);

        // Customer attempts to book overlapping slot 2
        var book2Result = await bookingService.BookAppointmentAsync(customer.Id, slot2.Id, null);

        // Assert rejection
        Assert.False(book2Result.IsSuccess);
        Assert.Contains("overlapping", book2Result.Message.ToLower());
    }

    [Fact]
    public async Task ProviderSlotOverlap_IsRejected()
    {
        using var context = _fixture.CreateContext();
        var (_, _, provider, service, slot1) = await _fixture.SeedStandardDataAsync(context);
        var bookingService = new BookingService(context, NullLogger<BookingService>.Instance);

        // Attempt to create a new slot for the same provider that overlaps slot1
        var overlappingStart = slot1.StartUtc.AddMinutes(15);
        var result = await bookingService.CreateSlotAsync(provider.Id, service.Id, overlappingStart);

        Assert.False(result.IsSuccess);
        Assert.Contains("overlaps", result.Message.ToLower());
    }

    [Fact]
    public async Task Cancellation_ReleasesSlotReservation_WhileRetainingAppointmentHistory()
    {
        using var context = _fixture.CreateContext();
        var (customer, _, _, _, slot) = await _fixture.SeedStandardDataAsync(context);
        var bookingService = new BookingService(context, NullLogger<BookingService>.Instance);

        // Book slot
        var bookResult = await bookingService.BookAppointmentAsync(customer.Id, slot.Id, "Will cancel soon");
        Assert.True(bookResult.IsSuccess);
        var apptId = bookResult.Data!.Id;

        // Cancel appointment
        var cancelResult = await bookingService.CancelAppointmentAsync(apptId, customer.Id, isPrivilegedUser: false);
        Assert.True(cancelResult.IsSuccess);

        // Check appointment status in database is Cancelled
        var appt = await context.Appointments.FindAsync(apptId);
        Assert.NotNull(appt);
        Assert.Equal(AppointmentStatus.Cancelled, appt.Status);
        Assert.NotNull(appt.CancelledAtUtc);

        // Check SlotReservation was deleted (released)
        var reservation = await context.SlotReservations.FirstOrDefaultAsync(r => r.AppointmentSlotId == slot.Id);
        Assert.Null(reservation);
    }

    [Fact]
    public async Task FailedRescheduling_PreservesOriginalBooking()
    {
        using var context = _fixture.CreateContext();
        var (customer1, customer2, provider, service, slot1) = await _fixture.SeedStandardDataAsync(context);
        var bookingService = new BookingService(context, NullLogger<BookingService>.Instance);

        // Create slot 2 (day after tomorrow)
        var slot2Date = DateTime.UtcNow.Date.AddDays(2).AddHours(14);
        var slot2 = new AppointmentSlot
        {
            ProviderProfileId = provider.Id,
            ServiceId = service.Id,
            StartUtc = slot2Date,
            EndUtc = slot2Date.AddMinutes(service.DurationMinutes),
            IsActive = true
        };
        context.AppointmentSlots.Add(slot2);
        await context.SaveChangesAsync();

        // Customer 1 books slot 1
        var book1 = await bookingService.BookAppointmentAsync(customer1.Id, slot1.Id, "Original");
        Assert.True(book1.IsSuccess);

        // Customer 2 books slot 2 (so slot 2 is now taken)
        var book2 = await bookingService.BookAppointmentAsync(customer2.Id, slot2.Id, "Slot 2 booked by customer 2");
        Assert.True(book2.IsSuccess);

        // Customer 1 attempts to reschedule their booking to slot 2 (which is occupied)
        var rescheduleResult = await bookingService.RescheduleAppointmentAsync(
            book1.Data!.Id,
            slot2.Id,
            customer1.Id,
            isPrivilegedUser: false);

        // Reschedule must fail
        Assert.False(rescheduleResult.IsSuccess);

        // Original booking must remain Booked on slot 1
        var originalAppt = await context.Appointments.FindAsync(book1.Data.Id);
        Assert.NotNull(originalAppt);
        Assert.Equal(slot1.Id, originalAppt.AppointmentSlotId);
        Assert.Equal(AppointmentStatus.Booked, originalAppt.Status);

        // Original slot reservation must still exist
        var reservation1 = await context.SlotReservations.FirstOrDefaultAsync(r => r.AppointmentSlotId == slot1.Id);
        Assert.NotNull(reservation1);
        Assert.Equal(book1.Data.Id, reservation1.AppointmentId);
    }

    [Fact]
    public async Task UnauthorizedAccess_Rejected()
    {
        using var context = _fixture.CreateContext();
        var (customer1, customer2, _, _, slot) = await _fixture.SeedStandardDataAsync(context);
        var bookingService = new BookingService(context, NullLogger<BookingService>.Instance);

        // Customer 1 books slot
        var bookResult = await bookingService.BookAppointmentAsync(customer1.Id, slot.Id, "Customer 1 private appt");
        Assert.True(bookResult.IsSuccess);

        // Customer 2 tries to cancel customer 1's appointment
        var cancelResult = await bookingService.CancelAppointmentAsync(bookResult.Data!.Id, customer2.Id, isPrivilegedUser: false);
        Assert.False(cancelResult.IsSuccess);
        Assert.Contains("not authorized", cancelResult.Message.ToLower());

        // Appointment remains Booked
        var appt = await context.Appointments.FindAsync(bookResult.Data.Id);
        Assert.NotNull(appt);
        Assert.Equal(AppointmentStatus.Booked, appt.Status);
    }

    [Fact]
    public async Task PastDateAndInactive_Rejected()
    {
        using var context = _fixture.CreateContext();
        var (customer, _, provider, service, _) = await _fixture.SeedStandardDataAsync(context);
        var bookingService = new BookingService(context, NullLogger<BookingService>.Instance);

        // Past slot
        var pastSlot = new AppointmentSlot
        {
            ProviderProfileId = provider.Id,
            ServiceId = service.Id,
            StartUtc = DateTime.UtcNow.AddHours(-3),
            EndUtc = DateTime.UtcNow.AddHours(-2.5),
            IsActive = true
        };
        // Inactive slot
        var inactiveSlot = new AppointmentSlot
        {
            ProviderProfileId = provider.Id,
            ServiceId = service.Id,
            StartUtc = DateTime.UtcNow.AddDays(3),
            EndUtc = DateTime.UtcNow.AddDays(3).AddMinutes(30),
            IsActive = false
        };

        context.AppointmentSlots.AddRange(pastSlot, inactiveSlot);
        await context.SaveChangesAsync();

        var pastResult = await bookingService.BookAppointmentAsync(customer.Id, pastSlot.Id, null);
        Assert.False(pastResult.IsSuccess);
        Assert.Contains("past", pastResult.Message.ToLower());

        var inactiveResult = await bookingService.BookAppointmentAsync(customer.Id, inactiveSlot.Id, null);
        Assert.False(inactiveResult.IsSuccess);
        Assert.Contains("not active", inactiveResult.Message.ToLower());
    }
}

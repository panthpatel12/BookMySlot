using System.Data;
using BookMySlot.Data;
using BookMySlot.Models;
using Microsoft.EntityFrameworkCore;

namespace BookMySlot.Services;

public class BookingService : IBookingService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BookingService> _logger;

    public BookingService(ApplicationDbContext dbContext, ILogger<BookingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<BookingOperationResult<Appointment>> BookAppointmentAsync(string customerId, int slotId, string? notes)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return BookingOperationResult<Appointment>.Failure("A valid customer ID is required.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // 1. Load Slot with Service and Provider
            var slot = await _dbContext.AppointmentSlots
                .Include(s => s.Service)
                .Include(s => s.ProviderProfile)
                    .ThenInclude(p => p.User)
                .Include(s => s.SlotReservation)
                .FirstOrDefaultAsync(s => s.Id == slotId);

            if (slot == null)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("The requested appointment slot does not exist.");
            }

            // 2. Validate slot active & not past
            if (!slot.IsActive)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("This slot is not active and cannot be booked.");
            }

            if (slot.StartUtc <= DateTime.UtcNow)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("Cannot book past or immediate appointment slots.");
            }

            // 3. Validate service & provider active
            if (!slot.Service.IsActive)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("The service associated with this slot is currently inactive.");
            }

            if (!slot.ProviderProfile.IsActive)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("The provider associated with this slot is currently inactive.");
            }

            // 4. Validate provider is assigned to service
            var isAssigned = await _dbContext.ProviderServices
                .AnyAsync(ps => ps.ProviderProfileId == slot.ProviderProfileId && ps.ServiceId == slot.ServiceId);

            if (!isAssigned)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("The provider is no longer assigned to provide this service.");
            }

            // 5. Database-backed slot reservation check
            var isSlotReserved = await _dbContext.SlotReservations
                .AnyAsync(sr => sr.AppointmentSlotId == slotId);

            if (isSlotReserved || slot.SlotReservation != null)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("This appointment slot is already reserved by another customer.");
            }

            // 6. Customer overlap check: Cannot have another Booked appointment overlapping with [slot.StartUtc, slot.EndUtc)
            var customerOverlap = await _dbContext.Appointments
                .Include(a => a.AppointmentSlot)
                .AnyAsync(a => a.CustomerId == customerId &&
                               a.Status == AppointmentStatus.Booked &&
                               a.AppointmentSlot.StartUtc < slot.EndUtc &&
                               a.AppointmentSlot.EndUtc > slot.StartUtc);

            if (customerOverlap)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("You already have another booked appointment during this time window. Overlapping bookings are not allowed.");
            }

            // 7. Create Appointment record
            var appointment = new Appointment
            {
                CustomerId = customerId,
                AppointmentSlotId = slotId,
                Status = AppointmentStatus.Booked,
                Notes = notes?.Trim(),
                BookedAtUtc = DateTime.UtcNow
            };

            _dbContext.Appointments.Add(appointment);
            await _dbContext.SaveChangesAsync();

            // 8. Create SlotReservation record (Primary Key is AppointmentSlotId)
            var reservation = new SlotReservation
            {
                AppointmentSlotId = slotId,
                AppointmentId = appointment.Id,
                ReservedAtUtc = DateTime.UtcNow
            };

            _dbContext.SlotReservations.Add(reservation);
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Appointment {AppointmentId} successfully booked for customer {CustomerId} on slot {SlotId}.",
                appointment.Id, customerId, slotId);

            return BookingOperationResult<Appointment>.Success(appointment, "Your appointment has been successfully booked!");
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Concurrency / Constraint violation during slot booking for slot {SlotId}.", slotId);
            return BookingOperationResult<Appointment>.Failure("This slot was just reserved by another customer. Please choose a different time slot.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Unexpected error occurred during booking for slot {SlotId}.", slotId);
            return BookingOperationResult<Appointment>.Failure("An unexpected error occurred while booking your appointment. Please try again.");
        }
    }

    public async Task<BookingOperationResult> CancelAppointmentAsync(int appointmentId, string userId, bool isPrivilegedUser)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var appointment = await _dbContext.Appointments
                .Include(a => a.AppointmentSlot)
                .Include(a => a.SlotReservation)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult.Failure("Appointment not found.");
            }

            // Security check: Only customer who booked or admin/provider can cancel
            if (!isPrivilegedUser && appointment.CustomerId != userId)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult.Failure("You are not authorized to cancel this appointment.");
            }

            if (appointment.Status != AppointmentStatus.Booked)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult.Failure($"Cannot cancel an appointment that is already {appointment.Status}. Only active bookings can be cancelled.");
            }

            if (appointment.AppointmentSlot.StartUtc <= DateTime.UtcNow)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult.Failure("Cannot cancel an appointment that has already started or is in the past.");
            }

            // Update status and timestamp
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancelledAtUtc = DateTime.UtcNow;

            // Release SlotReservation: this frees the slot at the database level while retaining appointment history
            var reservation = appointment.SlotReservation ??
                await _dbContext.SlotReservations.FirstOrDefaultAsync(r => r.AppointmentSlotId == appointment.AppointmentSlotId);

            if (reservation != null)
            {
                _dbContext.SlotReservations.Remove(reservation);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Appointment {AppointmentId} cancelled successfully by user {UserId}.", appointmentId, userId);
            return BookingOperationResult.Success("Appointment has been cancelled successfully and the slot is now released.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error cancelling appointment {AppointmentId}.", appointmentId);
            return BookingOperationResult.Failure("An error occurred while cancelling the appointment. Please try again.");
        }
    }

    public async Task<BookingOperationResult<Appointment>> RescheduleAppointmentAsync(int appointmentId, int newSlotId, string userId, bool isPrivilegedUser)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // 1. Fetch current appointment
            var appointment = await _dbContext.Appointments
                .Include(a => a.AppointmentSlot)
                    .ThenInclude(s => s.Service)
                .Include(a => a.SlotReservation)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("The appointment to reschedule was not found.");
            }

            // Security check
            if (!isPrivilegedUser && appointment.CustomerId != userId)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("You are not authorized to reschedule this appointment.");
            }

            if (appointment.Status != AppointmentStatus.Booked)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("Only active 'Booked' appointments can be rescheduled.");
            }

            if (appointment.AppointmentSlot.StartUtc <= DateTime.UtcNow)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("Cannot reschedule an appointment that has already begun or passed.");
            }

            if (appointment.AppointmentSlotId == newSlotId)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("The selected new slot is identical to the current slot.");
            }

            // 2. Fetch new slot
            var newSlot = await _dbContext.AppointmentSlots
                .Include(s => s.Service)
                .Include(s => s.ProviderProfile)
                .Include(s => s.SlotReservation)
                .FirstOrDefaultAsync(s => s.Id == newSlotId);

            if (newSlot == null)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("The requested new slot does not exist.");
            }

            if (!newSlot.IsActive)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("The requested new slot is not active.");
            }

            if (newSlot.StartUtc <= DateTime.UtcNow)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("Cannot reschedule into a past time slot.");
            }

            if (!newSlot.Service.IsActive || !newSlot.ProviderProfile.IsActive)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("The service or provider for the new slot is currently inactive.");
            }

            // Ensure service matches
            if (newSlot.ServiceId != appointment.AppointmentSlot.ServiceId)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("Rescheduling must be for the same service.");
            }

            // Check if new slot is already reserved
            var isNewSlotReserved = await _dbContext.SlotReservations.AnyAsync(r => r.AppointmentSlotId == newSlotId);
            if (isNewSlotReserved || newSlot.SlotReservation != null)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("The selected new slot is already booked. Your original appointment has been preserved.");
            }

            // Check customer overlap with new slot (excluding current appointment)
            var customerOverlap = await _dbContext.Appointments
                .Include(a => a.AppointmentSlot)
                .AnyAsync(a => a.CustomerId == appointment.CustomerId &&
                               a.Id != appointment.Id &&
                               a.Status == AppointmentStatus.Booked &&
                               a.AppointmentSlot.StartUtc < newSlot.EndUtc &&
                               a.AppointmentSlot.EndUtc > newSlot.StartUtc);

            if (customerOverlap)
            {
                await transaction.RollbackAsync();
                return BookingOperationResult<Appointment>.Failure("You have another booked appointment during the new slot time window.");
            }

            // 3. Atomically release old reservation and create new reservation
            var oldReservation = appointment.SlotReservation ??
                await _dbContext.SlotReservations.FirstOrDefaultAsync(r => r.AppointmentSlotId == appointment.AppointmentSlotId);

            if (oldReservation != null)
            {
                _dbContext.SlotReservations.Remove(oldReservation);
                await _dbContext.SaveChangesAsync(); // Remove old reservation first
            }

            // Update appointment pointer
            appointment.AppointmentSlotId = newSlotId;
            appointment.AppointmentSlot = newSlot;

            // Add new reservation
            var newReservation = new SlotReservation
            {
                AppointmentSlotId = newSlotId,
                AppointmentId = appointment.Id,
                ReservedAtUtc = DateTime.UtcNow
            };
            _dbContext.SlotReservations.Add(newReservation);

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Appointment {AppointmentId} rescheduled successfully to slot {NewSlotId}.", appointmentId, newSlotId);
            return BookingOperationResult<Appointment>.Success(appointment, "Appointment rescheduled successfully!");
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Concurrency error during rescheduling appointment {AppointmentId} to slot {NewSlotId}.", appointmentId, newSlotId);
            return BookingOperationResult<Appointment>.Failure("The requested new slot could not be secured because another user reserved it. Your original appointment remains intact.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Unexpected error during rescheduling appointment {AppointmentId}.", appointmentId);
            return BookingOperationResult<Appointment>.Failure("Failed to reschedule appointment. Your original appointment remains intact.");
        }
    }

    public async Task<BookingOperationResult<AppointmentSlot>> CreateSlotAsync(int providerProfileId, int serviceId, DateTime startUtc)
    {
        if (startUtc <= DateTime.UtcNow)
        {
            return BookingOperationResult<AppointmentSlot>.Failure("Slot start time must be in the future.");
        }

        var provider = await _dbContext.ProviderProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == providerProfileId);

        if (provider == null || !provider.IsActive)
        {
            return BookingOperationResult<AppointmentSlot>.Failure("Provider profile not found or is currently inactive.");
        }

        var service = await _dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId);
        if (service == null || !service.IsActive)
        {
            return BookingOperationResult<AppointmentSlot>.Failure("Service not found or is currently inactive.");
        }

        var isAssigned = await _dbContext.ProviderServices
            .AnyAsync(ps => ps.ProviderProfileId == providerProfileId && ps.ServiceId == serviceId);

        if (!isAssigned)
        {
            return BookingOperationResult<AppointmentSlot>.Failure("The provider is not assigned to this service.");
        }

        var endUtc = startUtc.AddMinutes(service.DurationMinutes);

        // Check for overlapping slots for the same provider
        var hasOverlap = await _dbContext.AppointmentSlots
            .AnyAsync(s => s.ProviderProfileId == providerProfileId &&
                           s.IsActive &&
                           s.StartUtc < endUtc &&
                           s.EndUtc > startUtc);

        if (hasOverlap)
        {
            return BookingOperationResult<AppointmentSlot>.Failure("This slot overlaps with an existing slot in your schedule. Please select a non-overlapping time.");
        }

        var slot = new AppointmentSlot
        {
            ProviderProfileId = providerProfileId,
            ServiceId = serviceId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.AppointmentSlots.Add(slot);
        await _dbContext.SaveChangesAsync();

        return BookingOperationResult<AppointmentSlot>.Success(slot, "Appointment slot created successfully.");
    }

    public async Task<BookingOperationResult> DeleteOrDeactivateSlotAsync(int slotId, int providerProfileId)
    {
        var slot = await _dbContext.AppointmentSlots
            .Include(s => s.SlotReservation)
            .Include(s => s.Appointments)
            .FirstOrDefaultAsync(s => s.Id == slotId && s.ProviderProfileId == providerProfileId);

        if (slot == null)
        {
            return BookingOperationResult.Failure("Slot not found or you do not have permission to modify it.");
        }

        // Active reservation prevents removal
        if (slot.SlotReservation != null || slot.Appointments.Any(a => a.Status == AppointmentStatus.Booked))
        {
            return BookingOperationResult.Failure("Cannot remove this slot because it currently has an active booking. The customer must cancel first.");
        }

        // If the slot has past appointment records (completed/cancelled), soft-deactivate to preserve foreign keys
        if (slot.Appointments.Any())
        {
            slot.IsActive = false;
            await _dbContext.SaveChangesAsync();
            return BookingOperationResult.Success("Slot has been deactivated to preserve appointment history.");
        }

        // Otherwise, completely remove unreferenced slot
        _dbContext.AppointmentSlots.Remove(slot);
        await _dbContext.SaveChangesAsync();
        return BookingOperationResult.Success("Available slot removed successfully.");
    }

    public async Task<BookingOperationResult> UpdateAppointmentStatusAsync(int appointmentId, AppointmentStatus newStatus, int providerProfileId)
    {
        var appointment = await _dbContext.Appointments
            .Include(a => a.AppointmentSlot)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.AppointmentSlot.ProviderProfileId == providerProfileId);

        if (appointment == null)
        {
            return BookingOperationResult.Failure("Appointment not found or not assigned to you.");
        }

        if (newStatus != AppointmentStatus.Completed && newStatus != AppointmentStatus.NoShow)
        {
            return BookingOperationResult.Failure("Providers can only mark appointments as Completed or No-Show.");
        }

        if (appointment.Status != AppointmentStatus.Booked)
        {
            return BookingOperationResult.Failure($"Cannot change status of an appointment that is already {appointment.Status}.");
        }

        appointment.Status = newStatus;
        await _dbContext.SaveChangesAsync();

        return BookingOperationResult.Success($"Appointment marked as {newStatus}.");
    }
}

using BookMySlot.Models;

namespace BookMySlot.Services;

public interface IBookingService
{
    Task<BookingOperationResult<Appointment>> BookAppointmentAsync(string customerId, int slotId, string? notes);
    Task<BookingOperationResult> CancelAppointmentAsync(int appointmentId, string userId, bool isPrivilegedUser);
    Task<BookingOperationResult<Appointment>> RescheduleAppointmentAsync(int appointmentId, int newSlotId, string userId, bool isPrivilegedUser);
    Task<BookingOperationResult<AppointmentSlot>> CreateSlotAsync(int providerProfileId, int serviceId, DateTime startUtc);
    Task<BookingOperationResult> DeleteOrDeactivateSlotAsync(int slotId, int providerProfileId);
    Task<BookingOperationResult> UpdateAppointmentStatusAsync(int appointmentId, AppointmentStatus newStatus, int providerProfileId);
}

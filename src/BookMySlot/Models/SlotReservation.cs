using System.ComponentModel.DataAnnotations;

namespace BookMySlot.Models;

public class SlotReservation
{
    // Primary Key and Foreign Key to AppointmentSlot
    [Key]
    public int AppointmentSlotId { get; set; }
    public AppointmentSlot AppointmentSlot { get; set; } = null!;

    // Unique Foreign Key to Appointment
    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;

    public DateTime ReservedAtUtc { get; set; } = DateTime.UtcNow;
}

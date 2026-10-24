using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BookMySlot.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ProviderProfile? ProviderProfile { get; set; }
    public ICollection<Appointment> CustomerAppointments { get; set; } = new List<Appointment>();
}

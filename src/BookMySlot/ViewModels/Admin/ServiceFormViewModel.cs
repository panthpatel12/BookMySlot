using System.ComponentModel.DataAnnotations;

namespace BookMySlot.ViewModels.Admin;

public class ServiceFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Service name is required.")]
    [StringLength(120, ErrorMessage = "Service name cannot exceed 120 characters.")]
    [Display(Name = "Service Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Duration in minutes is required.")]
    [Range(5, 480, ErrorMessage = "Duration must be between 5 and 480 minutes.")]
    [Display(Name = "Duration (Minutes)")]
    public int DurationMinutes { get; set; } = 30;

    [Required(ErrorMessage = "Price is required.")]
    [Range(0, 1000000, ErrorMessage = "Price must be a positive amount.")]
    [Display(Name = "Price (₹)")]
    public decimal Price { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;
}

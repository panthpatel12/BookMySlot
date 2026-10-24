using System.ComponentModel.DataAnnotations;

namespace BookMySlot.ViewModels.Admin;

public class CreateProviderViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    [Display(Name = "Provider Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Provider Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Initial password is required.")]
    [StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Temporary / Initial Password")]
    public string InitialPassword { get; set; } = "Provider@2026";

    [MaxLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters.")]
    [Display(Name = "Professional Bio / Specialization")]
    public string? Bio { get; set; }

    [Display(Name = "Assign Services")]
    public List<int> SelectedServiceIds { get; set; } = new();
}

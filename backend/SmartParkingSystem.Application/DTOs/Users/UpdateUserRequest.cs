using System.ComponentModel.DataAnnotations;

namespace SmartParkingSystem.Application.DTOs.Users;

public class UpdateUserRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format.")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    public string? PhoneNumber { get; set; }

    public Guid? AssignedParkingAreaId { get; set; }
    public bool? IsActive { get; set; }
}

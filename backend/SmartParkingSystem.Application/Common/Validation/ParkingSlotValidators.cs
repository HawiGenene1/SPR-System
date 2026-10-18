using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.Common.Validation;

public static class ParkingSlotValidators
{
    private static readonly Regex SlotNumberRegex = new(
        @"^[A-Z0-9\s\-#]{1,20}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsValidSlotNumber(string? slotNumber)
    {
        if (string.IsNullOrWhiteSpace(slotNumber))
            return false;

        var trimmed = slotNumber.Trim();
        return trimmed.Length is >= 1 and <= 20 && SlotNumberRegex.IsMatch(trimmed);
    }

    public static bool IsValidFloor(int floor)
    {
        return floor is >= -5 and <= 50;
    }

    public static bool IsValidStatus(SlotStatus status)
    {
        return Enum.IsDefined(typeof(SlotStatus), status);
    }
}

public class ValidSlotNumberAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string slotNumber || string.IsNullOrWhiteSpace(slotNumber))
        {
            return new ValidationResult("Slot number is required.");
        }

        if (!ParkingSlotValidators.IsValidSlotNumber(slotNumber))
        {
            return new ValidationResult("Slot number must contain between 1 and 20 alphanumeric characters or hyphens.");
        }

        return ValidationResult.Success;
    }
}

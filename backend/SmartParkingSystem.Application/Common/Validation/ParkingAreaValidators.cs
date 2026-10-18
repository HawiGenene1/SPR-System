using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SmartParkingSystem.Application.Common.Validation;

public static class ParkingAreaValidators
{
    private static readonly Regex OperatingHoursRegex = new(
        @"^(?:(?:(?:Mon|Tue|Wed|Thu|Fri|Sat|Sun|Daily|Weekdays|Weekends)(?:\s*[-–]\s*(?:Mon|Tue|Wed|Thu|Fri|Sat|Sun))?[:\s]*)?(?:24\/7|24\s*hours|(?:[0-1]?[0-9]|2[0-3]):[0-5][0-9]\s*(?:AM|PM|am|pm)?\s*[-–]\s*(?:[0-1]?[0-9]|2[0-3]):[0-5][0-9]\s*(?:AM|PM|am|pm)?))$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsValidOperatingHours(string? hours)
    {
        if (string.IsNullOrWhiteSpace(hours))
            return false;

        var trimmed = hours.Trim();
        if (trimmed.Length < 3 || trimmed.Length > 100)
            return false;

        return OperatingHoursRegex.IsMatch(trimmed);
    }

    public static bool IsValidCapacity(int capacity)
    {
        return capacity is >= 1 and <= 10000;
    }
}

public class ValidOperatingHoursAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string hours || string.IsNullOrWhiteSpace(hours))
        {
            return new ValidationResult("Operating hours are required.");
        }

        if (!ParkingAreaValidators.IsValidOperatingHours(hours))
        {
            return new ValidationResult("Operating hours must be a valid schedule (e.g., '24/7' or '06:00 - 23:00').");
        }

        return ValidationResult.Success;
    }
}

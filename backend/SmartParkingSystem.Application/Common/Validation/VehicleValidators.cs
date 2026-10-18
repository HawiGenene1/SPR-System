using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.Common.Validation;

public static class VehicleValidators
{
    private static readonly Regex PlateFormatRegex = new(
        @"^[A-Za-z0-9]+(?:[ -][A-Za-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsValidPlate(string plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
            return false;

        var clean = plate.Trim();
        if (clean.Length < 2 || clean.Length > 20)
            return false;

        // Must contain at least 2 alphanumeric characters
        int alphaNumCount = clean.Count(char.IsLetterOrDigit);
        if (alphaNumCount < 2)
            return false;

        return PlateFormatRegex.IsMatch(clean);
    }

    public static bool IsValidVehicleType(VehicleType vehicleType)
    {
        return Enum.IsDefined(typeof(VehicleType), vehicleType);
    }

    public static string NormalizePlate(string plate)
    {
        if (string.IsNullOrWhiteSpace(plate))
            return string.Empty;

        var upper = plate.Trim().ToUpperInvariant();
        upper = Regex.Replace(upper, @"[\s\-]+", "-");
        return upper;
    }
}

public class ValidLicensePlateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string plate || string.IsNullOrWhiteSpace(plate))
        {
            return new ValidationResult("License plate is required.");
        }

        if (!VehicleValidators.IsValidPlate(plate))
        {
            return new ValidationResult("License plate must contain 2 to 20 valid alphanumeric characters, spaces, or hyphens.");
        }

        return ValidationResult.Success;
    }
}

public class ValidVehicleTypeAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is VehicleType vehicleType && VehicleValidators.IsValidVehicleType(vehicleType))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult("Invalid vehicle type specified.");
    }
}

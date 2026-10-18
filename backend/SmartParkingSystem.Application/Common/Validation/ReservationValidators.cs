using System.ComponentModel.DataAnnotations;
using SmartParkingSystem.Application.Common.BusinessLogic;
using SmartParkingSystem.Application.DTOs.Reservations;

namespace SmartParkingSystem.Application.Common.Validation;

public static class ReservationValidators
{
    public static (bool IsValid, string? ErrorMessage) ValidateReservationRequest(
        CreateReservationRequest request,
        DateTime? currentUtc = null)
    {
        if (request == null)
            return (false, "Reservation request cannot be null.");

        if (request.ParkingSlotId == Guid.Empty)
            return (false, "A valid parking slot ID is required.");

        if (request.VehicleId == Guid.Empty)
            return (false, "A valid vehicle ID is required.");

        var now = currentUtc ?? DateTime.UtcNow;
        return ReservationPricingCalculator.ValidateReservationTimes(request.StartTime, request.EndTime, now);
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class ValidReservationTimeWindowAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is CreateReservationRequest req)
        {
            var (isValid, errorMessage) = ReservationPricingCalculator.ValidateReservationTimes(
                req.StartTime, req.EndTime, DateTime.UtcNow);

            if (!isValid)
            {
                return new ValidationResult(errorMessage, new[] { nameof(req.StartTime), nameof(req.EndTime) });
            }
        }

        return ValidationResult.Success;
    }
}

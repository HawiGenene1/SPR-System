namespace SmartParkingSystem.Application.Common.BusinessLogic;

/// <summary>
/// Pure, unit-testable business logic for reservation duration, pricing calculations, overlap detection, and timing validations.
/// </summary>
public static class ReservationPricingCalculator
{
    /// <summary>
    /// Calculates duration in hours between start and end time.
    /// </summary>
    public static decimal CalculateDurationHours(DateTime startTime, DateTime endTime)
    {
        if (endTime <= startTime)
        {
            return 0m;
        }

        var duration = endTime - startTime;
        return (decimal)duration.TotalHours;
    }

    /// <summary>
    /// Calculates total reservation price given an hourly rate and time window.
    /// Strictly computed on the server, enforcing a minimum billing duration (default: 1 hour).
    /// </summary>
    public static decimal CalculateTotalPrice(decimal hourlyRate, DateTime startTime, DateTime endTime, decimal minimumHours = 1.0m)
    {
        if (hourlyRate <= 0m || endTime <= startTime)
        {
            return 0m;
        }

        var durationHours = CalculateDurationHours(startTime, endTime);
        var billableHours = Math.Max(durationHours, minimumHours);

        return Math.Round(billableHours * hourlyRate, 2);
    }

    /// <summary>
    /// Checks whether two time intervals overlap.
    /// [start1, end1] overlaps with [start2, end2] if and only if start1 &lt; end2 AND end1 &gt; start2.
    /// Adjacent intervals (e.g. end1 == start2) do NOT overlap.
    /// </summary>
    public static bool IsOverlapping(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
    {
        return start1 < end2 && end1 > start2;
    }

    /// <summary>
    /// Validates start and end times against system clock and minimum duration requirements.
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateReservationTimes(
        DateTime startTime,
        DateTime endTime,
        DateTime currentUtc,
        int pastToleranceMinutes = 5)
    {
        if (startTime < currentUtc.AddMinutes(-pastToleranceMinutes))
        {
            return (false, "Reservation entry time cannot be in the past.");
        }

        if (endTime <= startTime)
        {
            return (false, "Reservation entry time must be before exit time (end time must be after start time).");
        }

        if ((endTime - startTime).TotalMinutes < 15)
        {
            return (false, "Reservation duration must be at least 15 minutes.");
        }

        return (true, null);
    }
}

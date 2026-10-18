using System.ComponentModel.DataAnnotations;

namespace SmartParkingSystem.Application.DTOs.ParkingHistory;

public class ParkingHistoryFilterRequest
{
    /// <summary>
    /// Filter check-in history starting from this date/time (UTC).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Filter check-in history up to this date/time (UTC).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by specific parking facility.
    /// </summary>
    public Guid? ParkingAreaId { get; set; }

    /// <summary>
    /// Filter by specific customer vehicle.
    /// </summary>
    public Guid? VehicleId { get; set; }

    /// <summary>
    /// Optional search term across reservation number, license plate, slot number, or customer name.
    /// </summary>
    [StringLength(100, ErrorMessage = "Search query cannot exceed 100 characters.")]
    public string? Search { get; set; }

    /// <summary>
    /// 1-based page index (default: 1).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than or equal to 1.")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of records per page (default: 10, max: 100).
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 10;
}

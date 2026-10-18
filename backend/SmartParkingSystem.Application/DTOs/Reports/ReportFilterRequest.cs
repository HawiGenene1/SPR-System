namespace SmartParkingSystem.Application.DTOs.Reports;

/// <summary>
/// Query filter request parameters for generating analytics reports.
/// Supports date range presets (daily, weekly, monthly, custom), explicit UTC start/end dates,
/// facility filtering, search criteria, and pagination.
/// </summary>
public class ReportFilterRequest
{
    /// <summary>
    /// Reporting period preset: "daily", "weekly", "monthly", or "custom".
    /// </summary>
    public string? Period { get; set; }

    /// <summary>
    /// Explicit start date for custom range (inclusive).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Explicit end date for custom range (inclusive).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Optional parking facility filter.
    /// </summary>
    public Guid? ParkingAreaId { get; set; }

    /// <summary>
    /// Optional search keyword (e.g. customer name or email).
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// 1-based page number for tabular paginated data.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size for tabular paginated data.
    /// </summary>
    public int PageSize { get; set; } = 10;
}

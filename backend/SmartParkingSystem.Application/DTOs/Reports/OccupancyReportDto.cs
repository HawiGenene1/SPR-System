using SmartParkingSystem.Application.DTOs.Common;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Reports;

/// <summary>
/// Facility occupancy analytics report suitable for Angular charts and tables.
/// Includes live &amp; period occupancy rates, slot-type and hourly distributions,
/// and facility-by-facility inventory tables with pagination.
/// </summary>
public class OccupancyReportDto
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Period { get; set; } = string.Empty;

    // Filter Scope
    public Guid? ParkingAreaId { get; set; }
    public string? ParkingAreaName { get; set; }

    // Summary KPIs
    public int TotalSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public int AvailableSlots { get; set; }
    public int ReservedSlots { get; set; }
    public int MaintenanceSlots { get; set; }
    public double OccupancyRatePercentage { get; set; }
    public double AverageTurnoverRate { get; set; }

    // Chart Series
    public List<SlotTypeOccupancyDto> OccupancyBySlotType { get; set; } = new();
    public List<HourlyOccupancyDto> HourlyOccupancy { get; set; } = new();
    public List<PeriodOccupancyDto> OccupancyByPeriod { get; set; } = new();
    public List<ParkingAreaOccupancyDto> OccupancyByParkingArea { get; set; } = new();

    // Tabular Paginated Facilities Table
    public PagedResultDto<FacilityOccupancyTableDto> Facilities { get; set; } = new();
}

public class SlotTypeOccupancyDto
{
    public SlotType SlotType { get; set; }
    public string SlotTypeName => SlotType.ToString();
    public int TotalSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public int AvailableSlots => Math.Max(0, TotalSlots - OccupiedSlots);
    public double OccupancyRatePercentage { get; set; }
}

public class HourlyOccupancyDto
{
    public int Hour { get; set; }
    public string HourLabel { get; set; } = string.Empty;
    public double AverageOccupancyRate { get; set; }
    public int CheckInCount { get; set; }
    public bool IsPeakHour { get; set; }
}

public class PeriodOccupancyDto
{
    public DateTime Date { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public double OccupancyRatePercentage { get; set; }
    public int PeakOccupiedSlots { get; set; }
    public int TotalCheckIns { get; set; }
}

public class ParkingAreaOccupancyDto
{
    public Guid ParkingAreaId { get; set; }
    public string ParkingAreaName { get; set; } = string.Empty;
    public int TotalSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public int AvailableSlots { get; set; }
    public double OccupancyRatePercentage { get; set; }
}

public class FacilityOccupancyTableDto
{
    public Guid ParkingAreaId { get; set; }
    public string ParkingAreaName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public int TotalSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public int ReservedSlots { get; set; }
    public int AvailableSlots { get; set; }
    public int MaintenanceSlots { get; set; }
    public double OccupancyRatePercentage { get; set; }
    public int TotalCheckInsInPeriod { get; set; }
    public bool IsActive { get; set; }
}

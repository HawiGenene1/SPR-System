using SmartParkingSystem.Application.DTOs.Common;

namespace SmartParkingSystem.Application.DTOs.Reports;

/// <summary>
/// Customer analytics report suitable for Angular charts and tables.
/// Includes growth metrics, spending aggregations, activity tier distribution,
/// and paginated customer activity tables with search.
/// </summary>
public class CustomerReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Period { get; set; } = string.Empty;

    // Summary KPIs
    public int TotalCustomers { get; set; }
    public int NewCustomersInPeriod { get; set; }
    public int ActiveCustomersInPeriod { get; set; }
    public decimal TotalCustomerSpend { get; set; }
    public decimal AverageSpendPerCustomer { get; set; }
    public int TotalCustomerReservations { get; set; }

    // Chart Series
    public List<PeriodCustomerDto> RegistrationsByPeriod { get; set; } = new();
    public Dictionary<string, int> CustomerActivityTiers { get; set; } = new();

    // Tabular Paginated Customers Table
    public PagedResultDto<CustomerActivityDto> Customers { get; set; } = new();
}

public class CustomerActivityDto
{
    public Guid CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime RegisteredAt { get; set; }
    public int TotalReservations { get; set; }
    public int CompletedReservations { get; set; }
    public int CancelledReservations { get; set; }
    public decimal TotalSpend { get; set; }
    public int RegisteredVehiclesCount { get; set; }
    public DateTime? LastReservationDate { get; set; }
    public string ActivityTier { get; set; } = "Inactive";
}

public class PeriodCustomerDto
{
    public DateTime Date { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public int NewRegistrationsCount { get; set; }
}

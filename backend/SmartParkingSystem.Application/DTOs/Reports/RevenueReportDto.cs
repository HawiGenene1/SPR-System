using SmartParkingSystem.Application.DTOs.Common;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.DTOs.Reports;

/// <summary>
/// Financial revenue analytics report suitable for Angular charts and tables.
/// Includes executive summaries, time-series data, breakdowns by payment method and area,
/// and paginated transactional records.
/// </summary>
public class RevenueReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Period { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal PaidPaymentsRevenue { get; set; }
    public decimal OverstayFeesRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageTransactionValue { get; set; }

    // Chart Series
    public Dictionary<string, decimal> RevenueByPaymentMethod { get; set; } = new();
    public List<ParkingAreaRevenueDto> RevenueByParkingArea { get; set; } = new();
    public List<DailyRevenueDto> RevenueByPeriod { get; set; } = new();
    public List<DailyRevenueDto> DailyRevenue
    {
        get => RevenueByPeriod;
        set => RevenueByPeriod = value;
    }

    // Tabular Paginated Transactions
    public PagedResultDto<RevenueTransactionDto> Transactions { get; set; } = new();
}

public class ParkingAreaRevenueDto
{
    public Guid ParkingAreaId { get; set; }
    public string ParkingAreaName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int TotalTransactions { get; set; }
    public double Percentage { get; set; }
}

public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int TransactionsCount { get; set; }
}

public class RevenueTransactionDto
{
    public Guid PaymentId { get; set; }
    public Guid ReservationId { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string ParkingAreaName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodName => PaymentMethod.ToString();
    public PaymentStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? TransactionId { get; set; }
    public DateTime PaidAt { get; set; }
}

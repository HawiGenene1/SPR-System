using SmartParkingSystem.Application.DTOs.Parking;

namespace SmartParkingSystem.Application.Interfaces;

public interface IParkingService
{
    /// <summary>
    /// Processes vehicle check-in by scanning a customer's reservation QR code.
    /// Strictly verifies QR validity, reservation status, scheduled time window, and completed payment.
    /// Records entry time, attendant identity, sets reservation status to Active, and slot to Occupied.
    /// </summary>
    Task<CheckInResultDto> CheckInVehicleAsync(
        CheckInRequest request,
        Guid attendantId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes vehicle check-out by scanning a customer's reservation QR code.
    /// Strictly verifies vehicle was checked in, reservation is Active, calculates actual duration and overstay fees,
    /// sets reservation to Completed, slot to Available, and records exit in ParkingHistory with attendant attribution.
    /// </summary>
    Task<CheckOutResultDto> CheckOutVehicleAsync(
        CheckOutRequest request,
        Guid attendantId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);
}

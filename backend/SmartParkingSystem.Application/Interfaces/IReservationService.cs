using SmartParkingSystem.Application.DTOs.Reservations;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.Interfaces;

public interface IReservationService
{
    Task<ReservationDto> CreateReservationAsync(
        CreateReservationRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationDto>> GetCustomerReservationsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> GetReservationByIdAsync(
        Guid id,
        Guid? currentUserId = null,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> CancelReservationAsync(
        Guid id,
        Guid currentUserId,
        string? reason = null,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationDto>> GetAllReservationsAsync(
        Guid? parkingAreaId = null,
        ReservationStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    Task<ReservationDto> UpdateReservationStatusAsync(
        Guid id,
        ReservationStatus newStatus,
        string? note = null,
        CancellationToken cancellationToken = default);
}

using SmartParkingSystem.Application.DTOs.Payments;
using SmartParkingSystem.Domain.Enums;

namespace SmartParkingSystem.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto> ProcessPaymentAsync(
        CreatePaymentRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<PaymentDto> ConfirmPaymentAsync(
        Guid paymentId,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    Task<PaymentDto> GetPaymentByIdAsync(
        Guid paymentId,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    Task<PaymentStatusDto> GetPaymentStatusAsync(
        Guid paymentId,
        Guid currentUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentDto>> GetCustomerPaymentsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentDto>> GetAllPaymentsAsync(
        PaymentStatus? status = null,
        PaymentMethod? method = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}

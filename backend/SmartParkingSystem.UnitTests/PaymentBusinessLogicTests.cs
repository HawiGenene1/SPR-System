using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.Payments;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;
using SmartParkingSystem.Infrastructure.Repositories;
using SmartParkingSystem.Infrastructure.Services;

namespace SmartParkingSystem.UnitTests;

public static class PaymentBusinessLogicTests
{
    private static int _passed = 0;
    private static int _failed = 0;

    public static int Run()
    {
        _passed = 0;
        _failed = 0;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        Console.WriteLine(" Running Payment Business Logic & Gateway Unit Tests");
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        // 1. Gateway Abstraction & Simulation Tests
        TestGatewayCardSuccess();
        TestGatewayMobileMoneySuccess();
        TestGatewayCashAndOnlineSuccess();
        TestGatewayDeclineSimulation();
        TestGatewayRefundSimulation();
        TestSensitiveDataMaskingIntegrity();
        TestPaymentStatusDtoIntegrity();

        // 2. Step 12 Payment Business Logic Tests
        TestPaymentCreationAndSuccessConfirmsReservation();
        TestCustomerCannotPayForAnotherCustomersReservation();
        TestCannotPayForCancelledOrCompletedReservation();
        TestDuplicatePaymentPreventionOnSameReservation();
        TestPaymentConfirmation();
        TestPaymentAmountMismatchRejected();
        TestCustomerPaymentHistoryIsolation();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=========================================================");
        if (_failed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" Payment Tests Summary: {_passed} Passed, {_failed} Failed");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Payment Tests Summary: {_passed} Passed, {_failed} Failed");
        }
        Console.WriteLine("=========================================================");
        Console.ResetColor();

        return _failed == 0 ? 0 : 1;
    }

    private static void Assert(bool condition, string testName)
    {
        if (condition)
        {
            _passed++;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [PASS] {testName}");
            Console.ResetColor();
        }
        else
        {
            _failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {testName}");
            Console.ResetColor();
        }
    }

    private static (ApplicationDbContext context, IPaymentService service, IPaymentGateway gateway) CreateMockContextAndService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var paymentRepo = new PaymentRepository(context);
        var gateway = new MockPaymentGateway(NullLogger<MockPaymentGateway>.Instance);
        var service = new PaymentService(context, paymentRepo, gateway, NullLogger<PaymentService>.Instance);

        return (context, service, gateway);
    }

    private static (User customer, ParkingSlot slot, Reservation reservation) SeedPaymentTestData(ApplicationDbContext context)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = AppRoles.Customer };
        context.Roles.Add(role);

        var customer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Alice Walker",
            Email = "alice@walker.com",
            PasswordHash = "hash",
            RoleId = role.Id,
            Role = role
        };
        context.Users.Add(customer);

        var area = new ParkingArea
        {
            Id = Guid.NewGuid(),
            Name = "Metro Terminal",
            Address = "500 Airport Way",
            City = "Metropolis",
            HourlyRate = 10.00m
        };
        context.ParkingAreas.Add(area);

        var slot = new ParkingSlot
        {
            Id = Guid.NewGuid(),
            ParkingAreaId = area.Id,
            SlotNumber = "P1-10",
            FloorNumber = 1,
            HourlyRate = 10.00m,
            Status = SlotStatus.Available,
            VehicleType = VehicleType.Car
        };
        context.ParkingSlots.Add(slot);

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            UserId = customer.Id,
            LicensePlate = "PAY-111",
            Make = "Tesla",
            Model = "Model 3",
            VehicleType = VehicleType.Car
        };
        context.Vehicles.Add(vehicle);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ReservationNumber = $"RES-PAY-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            UserId = customer.Id,
            User = customer,
            ParkingSlotId = slot.Id,
            ParkingSlot = slot,
            VehicleId = vehicle.Id,
            Vehicle = vehicle,
            StartTime = DateTime.UtcNow.AddMinutes(10), // within 15m window
            EndTime = DateTime.UtcNow.AddHours(2),
            TotalPrice = 20.00m,
            Status = ReservationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        context.Reservations.Add(reservation);

        context.SaveChanges();
        return (customer, slot, reservation);
    }

    private static void TestGatewayCardSuccess()
    {
        Console.WriteLine("\n[1] Payment Gateway Card Processing Tests:");
        var gateway = new MockPaymentGateway(NullLogger<MockPaymentGateway>.Instance);
        var request = new PaymentGatewayRequest
        {
            TransactionReference = "TXN-TEST-001",
            Amount = 50.00m,
            Currency = "ETB",
            PaymentMethod = PaymentMethod.Card,
            CustomerEmail = "customer@example.com",
            CustomerName = "John Doe"
        };

        var result = gateway.ProcessPaymentAsync(request).GetAwaiter().GetResult();

        Assert(result.IsSuccess, "Card payment returns IsSuccess = true");
        Assert(result.Status == PaymentStatus.Paid, "Card payment status is Paid");
        Assert(result.TransactionReference == "TXN-TEST-001", "Transaction reference matches request");
        Assert(!string.IsNullOrWhiteSpace(result.GatewayReference), "Gateway reference is generated");
        Assert(result.MaskedAccountIdentifier == "**** **** **** 4242", "Account is masked and does not expose PAN");
    }

    private static void TestGatewayMobileMoneySuccess()
    {
        Console.WriteLine("\n[2] Payment Gateway Mobile Money Tests:");
        var gateway = new MockPaymentGateway(NullLogger<MockPaymentGateway>.Instance);
        var request = new PaymentGatewayRequest
        {
            TransactionReference = "TXN-TEST-MM01",
            Amount = 25.00m,
            Currency = "ETB",
            PaymentMethod = PaymentMethod.MobileMoney,
            CustomerEmail = "customer@example.com",
            CustomerName = "Jane Doe"
        };

        var result = gateway.ProcessPaymentAsync(request).GetAwaiter().GetResult();

        Assert(result.IsSuccess, "MobileMoney payment returns IsSuccess = true");
        Assert(result.Status == PaymentStatus.Paid, "MobileMoney payment status is Paid");
        Assert(result.MaskedAccountIdentifier == "+251***789", "Mobile money number is properly masked");
    }

    private static void TestGatewayCashAndOnlineSuccess()
    {
        Console.WriteLine("\n[3] Payment Gateway Cash & Online Tests:");
        var gateway = new MockPaymentGateway(NullLogger<MockPaymentGateway>.Instance);

        var cashResult = gateway.ProcessPaymentAsync(new PaymentGatewayRequest
        {
            TransactionReference = "TXN-TEST-CASH01",
            Amount = 15.00m,
            PaymentMethod = PaymentMethod.Cash
        }).GetAwaiter().GetResult();

        Assert(cashResult.IsSuccess, "Cash payment returns IsSuccess = true");
        Assert(cashResult.MaskedAccountIdentifier?.Contains("Cash") == true, "Cash payment indicates receipt pending");

        var onlineResult = gateway.ProcessPaymentAsync(new PaymentGatewayRequest
        {
            TransactionReference = "TXN-TEST-ONL01",
            Amount = 30.00m,
            PaymentMethod = PaymentMethod.Online
        }).GetAwaiter().GetResult();

        Assert(onlineResult.IsSuccess, "Online payment returns IsSuccess = true");
        Assert(onlineResult.MaskedAccountIdentifier?.Contains("***56") == true, "Online account identifier is masked");
    }

    private static void TestGatewayDeclineSimulation()
    {
        Console.WriteLine("\n[4] Payment Gateway Failure/Decline Simulation Tests:");
        var gateway = new MockPaymentGateway(NullLogger<MockPaymentGateway>.Instance);
        var request = new PaymentGatewayRequest
        {
            TransactionReference = "TXN-TEST-FAIL01",
            Amount = 40.00m,
            PaymentMethod = PaymentMethod.Card,
            PaymentToken = "DECLINED"
        };

        var result = gateway.ProcessPaymentAsync(request).GetAwaiter().GetResult();

        Assert(!result.IsSuccess, "Declined token returns IsSuccess = false");
        Assert(result.Status == PaymentStatus.Failed, "Declined payment status is Failed");
        Assert(!string.IsNullOrWhiteSpace(result.ErrorMessage), "Declined payment includes clear error message");
    }

    private static void TestGatewayRefundSimulation()
    {
        Console.WriteLine("\n[5] Payment Gateway Refund Simulation Tests:");
        var gateway = new MockPaymentGateway(NullLogger<MockPaymentGateway>.Instance);

        var refundResult = gateway.RefundPaymentAsync("TXN-TEST-REF01", 50.00m).GetAwaiter().GetResult();

        Assert(refundResult.IsSuccess, "Refund returns IsSuccess = true");
        Assert(refundResult.Status == PaymentStatus.Refunded, "Refund status is Refunded");
        Assert(refundResult.GatewayReference?.StartsWith("RF-") == true, "Refund gateway reference has RF- prefix");
    }

    private static void TestSensitiveDataMaskingIntegrity()
    {
        Console.WriteLine("\n[6] Sensitive Data Protection & Non-exposure Tests:");
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 45.00m,
            PaymentMethod = PaymentMethod.Card,
            Status = PaymentStatus.Paid,
            TransactionId = "TXN-20260920-ABC123",
            PaidAt = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            ReservationId = Guid.NewGuid()
        };

        var dto = new PaymentDto
        {
            Id = payment.Id,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            Status = payment.Status,
            TransactionId = payment.TransactionId,
            MaskedAccount = "**** **** **** 4242",
            PaidAt = payment.PaidAt,
            ReservationId = payment.ReservationId,
            UserId = payment.UserId
        };

        Assert(!dto.MaskedAccount.Contains("12345678"), "Payment DTO does not leak unmasked account numbers");
        Assert(dto.MaskedAccount.StartsWith("****"), "Payment DTO correctly masks leading account digits");
        Assert(dto.PaymentMethodName == "Card", "PaymentMethodName resolves correctly");
        Assert(dto.StatusName == "Paid", "StatusName resolves correctly");
        Assert(dto.TransactionReference == payment.TransactionId, "TransactionReference aliases TransactionId");
    }

    private static void TestPaymentStatusDtoIntegrity()
    {
        Console.WriteLine("\n[7] Payment Status DTO Tests:");
        var statusDto = new PaymentStatusDto
        {
            PaymentId = Guid.NewGuid(),
            ReservationId = Guid.NewGuid(),
            ReservationNumber = "RES-20260920-001",
            ReservationStatus = ReservationStatus.Confirmed,
            Amount = 35.00m,
            Status = PaymentStatus.Paid,
            PaymentMethod = PaymentMethod.MobileMoney,
            TransactionReference = "TXN-MM-999",
            MaskedAccount = "+251***789",
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        Assert(statusDto.StatusName == "Paid", "StatusName is Paid");
        Assert(statusDto.PaymentMethodName == "MobileMoney", "PaymentMethodName is MobileMoney");
        Assert(statusDto.ReservationStatusName == "Confirmed", "ReservationStatusName is Confirmed");
        Assert(statusDto.MaskedAccount == "+251***789", "MaskedAccount preserves security mask");
    }

    private static void TestPaymentCreationAndSuccessConfirmsReservation()
    {
        Console.WriteLine("\n[8] Rule: Payment Success Confirms Reservation & Synchronizes Slot:");
        var (context, service, _) = CreateMockContextAndService();
        var (customer, slot, reservation) = SeedPaymentTestData(context);

        Assert(reservation.Status == ReservationStatus.Pending, "Initial reservation status is Pending");

        // Customer processes payment
        var result = service.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = reservation.Id,
            PaymentMethod = PaymentMethod.Card,
            Amount = 20.00m
        }, customer.Id).GetAwaiter().GetResult();

        Assert(result.Status == PaymentStatus.Paid, "Payment result status is Paid");
        Assert(result.Amount == 20.00m, "Payment amount is $20.00");
        Assert(!string.IsNullOrWhiteSpace(result.TransactionReference), "Transaction reference is generated");
        Assert(result.MaskedAccount == "**** **** **** 4242", "Account is securely masked");

        // Reload reservation and slot
        context.Entry(reservation).Reload();
        context.Entry(slot).Reload();

        Assert(reservation.Status == ReservationStatus.Confirmed, "Reservation status transitioned to Confirmed upon payment");
        Assert(slot.Status == SlotStatus.Reserved, "Slot status transitioned to Reserved for upcoming session");
        
        // Entrance QR code generated
        var qrCode = context.QRCodes.FirstOrDefault(q => q.ReservationId == reservation.Id);
        Assert(qrCode != null, "Entrance QR code was created");
        Assert(!string.IsNullOrWhiteSpace(qrCode?.ImageData), "Entrance QR code has rendered image data");
    }

    private static void TestCustomerCannotPayForAnotherCustomersReservation()
    {
        Console.WriteLine("\n[9] Rule: Customer Cannot Pay for Another Customer's Reservation:");
        var (context, service, _) = CreateMockContextAndService();
        var (_, _, reservation) = SeedPaymentTestData(context);

        var intruderRole = context.Roles.FirstOrDefault(r => r.Name == AppRoles.Customer)
            ?? new Role { Id = Guid.NewGuid(), Name = AppRoles.Customer };

        var intruder = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Intruder User",
            Email = "intruder@domain.com",
            RoleId = intruderRole.Id,
            Role = intruderRole
        };
        context.Users.Add(intruder);
        context.SaveChanges();

        bool unauthorizedCaught = false;
        try
        {
            service.ProcessPaymentAsync(new CreatePaymentRequest
            {
                ReservationId = reservation.Id,
                PaymentMethod = PaymentMethod.Card
            }, intruder.Id).GetAwaiter().GetResult();
        }
        catch (UnauthorizedAccessException ex)
        {
            unauthorizedCaught = ex.Message.Contains("not authorized to pay for another customer's reservation");
        }

        Assert(unauthorizedCaught, "Paying for another user's reservation throws UnauthorizedAccessException");
    }

    private static void TestCannotPayForCancelledOrCompletedReservation()
    {
        Console.WriteLine("\n[10] Rule: Cannot Pay for Cancelled or Completed Reservation:");
        var (context, service, _) = CreateMockContextAndService();
        var (customer, _, reservation) = SeedPaymentTestData(context);

        // Cancel the reservation
        reservation.Status = ReservationStatus.Cancelled;
        context.SaveChanges();

        bool cancelledCaught = false;
        try
        {
            service.ProcessPaymentAsync(new CreatePaymentRequest
            {
                ReservationId = reservation.Id,
                PaymentMethod = PaymentMethod.MobileMoney
            }, customer.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            cancelledCaught = ex.Message.Contains("cancelled");
        }
        Assert(cancelledCaught, "Payment for cancelled reservation rejected with InvalidOperationException");

        // Mark completed
        reservation.Status = ReservationStatus.Completed;
        context.SaveChanges();

        bool completedCaught = false;
        try
        {
            service.ProcessPaymentAsync(new CreatePaymentRequest
            {
                ReservationId = reservation.Id,
                PaymentMethod = PaymentMethod.Card
            }, customer.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            completedCaught = ex.Message.Contains("completed");
        }
        Assert(completedCaught, "Payment for completed reservation rejected with InvalidOperationException");
    }

    private static void TestDuplicatePaymentPreventionOnSameReservation()
    {
        Console.WriteLine("\n[11] Rule: Duplicate Payment Prevention on Same Reservation:");
        var (context, service, _) = CreateMockContextAndService();
        var (customer, _, reservation) = SeedPaymentTestData(context);

        // First payment succeeds
        var firstPayment = service.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = reservation.Id,
            PaymentMethod = PaymentMethod.Online
        }, customer.Id).GetAwaiter().GetResult();
        Assert(firstPayment.Status == PaymentStatus.Paid, "First payment succeeded");

        // Second payment attempt rejected
        bool duplicateCaught = false;
        try
        {
            service.ProcessPaymentAsync(new CreatePaymentRequest
            {
                ReservationId = reservation.Id,
                PaymentMethod = PaymentMethod.Online
            }, customer.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            duplicateCaught = ex.Message.Contains("already been paid for");
        }
        Assert(duplicateCaught, "Duplicate payment attempt rejected with InvalidOperationException");
    }

    private static void TestPaymentConfirmation()
    {
        Console.WriteLine("\n[12] Payment Confirmation Endpoint Tests:");
        var (context, service, _) = CreateMockContextAndService();
        var (customer, slot, reservation) = SeedPaymentTestData(context);

        // Create pending payment manually (e.g. Cash / Offline)
        var pendingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            UserId = customer.Id,
            Amount = reservation.TotalPrice,
            PaymentMethod = PaymentMethod.Cash,
            TransactionId = "TXN-CASH-MANUAL",
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        context.Payments.Add(pendingPayment);
        context.SaveChanges();

        // Confirm payment
        var confirmed = service.ConfirmPaymentAsync(pendingPayment.Id, customer.Id).GetAwaiter().GetResult();
        Assert(confirmed.Status == PaymentStatus.Paid, "Payment status confirmed to Paid");

        context.Entry(reservation).Reload();
        Assert(reservation.Status == ReservationStatus.Confirmed, "Reservation status transitioned to Confirmed");
    }

    private static void TestPaymentAmountMismatchRejected()
    {
        Console.WriteLine("\n[13] Rule: Client Amount Mismatch Validation:");
        var (context, service, _) = CreateMockContextAndService();
        var (customer, _, reservation) = SeedPaymentTestData(context);

        // Reservation total is $20.00. Client attempts to pay $5.00
        bool mismatchCaught = false;
        try
        {
            service.ProcessPaymentAsync(new CreatePaymentRequest
            {
                ReservationId = reservation.Id,
                PaymentMethod = PaymentMethod.Card,
                Amount = 5.00m // Mismatched amount
            }, customer.Id).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException ex)
        {
            mismatchCaught = ex.Message.Contains("does not match");
        }
        Assert(mismatchCaught, "Mismatched payment amount rejected with InvalidOperationException");
    }

    private static void TestCustomerPaymentHistoryIsolation()
    {
        Console.WriteLine("\n[14] Customer Payment History Isolation Tests:");
        var (context, service, _) = CreateMockContextAndService();
        var (customerA, _, reservationA) = SeedPaymentTestData(context);

        // Process payment for customer A
        service.ProcessPaymentAsync(new CreatePaymentRequest
        {
            ReservationId = reservationA.Id,
            PaymentMethod = PaymentMethod.Card
        }, customerA.Id).GetAwaiter().GetResult();

        // Customer B
        var customerB = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Bob Smith",
            Email = "bob@smith.com",
            RoleId = customerA.RoleId
        };
        context.Users.Add(customerB);
        context.SaveChanges();

        var customerAPayments = service.GetCustomerPaymentsAsync(customerA.Id).GetAwaiter().GetResult();
        var customerBPayments = service.GetCustomerPaymentsAsync(customerB.Id).GetAwaiter().GetResult();

        Assert(customerAPayments.Count == 1, "Customer A has 1 payment in history");
        Assert(customerBPayments.Count == 0, "Customer B has 0 payments in history (proper isolation)");
    }
}

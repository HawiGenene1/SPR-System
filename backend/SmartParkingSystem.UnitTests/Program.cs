using SmartParkingSystem.UnitTests;

var reservationExit = ReservationBusinessLogicTests.Run();
var paymentExit = PaymentBusinessLogicTests.Run();
var qrExit = QRCodeBusinessLogicTests.Run();
var checkInExit = VehicleCheckInBusinessLogicTests.Run();
var checkOutExit = VehicleCheckOutBusinessLogicTests.Run();
var vehicleExit = VehicleManagementBusinessLogicTests.Run();
var parkingAreaExit = ParkingAreaManagementBusinessLogicTests.Run();
var parkingSlotExit = ParkingSlotManagementBusinessLogicTests.Run();
var parkingHistoryExit = ParkingHistoryBusinessLogicTests.Run();
var adminDashboardExit = AdminDashboardBusinessLogicTests.Run();
var attendantDashboardExit = AttendantDashboardBusinessLogicTests.Run();
var customerDashboardExit = CustomerDashboardBusinessLogicTests.Run();
var reportingExit = ReportingBusinessLogicTests.Run();
var exceptionMiddlewareExit = ExceptionMiddlewareBusinessLogicTests.Run();
var coreBusinessRulesExit = CoreBusinessRulesTests.Run();

var finalExitCode = (reservationExit != 0 || paymentExit != 0 || qrExit != 0 || checkInExit != 0 || checkOutExit != 0 || vehicleExit != 0 || parkingAreaExit != 0 || parkingSlotExit != 0 || parkingHistoryExit != 0 || adminDashboardExit != 0 || attendantDashboardExit != 0 || customerDashboardExit != 0 || reportingExit != 0 || exceptionMiddlewareExit != 0 || coreBusinessRulesExit != 0) ? 1 : 0;
Environment.Exit(finalExitCode);

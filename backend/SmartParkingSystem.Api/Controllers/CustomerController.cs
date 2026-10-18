using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartParkingSystem.Application.Common.Security;
using SmartParkingSystem.Application.DTOs.ParkingAreas;
using SmartParkingSystem.Application.DTOs.Payments;
using SmartParkingSystem.Application.DTOs.QRCodes;
using SmartParkingSystem.Application.DTOs.Reservations;
using SmartParkingSystem.Application.DTOs.Vehicles;
using SmartParkingSystem.Application.Interfaces;
using SmartParkingSystem.Domain.Entities;
using SmartParkingSystem.Domain.Enums;
using SmartParkingSystem.Infrastructure.Data;

namespace SmartParkingSystem.Api.Controllers;

[ApiController]
[Route("api/customer")]
[Authorize(Policy = AuthorizationPolicies.RequireCustomer)]
public class CustomerController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IVehicleService _vehicleService;
    private readonly IParkingAreaService _parkingAreaService;
    private readonly IReservationService _reservationService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(
        ApplicationDbContext context,
        IVehicleService vehicleService,
        IParkingAreaService parkingAreaService,
        IReservationService reservationService,
        IPaymentService paymentService,
        ILogger<CustomerController> logger)
    {
        _context = context;
        _vehicleService = vehicleService;
        _parkingAreaService = parkingAreaService;
        _reservationService = reservationService;
        _paymentService = paymentService;
        _logger = logger;
    }

    #region Parking Areas

    /// <summary>
    /// Customer: List all active parking facilities with live availability counts.
    /// </summary>
    [HttpGet("parking-areas")]
    [ProducesResponseType(typeof(IReadOnlyList<ParkingAreaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParkingAreas([FromQuery] string? city, CancellationToken cancellationToken)
    {
        var areas = await _parkingAreaService.GetAllAsync(includeInactive: false, city: city, cancellationToken);
        return Ok(areas);
    }

    /// <summary>
    /// Customer: Retrieve parking facility details by ID.
    /// </summary>
    [HttpGet("parking-areas/{id:guid}")]
    [ProducesResponseType(typeof(ParkingAreaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParkingAreaById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var area = await _parkingAreaService.GetByIdAsync(id, cancellationToken);
            return Ok(area);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region My Vehicles

    /// <summary>
    /// Customer: List all registered vehicles belonging to the current user.
    /// </summary>
    [HttpGet("my-vehicles")]
    [ProducesResponseType(typeof(List<VehicleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyVehicles()
    {
        var userId = GetCurrentUserId();
        var vehicles = await _vehicleService.GetMyVehiclesAsync(userId);
        return Ok(vehicles);
    }

    /// <summary>
    /// Customer: Register a new personal vehicle.
    /// </summary>
    [HttpPost("my-vehicles")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterMyVehicle([FromBody] CreateVehicleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var vehicle = await _vehicleService.CreateVehicleAsync(userId, request);
            return StatusCode(StatusCodes.Status201Created, vehicle);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region My Reservations

    /// <summary>
    /// Customer: List all parking reservations made by the current user.
    /// </summary>
    [HttpGet("my-reservations")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReservations(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var reservations = await _reservationService.GetCustomerReservationsAsync(userId, cancellationToken);
        return Ok(reservations);
    }

    /// <summary>
    /// Customer: Create a new slot reservation.
    /// </summary>
    [HttpPost("my-reservations")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateMyReservation([FromBody] CreateReservationRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var created = await _reservationService.CreateReservationAsync(request, userId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region My Payments

    /// <summary>
    /// Customer: List all payment transactions made by the current user.
    /// </summary>
    [HttpGet("my-payments")]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPayments(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var payments = await _paymentService.GetCustomerPaymentsAsync(userId, cancellationToken);
        return Ok(payments);
    }

    #endregion

    #region My Parking History

    /// <summary>
    /// Customer: Retrieve past parking history check-ins and check-outs.
    /// </summary>
    [HttpGet("my-parking-history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyParkingHistory()
    {
        var userId = GetCurrentUserId();

        var history = await _context.ParkingHistories
            .Include(h => h.Reservation)
            .Include(h => h.Vehicle)
            .Include(h => h.ParkingSlot)
                .ThenInclude(s => s.ParkingArea)
            .Where(h => h.Reservation.UserId == userId)
            .OrderByDescending(h => h.CheckInTime)
            .Select(h => new
            {
                h.Id,
                ReservationNumber = h.Reservation.ReservationNumber,
                ParkingAreaName = h.ParkingSlot.ParkingArea.Name,
                SlotNumber = h.ParkingSlot.SlotNumber,
                LicensePlate = h.Vehicle.LicensePlate,
                h.CheckInTime,
                h.CheckOutTime,
                h.OverstayDurationMinutes,
                h.OverstayFee,
                h.Notes
            })
            .ToListAsync();

        return Ok(history);
    }

    #endregion

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(claim, out var id))
        {
            return id;
        }

        throw new UnauthorizedAccessException("Customer ID not found in claims.");
    }
}

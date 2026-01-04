using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelReservation.API.Data;
using HotelReservation.API.Models;
using HotelReservation.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HotelReservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReservationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ReservationDTO>>> GetReservations()
        {
            try
            {
                var reservations = await _context.Reservations
                    .Include(r => r.Room)
                    .ThenInclude(room => room!.Hotel)
                    .Include(r => r.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReservationDTO
                    {
                        Id = r.Id,
                        GuestName = r.GuestName,
                        GuestEmail = r.GuestEmail,
                        GuestPhone = r.GuestPhone,
                        RoomId = r.RoomId,
                        RoomNumber = r.Room!.RoomNumber,
                        RoomType = r.Room.RoomType,
                        HotelName = r.Room.Hotel!.Name,
                        HotelId = r.Room.HotelId, // FIXED LINE
                        CheckInDate = r.CheckInDate,
                        CheckOutDate = r.CheckOutDate,
                        NumberOfGuests = r.NumberOfGuests,
                        SpecialRequests = r.SpecialRequests,
                        TotalPrice = r.TotalPrice,
                        Status = r.Status,
                        PaymentStatus = r.PaymentStatus,
                        CreatedAt = r.CreatedAt,
                        UserId = r.UserId,
                        UserName = r.User != null ? r.User.FullName : "Guest"
                    })
                    .ToListAsync();

                return Ok(reservations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get reservations", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ReservationDTO>> GetReservation(int id)
        {
            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.Room)
                    .ThenInclude(room => room!.Hotel)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reservation == null)
                    return NotFound(new { message = "Reservation not found" });

                // Check authorization
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var userRole = User.FindFirstValue(ClaimTypes.Role)!;

                if (userRole != "Admin" && reservation.UserId != userId)
                    return Forbid();

                var reservationDTO = new ReservationDTO
                {
                    Id = reservation.Id,
                    GuestName = reservation.GuestName,
                    GuestEmail = reservation.GuestEmail,
                    GuestPhone = reservation.GuestPhone,
                    RoomId = reservation.RoomId,
                    RoomNumber = reservation.Room!.RoomNumber,
                    RoomType = reservation.Room.RoomType,
                    HotelName = reservation.Room.Hotel!.Name,
                    HotelId = reservation.Room.HotelId,
                    CheckInDate = reservation.CheckInDate,
                    CheckOutDate = reservation.CheckOutDate,
                    NumberOfGuests = reservation.NumberOfGuests,
                    SpecialRequests = reservation.SpecialRequests,
                    TotalPrice = reservation.TotalPrice,
                    Status = reservation.Status,
                    PaymentStatus = reservation.PaymentStatus,
                    CreatedAt = reservation.CreatedAt,
                    UserId = reservation.UserId,
                    UserName = reservation.User != null ? reservation.User.FullName : "Guest"
                };

                return Ok(reservationDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get reservation", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Reservation>> CreateReservation(CreateReservationDTO reservationDTO)
        {
            try
            {
                // Validate dates
                if (reservationDTO.CheckInDate >= reservationDTO.CheckOutDate)
                    return BadRequest(new { message = "Check-in date must be before check-out date" });

                if (reservationDTO.CheckInDate < DateTime.Today)
                    return BadRequest(new { message = "Check-in date cannot be in the past" });

                // Get room
                var room = await _context.Rooms
                    .Include(r => r.Hotel)
                    .FirstOrDefaultAsync(r => r.Id == reservationDTO.RoomId);

                if (room == null)
                    return BadRequest(new { message = "Room not found" });

                if (!room.IsAvailable)
                    return BadRequest(new { message = "Room is not available" });

                // Check for date conflicts
                var conflictingReservation = await _context.Reservations
                    .AnyAsync(r => r.RoomId == reservationDTO.RoomId
                                && r.Status != "Cancelled"
                                && r.CheckInDate < reservationDTO.CheckOutDate
                                && r.CheckOutDate > reservationDTO.CheckInDate);

                if (conflictingReservation)
                    return BadRequest(new { message = "Room is already booked for these dates" });

                // Calculate total price
                var nights = (reservationDTO.CheckOutDate - reservationDTO.CheckInDate).Days;
                if (nights <= 0) nights = 1;

                var totalPrice = room.PricePerNight * nights;

                // Get user if logged in
                int? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                }
                else if (reservationDTO.UserId.HasValue)
                {
                    userId = reservationDTO.UserId.Value;
                }

                var reservation = new Reservation
                {
                    GuestName = reservationDTO.GuestName,
                    GuestEmail = reservationDTO.GuestEmail,
                    GuestPhone = reservationDTO.GuestPhone,
                    RoomId = reservationDTO.RoomId,
                    UserId = userId,
                    CheckInDate = reservationDTO.CheckInDate,
                    CheckOutDate = reservationDTO.CheckOutDate,
                    NumberOfGuests = reservationDTO.NumberOfGuests,
                    SpecialRequests = reservationDTO.SpecialRequests,
                    TotalPrice = totalPrice,
                    Status = "Confirmed",
                    PaymentStatus = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                // Return created reservation with details
                var createdReservation = await _context.Reservations
                    .Include(r => r.Room)
                    .ThenInclude(room => room!.Hotel)
                    .FirstOrDefaultAsync(r => r.Id == reservation.Id);

                return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, new ReservationDTO
                {
                    Id = reservation.Id,
                    GuestName = reservation.GuestName,
                    GuestEmail = reservation.GuestEmail,
                    GuestPhone = reservation.GuestPhone,
                    RoomId = reservation.RoomId,
                    RoomNumber = room.RoomNumber,
                    RoomType = room.RoomType,
                    HotelName = room.Hotel!.Name,
                    HotelId = room.HotelId,
                    CheckInDate = reservation.CheckInDate,
                    CheckOutDate = reservation.CheckOutDate,
                    NumberOfGuests = reservation.NumberOfGuests,
                    SpecialRequests = reservation.SpecialRequests,
                    TotalPrice = reservation.TotalPrice,
                    Status = reservation.Status,
                    PaymentStatus = reservation.PaymentStatus,
                    CreatedAt = reservation.CreatedAt,
                    UserId = reservation.UserId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create reservation", error = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateReservationStatus(int id, UpdateReservationStatusDTO statusDTO)
        {
            try
            {
                var reservation = await _context.Reservations.FindAsync(id);
                if (reservation == null)
                    return NotFound(new { message = "Reservation not found" });

                // Check authorization
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var userRole = User.FindFirstValue(ClaimTypes.Role)!;

                if (userRole != "Admin" && reservation.UserId != userId)
                    return Forbid();

                // Validate status transition
                var validStatuses = new[] { "Confirmed", "Cancelled", "CheckedIn", "CheckedOut", "Completed" };
                if (!validStatuses.Contains(statusDTO.Status))
                    return BadRequest(new { message = "Invalid status" });

                reservation.Status = statusDTO.Status;
                reservation.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Reservation status updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update reservation status", error = ex.Message });
            }
        }

        [HttpPut("{id}/payment")]
        [Authorize]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] string paymentStatus)
        {
            try
            {
                var reservation = await _context.Reservations.FindAsync(id);
                if (reservation == null)
                    return NotFound(new { message = "Reservation not found" });

                // Only admin can update payment status
                var userRole = User.FindFirstValue(ClaimTypes.Role)!;
                if (userRole != "Admin")
                    return Forbid();

                var validPaymentStatuses = new[] { "Pending", "Paid", "Refunded" };
                if (!validPaymentStatuses.Contains(paymentStatus))
                    return BadRequest(new { message = "Invalid payment status" });

                reservation.PaymentStatus = paymentStatus;
                reservation.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Payment status updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update payment status", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            try
            {
                var reservation = await _context.Reservations.FindAsync(id);
                if (reservation == null)
                    return NotFound(new { message = "Reservation not found" });

                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete reservation", error = ex.Message });
            }
        }

        [HttpGet("available-rooms")]
        public async Task<IActionResult> GetAvailableRooms(
            [FromQuery] DateTime? checkIn,
            [FromQuery] DateTime? checkOut,
            [FromQuery] int? guests,
            [FromQuery] string? roomType,
            [FromQuery] int? hotelId)
        {
            try
            {
                var checkInDate = checkIn ?? DateTime.Today.AddDays(1);
                var checkOutDate = checkOut ?? DateTime.Today.AddDays(2);

                if (checkInDate >= checkOutDate)
                    return BadRequest(new { message = "Check-in date must be before check-out date" });

                // Get all rooms
                var query = _context.Rooms
                    .Include(r => r.Hotel)
                    .Where(r => r.IsAvailable);

                // Apply filters
                if (hotelId.HasValue)
                    query = query.Where(r => r.HotelId == hotelId.Value);

                if (!string.IsNullOrEmpty(roomType))
                    query = query.Where(r => r.RoomType == roomType);

                if (guests.HasValue)
                    query = query.Where(r => r.Capacity >= guests.Value);

                var allRooms = await query.ToListAsync();

                // Get rooms with conflicting reservations
                var conflictingRoomIds = await _context.Reservations
                    .Where(r => r.Status != "Cancelled"
                             && r.CheckInDate < checkOutDate
                             && r.CheckOutDate > checkInDate)
                    .Select(r => r.RoomId)
                    .Distinct()
                    .ToListAsync();

                // Filter out rooms with conflicts
                var availableRooms = allRooms
                    .Where(r => !conflictingRoomIds.Contains(r.Id))
                    .Select(r => new RoomDTO
                    {
                        Id = r.Id,
                        RoomNumber = r.RoomNumber,
                        RoomType = r.RoomType,
                        Description = r.Description,
                        Capacity = r.Capacity,
                        PricePerNight = r.PricePerNight,
                        IsAvailable = r.IsAvailable,
                        HotelName = r.Hotel!.Name,
                        HotelId = r.HotelId
                    })
                    .ToList();

                return Ok(availableRooms);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get available rooms", error = ex.Message });
            }
        }
    }
}
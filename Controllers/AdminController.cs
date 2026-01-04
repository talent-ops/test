using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using HotelReservation.API.Data;
using HotelReservation.API.DTOs;
using HotelReservation.API.Models;

namespace HotelReservation.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard/stats")]
        public async Task<ActionResult<DashboardStatsDTO>> GetDashboardStats()
        {
            try
            {
                var today = DateTime.Today;
                var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var stats = new DashboardStatsDTO
                {
                    TotalHotels = await _context.Hotels.CountAsync(),
                    TotalRooms = await _context.Rooms.CountAsync(),
                    AvailableRooms = await _context.Rooms.CountAsync(r => r.IsAvailable),
                    TotalReservations = await _context.Reservations.CountAsync(),
                    ActiveReservations = await _context.Reservations
                        .CountAsync(r => r.Status == "Confirmed" || r.Status == "CheckedIn"),
                    TotalUsers = await _context.Users.CountAsync(),
                    TodayRevenue = await _context.Reservations
                        .Where(r => r.CreatedAt.Date == today && r.PaymentStatus == "Paid")
                        .SumAsync(r => r.TotalPrice),
                    MonthlyRevenue = await _context.Reservations
                        .Where(r => r.CreatedAt >= firstDayOfMonth && r.CreatedAt <= lastDayOfMonth && r.PaymentStatus == "Paid")
                        .SumAsync(r => r.TotalPrice),
                    TotalRevenue = await _context.Reservations
                        .Where(r => r.PaymentStatus == "Paid")
                        .SumAsync(r => r.TotalPrice)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get dashboard stats", error = ex.Message });
            }
        }

        [HttpGet("dashboard/recent-reservations")]
        public async Task<ActionResult<IEnumerable<RecentReservationDTO>>> GetRecentReservations()
        {
            try
            {
                var recentReservations = await _context.Reservations
                    .Include(r => r.Room)
                    .ThenInclude(room => room.Hotel)
                    .Include(r => r.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .Select(r => new RecentReservationDTO
                    {
                        Id = r.Id,
                        GuestName = r.GuestName,
                        RoomNumber = r.Room!.RoomNumber,
                        HotelName = r.Room.Hotel!.Name,
                        CheckInDate = r.CheckInDate,
                        CheckOutDate = r.CheckOutDate,
                        TotalPrice = r.TotalPrice,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

                return Ok(recentReservations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get recent reservations", error = ex.Message });
            }
        }

        [HttpGet("dashboard/revenue-chart")]
        public async Task<ActionResult<IEnumerable<RevenueChartDTO>>> GetRevenueChart()
        {
            try
            {
                var sixMonthsAgo = DateTime.Today.AddMonths(-6);

                var monthlyRevenue = await _context.Reservations
                    .Where(r => r.CreatedAt >= sixMonthsAgo && r.PaymentStatus == "Paid")
                    .GroupBy(r => new { Year = r.CreatedAt.Year, Month = r.CreatedAt.Month })
                    .Select(g => new RevenueChartDTO
                    {
                        Period = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                        Revenue = g.Sum(r => r.TotalPrice),
                        Bookings = g.Count()
                    })
                    .OrderBy(g => g.Period)
                    .ToListAsync();

                // Fill in missing months
                var allMonths = Enumerable.Range(0, 6)
                    .Select(i => sixMonthsAgo.AddMonths(i))
                    .Select(d => new RevenueChartDTO
                    {
                        Period = d.ToString("MMM yyyy"),
                        Revenue = 0,
                        Bookings = 0
                    });

                var result = allMonths
                    .GroupJoin(monthlyRevenue,
                        month => month.Period,
                        revenue => revenue.Period,
                        (month, revenues) => revenues.FirstOrDefault() ?? month)
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get revenue chart", error = ex.Message });
            }
        }

        [HttpGet("dashboard/occupancy")]
        public async Task<ActionResult<IEnumerable<OccupancyDTO>>> GetOccupancyRates()
        {
            try
            {
                var today = DateTime.Today;

                var occupancyData = await _context.Hotels
                    .Include(h => h.Rooms)
                    .Select(h => new OccupancyDTO
                    {
                        HotelName = h.Name,
                        TotalRooms = h.Rooms.Count,
                        OccupiedRooms = _context.Reservations
                            .Count(r => r.Room!.HotelId == h.Id
                                     && r.Status != "Cancelled"
                                     && r.CheckInDate <= today
                                     && r.CheckOutDate > today),
                        OccupancyRate = h.Rooms.Count > 0
                            ? (decimal)_context.Reservations
                                .Count(r => r.Room!.HotelId == h.Id
                                         && r.Status != "Cancelled"
                                         && r.CheckInDate <= today
                                         && r.CheckOutDate > today) / h.Rooms.Count * 100
                            : 0
                    })
                    .ToListAsync();

                return Ok(occupancyData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get occupancy rates", error = ex.Message });
            }
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Select(u => new UserDTO
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        Role = u.Role,
                        FullName = u.FullName,
                        Phone = u.Phone,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get users", error = ex.Message });
            }
        }

        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] bool isActive)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                user.IsActive = isActive;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "User status updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update user status", error = ex.Message });
            }
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] string role)
        {
            try
            {
                var validRoles = new[] { "User", "Admin" };
                if (!validRoles.Contains(role))
                    return BadRequest(new { message = "Invalid role" });

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                user.Role = role;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "User role updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update user role", error = ex.Message });
            }
        }

        [HttpGet("reservations/report")]
        public async Task<IActionResult> GetReservationsReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? status)
        {
            try
            {
                var query = _context.Reservations
                    .Include(r => r.Room)
                    .ThenInclude(room => room.Hotel)
                    .Include(r => r.User)
                    .AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(r => r.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(r => r.CreatedAt <= endDate.Value);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(r => r.Status == status);

                var reservations = await query
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
                        HotelId = r.Room.HotelId,
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

                var summary = new
                {
                    TotalReservations = reservations.Count,
                    TotalRevenue = reservations.Sum(r => r.TotalPrice),
                    AverageBookingValue = reservations.Any() ? reservations.Average(r => r.TotalPrice) : 0,
                    ByStatus = reservations.GroupBy(r => r.Status)
                        .Select(g => new { Status = g.Key, Count = g.Count(), Revenue = g.Sum(r => r.TotalPrice) }),
                    ByHotel = reservations.GroupBy(r => r.HotelName)
                        .Select(g => new { Hotel = g.Key, Count = g.Count(), Revenue = g.Sum(r => r.TotalPrice) })
                };

                return Ok(new { Reservations = reservations, Summary = summary });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to generate report", error = ex.Message });
            }
        }
    }
}
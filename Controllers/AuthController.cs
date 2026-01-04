using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelReservation.API.Services;
using HotelReservation.API.DTOs;
using HotelReservation.API.Data;  // ADD THIS LINE
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HotelReservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDTO registerDto)
        {
            try
            {
                var result = await _authService.Register(registerDto);
                if (result == null)
                    return BadRequest(new { message = "Username or email already exists" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Registration failed", error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO loginDto)
        {
            try
            {
                var result = await _authService.Login(loginDto);
                if (result == null)
                    return Unauthorized(new { message = "Invalid username or password" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Login failed", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var user = await _authService.GetUserById(userId);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                return Ok(new UserDTO
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role,
                    FullName = user.FullName,
                    Phone = user.Phone,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get profile", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO changePasswordDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var success = await _authService.ChangePassword(userId, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);

                if (!success)
                    return BadRequest(new { message = "Current password is incorrect" });

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to change password", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("my-reservations")]
        public async Task<IActionResult> GetMyReservations([FromServices] ApplicationDbContext context)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var reservations = await context.Reservations
                    .Where(r => r.UserId == userId)
                    .Include(r => r.Room)
                    .ThenInclude(room => room.Hotel)
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
                        UserName = r.User!.FullName
                    })
                    .ToListAsync();

                return Ok(reservations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get reservations", error = ex.Message });
            }
        }
    }
}
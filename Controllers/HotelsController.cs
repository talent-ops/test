using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using HotelReservation.API.Data;
using HotelReservation.API.Models;
using HotelReservation.API.DTOs;

namespace HotelReservation.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotelsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HotelsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Hotels
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HotelDTO>>> GetHotels()
        {
            try
            {
                var hotels = await _context.Hotels
                    .Include(h => h.Rooms)
                    .ToListAsync();

                var hotelDTOs = hotels.Select(h => new HotelDTO
                {
                    Id = h.Id,
                    Name = h.Name,
                    Address = h.Address,
                    City = h.City,
                    Country = h.Country,
                    Phone = h.Phone,
                    Email = h.Email,
                    Rating = h.Rating,
                    CreatedAt = h.CreatedAt,
                    Rooms = h.Rooms.Select(r => new RoomDTO
                    {
                        Id = r.Id,
                        RoomNumber = r.RoomNumber,
                        RoomType = r.RoomType,
                        Description = r.Description,
                        Capacity = r.Capacity,
                        PricePerNight = r.PricePerNight,
                        IsAvailable = r.IsAvailable,
                        HotelName = h.Name
                    }).ToList()
                }).ToList();

                return Ok(hotelDTOs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get hotels", error = ex.Message });
            }
        }

        // GET: api/Hotels/5
        [HttpGet("{id}")]
        public async Task<ActionResult<HotelDTO>> GetHotel(int id)
        {
            try
            {
                var hotel = await _context.Hotels
                    .Include(h => h.Rooms)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (hotel == null)
                {
                    return NotFound(new { message = $"Hotel with ID {id} not found" });
                }

                var hotelDTO = new HotelDTO
                {
                    Id = hotel.Id,
                    Name = hotel.Name,
                    Address = hotel.Address,
                    City = hotel.City,
                    Country = hotel.Country,
                    Phone = hotel.Phone,
                    Email = hotel.Email,
                    Rating = hotel.Rating,
                    CreatedAt = hotel.CreatedAt,
                    Rooms = hotel.Rooms.Select(r => new RoomDTO
                    {
                        Id = r.Id,
                        RoomNumber = r.RoomNumber,
                        RoomType = r.RoomType,
                        Description = r.Description,
                        Capacity = r.Capacity,
                        PricePerNight = r.PricePerNight,
                        IsAvailable = r.IsAvailable,
                        HotelName = hotel.Name
                    }).ToList()
                };

                return Ok(hotelDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get hotel", error = ex.Message });
            }
        }

        // POST: api/Hotels
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Hotel>> PostHotel(Hotel hotel)
        {
            try
            {
                if (string.IsNullOrEmpty(hotel.Name))
                    return BadRequest(new { message = "Hotel name is required" });

                hotel.CreatedAt = DateTime.UtcNow;

                if (hotel.Rooms == null)
                    hotel.Rooms = new List<Room>();
                else
                    foreach (var room in hotel.Rooms)
                        room.CreatedAt = DateTime.UtcNow;

                _context.Hotels.Add(hotel);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetHotel), new { id = hotel.Id }, hotel);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create hotel", error = ex.Message });
            }
        }

        // PUT: api/Hotels/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutHotel(int id, Hotel hotel)
        {
            if (id != hotel.Id)
                return BadRequest(new { message = "ID mismatch" });

            try
            {
                var existingHotel = await _context.Hotels
                    .Include(h => h.Rooms)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (existingHotel == null)
                    return NotFound(new { message = $"Hotel with ID {id} not found" });

                existingHotel.Name = hotel.Name;
                existingHotel.Address = hotel.Address;
                existingHotel.City = hotel.City;
                existingHotel.Country = hotel.Country;
                existingHotel.Phone = hotel.Phone;
                existingHotel.Email = hotel.Email;
                existingHotel.Rating = hotel.Rating;

                if (hotel.Rooms != null && hotel.Rooms.Any())
                {
                    _context.Rooms.RemoveRange(existingHotel.Rooms);

                    foreach (var room in hotel.Rooms)
                    {
                        room.HotelId = id;
                        room.CreatedAt = DateTime.UtcNow;
                        existingHotel.Rooms.Add(room);
                    }
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HotelExists(id))
                    return NotFound(new { message = $"Hotel with ID {id} not found" });
                else
                    throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update hotel", error = ex.Message });
            }
        }

        // DELETE: api/Hotels/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteHotel(int id)
        {
            try
            {
                var hotel = await _context.Hotels
                    .Include(h => h.Rooms)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (hotel == null)
                    return NotFound(new { message = $"Hotel with ID {id} not found" });

                _context.Hotels.Remove(hotel);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete hotel", error = ex.Message });
            }
        }

        // GET: api/Hotels/search?city=NewYork
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<HotelDTO>>> SearchHotels(
            [FromQuery] string? city = null,
            [FromQuery] string? country = null,
            [FromQuery] decimal? minRating = null)
        {
            try
            {
                var query = _context.Hotels
                    .Include(h => h.Rooms)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(city))
                    query = query.Where(h => h.City.Contains(city));

                if (!string.IsNullOrEmpty(country))
                    query = query.Where(h => h.Country.Contains(country));

                if (minRating.HasValue)
                    query = query.Where(h => h.Rating >= minRating.Value);

                var hotels = await query.ToListAsync();

                var hotelDTOs = hotels.Select(h => new HotelDTO
                {
                    Id = h.Id,
                    Name = h.Name,
                    Address = h.Address,
                    City = h.City,
                    Country = h.Country,
                    Phone = h.Phone,
                    Email = h.Email,
                    Rating = h.Rating,
                    CreatedAt = h.CreatedAt,
                    Rooms = h.Rooms.Select(r => new RoomDTO
                    {
                        Id = r.Id,
                        RoomNumber = r.RoomNumber,
                        RoomType = r.RoomType,
                        Description = r.Description,
                        Capacity = r.Capacity,
                        PricePerNight = r.PricePerNight,
                        IsAvailable = r.IsAvailable,
                        HotelName = h.Name
                    }).ToList()
                }).ToList();

                return Ok(hotelDTOs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to search hotels", error = ex.Message });
            }
        }

        // GET: api/Hotels/5/rooms
        [HttpGet("{id}/rooms")]
        public async Task<ActionResult<IEnumerable<RoomDTO>>> GetHotelRooms(int id)
        {
            try
            {
                var hotelExists = await _context.Hotels.AnyAsync(h => h.Id == id);
                if (!hotelExists)
                    return NotFound(new { message = $"Hotel with ID {id} not found" });

                var rooms = await _context.Rooms
                    .Where(r => r.HotelId == id)
                    .Include(r => r.Hotel)
                    .Select(r => new RoomDTO
                    {
                        Id = r.Id,
                        RoomNumber = r.RoomNumber,
                        RoomType = r.RoomType,
                        Description = r.Description,
                        Capacity = r.Capacity,
                        PricePerNight = r.PricePerNight,
                        IsAvailable = r.IsAvailable,
                        HotelName = r.Hotel.Name
                    })
                    .ToListAsync();

                return Ok(rooms);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get hotel rooms", error = ex.Message });
            }
        }

        private bool HotelExists(int id)
        {
            return _context.Hotels.Any(e => e.Id == id);
        }
    }
}
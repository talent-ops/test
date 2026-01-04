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
    public class RoomsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Rooms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomDTO>>> GetRooms()
        {
            try
            {
                var rooms = await _context.Rooms
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
                return StatusCode(500, new { message = "Failed to get rooms", error = ex.Message });
            }
        }

        // GET: api/Rooms/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomDTO>> GetRoom(int id)
        {
            try
            {
                var room = await _context.Rooms
                    .Include(r => r.Hotel)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (room == null)
                    return NotFound(new { message = "Room not found" });

                var roomDTO = new RoomDTO
                {
                    Id = room.Id,
                    RoomNumber = room.RoomNumber,
                    RoomType = room.RoomType,
                    Description = room.Description,
                    Capacity = room.Capacity,
                    PricePerNight = room.PricePerNight,
                    IsAvailable = room.IsAvailable,
                    HotelName = room.Hotel.Name
                };

                return Ok(roomDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get room", error = ex.Message });
            }
        }

        // GET: api/Rooms/available?hotelId=1
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<RoomDTO>>> GetAvailableRooms(
            [FromQuery] int? hotelId = null,
            [FromQuery] string? roomType = null,
            [FromQuery] int? minCapacity = null)
        {
            try
            {
                var query = _context.Rooms
                    .Include(r => r.Hotel)
                    .Where(r => r.IsAvailable);

                if (hotelId.HasValue)
                    query = query.Where(r => r.HotelId == hotelId.Value);

                if (!string.IsNullOrEmpty(roomType))
                    query = query.Where(r => r.RoomType == roomType);

                if (minCapacity.HasValue)
                    query = query.Where(r => r.Capacity >= minCapacity.Value);

                var rooms = await query
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
                return StatusCode(500, new { message = "Failed to get available rooms", error = ex.Message });
            }
        }

        // POST: api/Rooms
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Room>> PostRoom(Room room)
        {
            try
            {
                room.CreatedAt = DateTime.UtcNow;
                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create room", error = ex.Message });
            }
        }

        // PUT: api/Rooms/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutRoom(int id, Room room)
        {
            if (id != room.Id)
                return BadRequest(new { message = "ID mismatch" });

            try
            {
                _context.Entry(room).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoomExists(id))
                    return NotFound(new { message = "Room not found" });
                else
                    throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update room", error = ex.Message });
            }
        }

        // DELETE: api/Rooms/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            try
            {
                var room = await _context.Rooms.FindAsync(id);
                if (room == null)
                    return NotFound(new { message = "Room not found" });

                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete room", error = ex.Message });
            }
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.Id == id);
        }
    }
}
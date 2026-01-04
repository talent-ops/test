using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelReservation.API.Models
{
    public class Room
    {
        [Key]
        public int Id { get; set; }

        public int HotelId { get; set; }

        [Required, MaxLength(10)]
        public string RoomNumber { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string RoomType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public int Capacity { get; set; } = 1;

        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerNight { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Hotel? Hotel { get; set; }
    }
}
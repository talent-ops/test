using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelReservation.API.Models
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string GuestName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string GuestEmail { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string GuestPhone { get; set; } = string.Empty;

        public int RoomId { get; set; }
        public int? UserId { get; set; } // NULL if guest booking without account

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }

        public int NumberOfGuests { get; set; } = 1;

        [MaxLength(500)]
        public string SpecialRequests { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Confirmed"; // Confirmed, Cancelled, Completed, CheckedIn, CheckedOut

        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Refunded

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Room? Room { get; set; }
        public User? User { get; set; }
    }
}
namespace HotelReservation.API.DTOs
{
    public class ReservationDTO
    {
        public int Id { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public string GuestEmail { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public int HotelId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public string SpecialRequests { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    public class CreateReservationDTO
    {
        public string GuestName { get; set; } = string.Empty;
        public string GuestEmail { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
        public int RoomId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; } = 1;
        public string SpecialRequests { get; set; } = string.Empty;
        public int? UserId { get; set; }
    }

    public class UpdateReservationStatusDTO
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
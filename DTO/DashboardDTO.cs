namespace HotelReservation.API.DTOs
{
    public class DashboardStatsDTO
    {
        public int TotalHotels { get; set; }
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int TotalReservations { get; set; }
        public int ActiveReservations { get; set; }
        public int TotalUsers { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RecentReservationDTO
    {
        public int Id { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class RevenueChartDTO
    {
        public string Period { get; set; } = string.Empty; // "Jan", "Feb", etc.
        public decimal Revenue { get; set; }
        public int Bookings { get; set; }
    }

    public class OccupancyDTO
    {
        public string HotelName { get; set; } = string.Empty;
        public int TotalRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public decimal OccupancyRate { get; set; }
    }
}
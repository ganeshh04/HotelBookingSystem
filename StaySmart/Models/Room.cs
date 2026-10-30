using System.ComponentModel.DataAnnotations;
namespace StaySmart.Models;
public class Room
{
    public int Id { get; set; }
    [Required, StringLength(12)] public string RoomNumber { get; set; } = "";
    [Required, StringLength(60)] public string Name { get; set; } = "";
    [Required, StringLength(30)] public string Category { get; set; } = "Deluxe";
    [Range(1, 10)] public int Capacity { get; set; } = 2;
    [Range(1, 1000000)] public decimal PricePerNight { get; set; }
    [StringLength(500)] public string Description { get; set; } = "";
    [StringLength(200)] public string Amenities { get; set; } = "Wi-Fi, Air conditioning";
    [StringLength(500)] public string ImageUrl { get; set; } = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?auto=format&fit=crop&w=1200&q=80";
    public bool IsActive { get; set; } = true;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

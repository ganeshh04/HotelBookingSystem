using System.ComponentModel.DataAnnotations;
namespace StaySmart.Models;
public enum BookingStatus { Confirmed = 1, CheckedIn = 2, CheckedOut = 3, Cancelled = 4 }
public class Booking
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string GuestName { get; set; } = "";
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = "";
    [Required, Phone, StringLength(25)] public string Phone { get; set; } = "";
    public int RoomId { get; set; }
    public Room? Room { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public ICollection<BookingHistory> History { get; set; } = new List<BookingHistory>();
    [DataType(DataType.Date)] public DateTime CheckIn { get; set; }
    [DataType(DataType.Date)] public DateTime CheckOut { get; set; }
    [Range(1, 10)] public int Guests { get; set; } = 1;
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [StringLength(500)] public string? SpecialRequest { get; set; }
    public string BookingReference { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
}

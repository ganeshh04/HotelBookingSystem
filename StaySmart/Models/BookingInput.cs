using System.ComponentModel.DataAnnotations;
namespace StaySmart.Models;
public class BookingInput
{
    public int RoomId { get; set; }
    [Required, StringLength(100, MinimumLength = 2)] public string GuestName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, Phone] public string Phone { get; set; } = "";
    [DataType(DataType.Date)] public DateTime CheckIn { get; set; } = DateTime.Today.AddDays(1);
    [DataType(DataType.Date)] public DateTime CheckOut { get; set; } = DateTime.Today.AddDays(2);
    [Range(1, 10)] public int Guests { get; set; } = 1;
    [StringLength(500)] public string? SpecialRequest { get; set; }
}

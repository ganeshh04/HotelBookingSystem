using System.ComponentModel.DataAnnotations;
namespace StaySmart.Models;

/// <summary>One audit row per booking status change: who changed it, from what, to what, and when.</summary>
public class BookingHistory
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }
    [StringLength(20)] public string? OldStatus { get; set; }          // null = booking created
    [Required, StringLength(20)] public string NewStatus { get; set; } = "";
    [Required, StringLength(256)] public string ChangedBy { get; set; } = "";
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}

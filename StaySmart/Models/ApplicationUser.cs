using Microsoft.AspNetCore.Identity;
namespace StaySmart.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Guest = "Guest";
    public const string StaffOrAdmin = "Admin,Staff";
}

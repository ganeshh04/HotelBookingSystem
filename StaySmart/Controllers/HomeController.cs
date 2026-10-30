using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StaySmart.Data;
namespace StaySmart.Controllers;
public class HomeController(HotelDbContext db) : Controller
{
    public async Task<IActionResult> Index(DateTime? checkIn, DateTime? checkOut, int guests=2)
    {
        var start=(checkIn ?? DateTime.Today.AddDays(1)).Date; var end=(checkOut ?? start.AddDays(1)).Date;
        ViewBag.CheckIn=start.ToString("yyyy-MM-dd"); ViewBag.CheckOut=end.ToString("yyyy-MM-dd"); ViewBag.Guests=guests;
        var rooms=await db.Rooms.Where(r=>r.IsActive && r.Capacity>=guests).OrderBy(r=>r.PricePerNight).ToListAsync();
        if(end>start) { var unavailable=await db.Bookings.Where(b=>b.Status!=Models.BookingStatus.Cancelled && b.Status!=Models.BookingStatus.CheckedOut && b.CheckIn<end && b.CheckOut>start).Select(b=>b.RoomId).Distinct().ToListAsync(); rooms=rooms.Where(r=>!unavailable.Contains(r.Id)).ToList(); }
        return View(rooms);
    }
    public IActionResult Error() => View();
}

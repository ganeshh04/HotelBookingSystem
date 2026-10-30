using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StaySmart.Data;
using StaySmart.Models;
using StaySmart.Models.ViewModels;

namespace StaySmart.Controllers;

public class RoomsController(HotelDbContext db) : Controller
{
    public async Task<IActionResult> Details(int id)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
        if (room is null) return NotFound();

        // Upcoming reserved date ranges only (no guest details are exposed).
        var today = DateTime.Today;
        ViewBag.Booked = await db.Bookings
            .Where(b => b.RoomId == id && b.CheckOut > today
                        && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.CheckedOut)
            .OrderBy(b => b.CheckIn)
            .Select(b => new BookedRange(b.CheckIn, b.CheckOut))
            .ToListAsync();
        return View(room);
    }
}

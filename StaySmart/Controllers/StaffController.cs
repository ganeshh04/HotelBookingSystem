using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StaySmart.Data;
using StaySmart.Models;
using StaySmart.Services;

namespace StaySmart.Controllers;

// Authorization: the whole controller needs a signed-in Admin or Staff user.
[Authorize(Roles = Roles.StaffOrAdmin)]
public class StaffController(HotelDbContext db, IBookingService bookingService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        ViewBag.RoomCount = await db.Rooms.CountAsync(r => r.IsActive);
        ViewBag.BookingCount = await db.Bookings.CountAsync(b => b.Status != BookingStatus.Cancelled);
        ViewBag.Arrivals = await db.Bookings.CountAsync(b => b.CheckIn == today && b.Status == BookingStatus.Confirmed);
        ViewBag.Revenue = await db.Bookings.Where(b => b.Status != BookingStatus.Cancelled).SumAsync(b => (decimal?)b.TotalPrice) ?? 0m;
        var bookings = await db.Bookings.Include(b => b.Room).OrderByDescending(b => b.CreatedAtUtc).Take(12).ToListAsync();
        return View(bookings);
    }

    // ---- Bookings: search + filter ----
    public async Task<IActionResult> Bookings(string? q, BookingStatus? status, DateTime? from, DateTime? to)
    {
        var query = db.Bookings.Include(b => b.Room).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var text = q.Trim(); var reference = text.ToUpperInvariant();
            query = query.Where(b => b.GuestName.Contains(text) || b.Email.Contains(text) || b.BookingReference == reference);
        }
        if (status is not null) query = query.Where(b => b.Status == status);
        if (from is not null) query = query.Where(b => b.CheckIn >= from.Value.Date);
        if (to is not null) query = query.Where(b => b.CheckIn <= to.Value.Date);

        ViewBag.Q = q; ViewBag.Status = status;
        ViewBag.From = from?.ToString("yyyy-MM-dd"); ViewBag.To = to?.ToString("yyyy-MM-dd");
        return View(await query.OrderByDescending(b => b.CheckIn).Take(200).ToListAsync());
    }

    // ---- Audit trail for one booking ----
    public async Task<IActionResult> History(int id)
    {
        var booking = await db.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);
        if (booking is null) return NotFound();
        ViewBag.Booking = booking;
        return View(await db.BookingHistories.Where(h => h.BookingId == id).OrderBy(h => h.ChangedAtUtc).ThenBy(h => h.Id).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, BookingStatus status, string? returnTo)
    {
        var (ok, error) = await bookingService.ChangeStatusAsync(id, status, User.Identity?.Name ?? "unknown");
        TempData[ok ? "Message" : "Error"] = ok ? "Booking status updated." : error;
        // Only redirect to local URLs (prevents open-redirect attacks).
        if (!string.IsNullOrEmpty(returnTo) && Url.IsLocalUrl(returnTo)) return LocalRedirect(returnTo);
        return RedirectToAction(nameof(Index));
    }

    // ---- Rooms ----
    public async Task<IActionResult> Rooms() => View(await db.Rooms.OrderBy(r => r.RoomNumber).ToListAsync());

    // Inventory changes are Admin-only (stricter than the controller-level rule).
    [Authorize(Roles = Roles.Admin), HttpGet] public IActionResult CreateRoom() => View(new Room());

    [Authorize(Roles = Roles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoom(Room room)
    {
        if (await db.Rooms.AnyAsync(r => r.RoomNumber == room.RoomNumber))
            ModelState.AddModelError(nameof(room.RoomNumber), "Room number already exists.");
        if (!ModelState.IsValid) return View(room);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        TempData["Message"] = "Room added successfully.";
        return RedirectToAction(nameof(Rooms));
    }

    [Authorize(Roles = Roles.Admin), HttpGet]
    public async Task<IActionResult> EditRoom(int id)
    {
        var room = await db.Rooms.FindAsync(id);
        return room is null ? NotFound() : View(room);
    }

    [Authorize(Roles = Roles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoom(Room input)
    {
        var room = await db.Rooms.FindAsync(input.Id);
        if (room is null) return NotFound();
        if (await db.Rooms.AnyAsync(r => r.RoomNumber == input.RoomNumber && r.Id != input.Id))
            ModelState.AddModelError(nameof(input.RoomNumber), "Room number already exists.");
        if (!ModelState.IsValid) return View(input);

        // Copy only the editable fields (never bind the whole entity: avoids over-posting).
        room.RoomNumber = input.RoomNumber; room.Name = input.Name; room.Category = input.Category;
        room.Capacity = input.Capacity; room.PricePerNight = input.PricePerNight;
        room.Description = input.Description; room.Amenities = input.Amenities;
        room.ImageUrl = input.ImageUrl; room.IsActive = input.IsActive;
        await db.SaveChangesAsync();
        TempData["Message"] = "Room updated.";
        return RedirectToAction(nameof(Rooms));
    }

    [Authorize(Roles = Roles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRoom(int id)
    {
        var room = await db.Rooms.FindAsync(id);
        if (room is null) return NotFound();
        room.IsActive = !room.IsActive;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Rooms));
    }
}

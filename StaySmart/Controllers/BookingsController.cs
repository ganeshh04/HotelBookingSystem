using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StaySmart.Data;
using StaySmart.Models;
using StaySmart.Services;

namespace StaySmart.Controllers;

public class BookingsController(HotelDbContext db, IBookingService bookingService, UserManager<ApplicationUser> users) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [Authorize, HttpGet]
    public async Task<IActionResult> Create(int roomId, DateTime? checkIn, DateTime? checkOut, int guests = 1)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == roomId && r.IsActive);
        if (room is null) return NotFound();
        var me = await users.GetUserAsync(User);
        var start = (checkIn ?? DateTime.Today.AddDays(1)).Date;
        ViewBag.Room = room;
        return View(new BookingInput
        {
            RoomId = roomId, GuestName = me?.FullName ?? "", Email = me?.Email ?? "",
            CheckIn = start, CheckOut = (checkOut ?? start.AddDays(1)).Date, Guests = Math.Clamp(guests, 1, room.Capacity)
        });
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingInput input)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == input.RoomId && r.IsActive);
        if (room is null) return NotFound();
        ViewBag.Room = room;
        if (!ModelState.IsValid) return View(input);
        var result = await bookingService.CreateAsync(input, UserId);
        if (result.Error is not null) { ModelState.AddModelError(string.Empty, result.Error); return View(input); }
        return RedirectToAction(nameof(Confirmation), new { id = result.Booking!.Id, reference = result.Booking.BookingReference });
    }

    // Reference is an unguessable token, so a guest who just booked can view the confirmation.
    public async Task<IActionResult> Confirmation(int id, string reference)
    {
        var booking = await db.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id && b.BookingReference == reference);
        return booking is null ? NotFound() : View(booking);
    }

    [Authorize]
    public async Task<IActionResult> My()
    {
        var mine = await db.Bookings.Include(b => b.Room).Where(b => b.UserId == UserId)
            .OrderByDescending(b => b.CheckIn).ToListAsync();
        return View(mine);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var isStaff = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Staff);
        var (ok, error) = await bookingService.CancelAsync(id, UserId, isStaff, User.Identity?.Name ?? UserId);
        TempData[ok ? "Message" : "Error"] = ok ? "Your booking was cancelled." : error;
        return RedirectToAction(nameof(My));
    }

    [HttpGet] public IActionResult Find() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Find(string? reference, string? email)
    {
        reference = (reference ?? "").Trim().ToUpperInvariant(); email = (email ?? "").Trim();
        var booking = await db.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.BookingReference == reference && b.Email == email);
        if (booking is null) { ViewBag.Error = "No booking matched that reference and email."; return View(); }
        return View("Confirmation", booking);
    }
}

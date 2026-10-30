using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StaySmart.Models;
using StaySmart.Models.ViewModels;

namespace StaySmart.Controllers;

// Only an Admin can see or change who is Staff.
[Authorize(Roles = Roles.Admin)]
public class AdminController(UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Users()
    {
        var rows = new List<UserRow>();
        foreach (var u in await users.Users.OrderBy(u => u.Email).ToListAsync())
            rows.Add(new UserRow(u.Id, u.FullName, u.Email ?? "", string.Join(", ", await users.GetRolesAsync(u))));
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role)
    {
        // Only Guest <-> Staff can be assigned from the UI. Nobody can be promoted to Admin here.
        if (role != Roles.Staff && role != Roles.Guest) return BadRequest();
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (user.Id == users.GetUserId(User)) { TempData["Error"] = "You cannot change your own role."; return RedirectToAction(nameof(Users)); }
        if (await users.IsInRoleAsync(user, Roles.Admin)) { TempData["Error"] = "Admin accounts cannot be changed here."; return RedirectToAction(nameof(Users)); }

        await users.RemoveFromRolesAsync(user, await users.GetRolesAsync(user));
        await users.AddToRoleAsync(user, role);
        await users.UpdateSecurityStampAsync(user); // forces re-validation of that user's cookie
        TempData["Message"] = $"{user.Email} is now {role}.";
        return RedirectToAction(nameof(Users));
    }
}

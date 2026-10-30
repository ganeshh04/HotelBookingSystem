using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StaySmart.Models;
using StaySmart.Models.ViewModels;

namespace StaySmart.Controllers;

public class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : Controller
{
    [HttpGet] public IActionResult Login(string? returnUrl = null) { ViewBag.ReturnUrl = returnUrl; return View(new LoginViewModel()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await users.FindByEmailAsync(model.Email.Trim());
        // Same generic error for "no such user" and "wrong password" (no account enumeration).
        var result = user is null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl); // blocks open-redirect
            if (await users.IsInRoleAsync(user!, Roles.Admin) || await users.IsInRoleAsync(user!, Roles.Staff))
                return RedirectToAction("Index", "Staff");
            return RedirectToAction("My", "Bookings");
        }
        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Too many failed attempts. Try again in a few minutes."
            : "Invalid email or password.");
        return View(model);
    }

    [HttpGet] public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new ApplicationUser { UserName = model.Email.Trim(), Email = model.Email.Trim(), FullName = model.FullName.Trim() };
        var result = await users.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }
        // Public sign-up always creates a Guest. Staff/Admin accounts cannot be self-registered.
        await users.AddToRoleAsync(user, Roles.Guest);
        await signIn.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return RedirectToAction("Index", "Home"); }

    public IActionResult AccessDenied() => View();
}

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaySmart.Controllers;
using StaySmart.Models;
using Xunit;

namespace StaySmart.Tests;

/// <summary>Role checks: verifies the [Authorize] rules cannot be removed by accident.</summary>
public class AuthorizationTests
{
    private static AuthorizeAttribute? OnClass(Type t) => t.GetCustomAttributes<AuthorizeAttribute>(true).FirstOrDefault();
    private static List<AuthorizeAttribute> OnMethod(Type t, string name) =>
        t.GetMethods().Where(m => m.Name == name).SelectMany(m => m.GetCustomAttributes<AuthorizeAttribute>(true)).ToList();

    [Fact]
    public void StaffController_RequiresStaffOrAdmin() =>
        Assert.Equal(Roles.StaffOrAdmin, OnClass(typeof(StaffController))?.Roles);

    [Fact]
    public void AdminController_RequiresAdmin() =>
        Assert.Equal(Roles.Admin, OnClass(typeof(AdminController))?.Roles);

    [Theory]
    [InlineData("CreateRoom")]
    [InlineData("EditRoom")]
    [InlineData("ToggleRoom")]
    public void RoomManagement_IsAdminOnly(string action)
    {
        var rules = OnMethod(typeof(StaffController), action);
        Assert.NotEmpty(rules);
        Assert.All(rules, r => Assert.Equal(Roles.Admin, r.Roles));
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("My")]
    [InlineData("Cancel")]
    public void GuestBookingActions_RequireLogin(string action) =>
        Assert.NotEmpty(OnMethod(typeof(BookingsController), action));

    [Fact]
    public void EveryPostAction_ValidatesAntiForgeryToken()
    {
        var controllers = typeof(StaffController).Assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract);
        foreach (var c in controllers)
            foreach (var m in c.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                if (m.GetCustomAttribute<HttpPostAttribute>() != null)
                    Assert.True(m.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() != null, $"{c.Name}.{m.Name} is missing [ValidateAntiForgeryToken]");
    }
}

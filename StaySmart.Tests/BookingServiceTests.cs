using StaySmart.Models;
using StaySmart.Services;
using Xunit;

namespace StaySmart.Tests;

public class BookingServiceTests
{
    [Fact]
    public async Task Create_ValidBooking_IsConfirmedAndLogged()
    {
        using var db = TestDb.Create(); var svc = new BookingService(db);
        var (booking, error) = await svc.CreateAsync(TestDb.Input(10, 2), "u1");
        Assert.Null(error);
        Assert.Equal(BookingStatus.Confirmed, booking!.Status);
        Assert.Single(db.BookingHistories);                     // creation is audited
    }

    [Fact]
    public async Task Create_UsesServerSidePrice_IncludingWeekendRule()
    {
        using var db = TestDb.Create(); var svc = new BookingService(db);
        var input = TestDb.Input(10, 3);
        var (booking, _) = await svc.CreateAsync(input, "u1");
        Assert.Equal(PriceCalculator.Calculate(1000, input.CheckIn, input.CheckOut).Total, booking!.TotalPrice);
    }

    [Fact]
    public async Task Create_OverlappingDates_IsRejected()
    {
        using var db = TestDb.Create(); var svc = new BookingService(db);
        await svc.CreateAsync(TestDb.Input(10, 3), "u1");
        var (booking, error) = await svc.CreateAsync(TestDb.Input(11, 3), "u2");   // starts inside the first stay
        Assert.Null(booking);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task Create_SameDayTurnover_IsAllowed()
    {
        using var db = TestDb.Create(); var svc = new BookingService(db);
        await svc.CreateAsync(TestDb.Input(10, 2), "u1");                           // checks out on day 12
        var (booking, error) = await svc.CreateAsync(TestDb.Input(12, 2), "u2");    // checks in on day 12
        Assert.Null(error); Assert.NotNull(booking);
    }

    [Fact]
    public async Task Create_AfterCancellation_RoomBecomesAvailableAgain()
    {
        using var db = TestDb.Create(); var svc = new BookingService(db);
        var (first, _) = await svc.CreateAsync(TestDb.Input(10, 2), "u1");
        await svc.CancelAsync(first!.Id, "u1", false, "u1");
        var (second, error) = await svc.CreateAsync(TestDb.Input(10, 2), "u2");
        Assert.Null(error); Assert.NotNull(second);
    }

    [Fact]
    public async Task Create_PastCheckIn_IsRejected()
    {
        using var db = TestDb.Create();
        var (booking, error) = await new BookingService(db).CreateAsync(TestDb.Input(-1, 2), "u1");
        Assert.Null(booking); Assert.NotNull(error);
    }

    [Fact]
    public async Task Create_MoreGuestsThanCapacity_IsRejected()
    {
        using var db = TestDb.Create();
        var (booking, error) = await new BookingService(db).CreateAsync(TestDb.Input(10, 2, guests: 3), "u1");
        Assert.Null(booking); Assert.Contains("up to 2", error);
    }

    // ---------- Cancellation rules ----------
    [Fact]
    public async Task Cancel_OwnBooking_Succeeds_AndIsLogged()
    {
        using var db = TestDb.Create(); var svc = new BookingService(db);
        var (b, _) = await svc.CreateAsync(TestDb.Input(10, 2), "u1");
        var (ok, error) = await svc.CancelAsync(b!.Id, "u1", false, "guest@test.com");
        Assert.True(ok, error);
        Assert.Equal(BookingStatus.Cancelled, db.Bookings.Single().Status);
        Assert.Equal(2, db.BookingHistories.Count());
    }

    [Fact]
    public async Task Cancel_SomeoneElsesBooking_IsRejected()
    {
        using var db = TestDb.Create(); var svc = new BookingService(db);
        var (b, _) = await svc.CreateAsync(TestDb.Input(10, 2), "u1");
        var (ok, error) = await svc.CancelAsync(b!.Id, "u2", false, "intruder");
        Assert.False(ok); Assert.Contains("own", error);
        Assert.Equal(BookingStatus.Confirmed, db.Bookings.Single().Status);
    }

    [Fact]
    public async Task Cancel_OnCheckInDay_GuestRejected_StaffAllowed()
    {
        using var db = TestDb.Create(); var svc = new BookingService(db);
        var (b, _) = await svc.CreateAsync(TestDb.Input(0, 1), "u1");              // check-in today
        var guest = await svc.CancelAsync(b!.Id, "u1", false, "guest");
        Assert.False(guest.Ok);
        var staff = await svc.CancelAsync(b.Id, "staff1", true, "staff@staysmart.com");
        Assert.True(staff.Ok);
    }

    // ---------- Status changes ----------
    [Fact]
    public async Task ChangeStatus_WritesAuditRow_WithWhoAndWhat()
    {
        using var db = TestDb.Create(); var svc = new BookingService(db);
        var (b, _) = await svc.CreateAsync(TestDb.Input(10, 2), "u1");
        await svc.ChangeStatusAsync(b!.Id, BookingStatus.CheckedIn, "staff@staysmart.com");
        var row = db.BookingHistories.OrderBy(h => h.Id).Last();
        Assert.Equal("Confirmed", row.OldStatus); Assert.Equal("CheckedIn", row.NewStatus); Assert.Equal("staff@staysmart.com", row.ChangedBy);
    }

    [Fact]
    public async Task ChangeStatus_FromCancelled_IsBlocked()
    {
        using var db = TestDb.Create(); var svc = new BookingService(db);
        var (b, _) = await svc.CreateAsync(TestDb.Input(10, 2), "u1");
        await svc.ChangeStatusAsync(b!.Id, BookingStatus.Cancelled, "staff");
        var (ok, _) = await svc.ChangeStatusAsync(b.Id, BookingStatus.Confirmed, "staff");
        Assert.False(ok);
    }
}

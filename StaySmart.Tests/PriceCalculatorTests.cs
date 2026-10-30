using StaySmart.Services;
using Xunit;

namespace StaySmart.Tests;

public class PriceCalculatorTests
{
    private static DateTime Next(DayOfWeek day) { var d = DateTime.Today.AddDays(1); while (d.DayOfWeek != day) d = d.AddDays(1); return d; }

    [Fact]
    public void WeekdayStay_NoSurcharge()
    {
        var monday = Next(DayOfWeek.Monday);
        var p = PriceCalculator.Calculate(1000, monday, monday.AddDays(2));        // Mon, Tue nights
        Assert.Equal(2, p.Nights); Assert.Equal(0, p.WeekendNights); Assert.Equal(2000m, p.Total);
    }

    [Fact]
    public void FridayToMonday_TwoWeekendNights()
    {
        var friday = Next(DayOfWeek.Friday);
        var p = PriceCalculator.Calculate(1000, friday, friday.AddDays(3));        // Fri(+20%), Sat(+20%), Sun
        Assert.Equal(3, p.Nights); Assert.Equal(2, p.WeekendNights); Assert.Equal(3400m, p.Total);
    }

    [Fact]
    public void SundayNight_IsNotAWeekendNight()
    {
        var sunday = Next(DayOfWeek.Sunday);
        Assert.Equal(1000m, PriceCalculator.Calculate(1000, sunday, sunday.AddDays(1)).Total);
    }

    [Fact]
    public void SaturdayNight_HasSurcharge()
    {
        var saturday = Next(DayOfWeek.Saturday);
        Assert.Equal(1200m, PriceCalculator.Calculate(1000, saturday, saturday.AddDays(1)).Total);
    }
}

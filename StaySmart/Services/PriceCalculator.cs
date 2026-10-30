namespace StaySmart.Services;

public record PriceBreakdown(int Nights, int WeekendNights, decimal Total);

/// <summary>
/// Business rule: nights that START on a Friday or Saturday cost 20% more.
/// (Sunday-to-Monday is a normal night.) Used by the server AND mirrored in wwwroot/js/site.js for the live preview.
/// </summary>
public static class PriceCalculator
{
    public const decimal WeekendSurcharge = 0.20m;

    public static bool IsWeekendNight(DateTime night) =>
        night.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;

    public static PriceBreakdown Calculate(decimal ratePerNight, DateTime checkIn, DateTime checkOut)
    {
        int nights = 0, weekend = 0; decimal total = 0m;
        for (var night = checkIn.Date; night < checkOut.Date; night = night.AddDays(1))
        {
            nights++;
            if (IsWeekendNight(night)) { weekend++; total += Math.Round(ratePerNight * (1 + WeekendSurcharge), 2); }
            else total += ratePerNight;
        }
        return new PriceBreakdown(nights, weekend, total);
    }
}

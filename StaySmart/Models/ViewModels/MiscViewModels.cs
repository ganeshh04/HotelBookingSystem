namespace StaySmart.Models.ViewModels;

public record UserRow(string Id, string FullName, string Email, string Roles);
public record BookedRange(DateTime CheckIn, DateTime CheckOut);

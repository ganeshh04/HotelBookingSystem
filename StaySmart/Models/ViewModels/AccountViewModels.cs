using System.ComponentModel.DataAnnotations;
namespace StaySmart.Models.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(100, MinimumLength = 2)] public string FullName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)] public string Password { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";
}

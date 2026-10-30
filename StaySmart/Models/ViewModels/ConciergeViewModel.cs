using System.ComponentModel.DataAnnotations;
using StaySmart.Services;
namespace StaySmart.Models.ViewModels;

public class ConciergeViewModel
{
    [Required(ErrorMessage = "Tell us a little about your stay."), StringLength(500, MinimumLength = 5)]
    public string Request { get; set; } = "";
    public ConciergeResult? Result { get; set; }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StaySmart.Models.ViewModels;
using StaySmart.Services;

namespace StaySmart.Controllers;

public class ConciergeController(IAiConcierge concierge) : Controller
{
    [HttpGet] public IActionResult Index() => View(new ConciergeViewModel());

    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("ai")]
    public async Task<IActionResult> Index(ConciergeViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        model.Result = await concierge.RecommendAsync(model.Request, ct);
        return View(model);
    }
}

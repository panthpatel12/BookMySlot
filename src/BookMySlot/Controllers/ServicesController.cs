using BookMySlot.Services;
using BookMySlot.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;

namespace BookMySlot.Controllers;

public class ServicesController : Controller
{
    private readonly IServiceCatalogService _catalogService;
    private readonly ITimeZoneService _timeZoneService;

    public ServicesController(IServiceCatalogService catalogService, ITimeZoneService timeZoneService)
    {
        _catalogService = catalogService;
        _timeZoneService = timeZoneService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var services = await _catalogService.GetActiveServicesAsync();
        return View(services);
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        // Redirect to HomeController.ServiceDetails for unified presentation
        return RedirectToAction("ServiceDetails", "Home", new { id });
    }
}

using BookMySlot.Services;
using BookMySlot.ViewModels.Api;
using Microsoft.AspNetCore.Mvc;

namespace BookMySlot.Controllers.Api;

[ApiController]
[Route("api/services")]
[Produces("application/json")]
public class ServicesApiController : ControllerBase
{
    private readonly IServiceCatalogService _catalogService;

    public ServicesApiController(IServiceCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>
    /// Returns active public services offered by the system.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceApiDto>>> GetServices()
    {
        var services = await _catalogService.GetActiveServicesAsync();
        var dtos = services.Select(s => new ServiceApiDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            DurationMinutes = s.DurationMinutes,
            Price = s.Price,
            Currency = "INR"
        });

        return Ok(dtos);
    }
}

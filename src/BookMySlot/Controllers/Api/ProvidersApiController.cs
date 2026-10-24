using System.Globalization;
using BookMySlot.Data;
using BookMySlot.Services;
using BookMySlot.ViewModels.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookMySlot.Controllers.Api;

[ApiController]
[Route("api/providers")]
[Produces("application/json")]
public class ProvidersApiController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITimeZoneService _timeZoneService;

    public ProvidersApiController(ApplicationDbContext dbContext, ITimeZoneService timeZoneService)
    {
        _dbContext = dbContext;
        _timeZoneService = timeZoneService;
    }

    /// <summary>
    /// Returns available appointment slots for a specific provider, service, and local date.
    /// Example: GET /api/providers/1/available-slots?serviceId=2&date=2026-10-25
    /// Date parameter is interpreted in the configured application time zone.
    /// </summary>
    [HttpGet("{id:int}/available-slots")]
    public async Task<ActionResult<AvailableSlotsApiResponse>> GetAvailableSlots(
        int id,
        [FromQuery] int? serviceId,
        [FromQuery] string? date)
    {
        var provider = await _dbContext.ProviderProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (provider == null)
        {
            return NotFound(new { error = $"Active provider with ID {id} was not found." });
        }

        // Parse date in configured application time zone
        DateTime localTargetDate;
        if (!string.IsNullOrWhiteSpace(date))
        {
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out localTargetDate))
            {
                return BadRequest(new { error = "Invalid date format. Expected YYYY-MM-DD." });
            }
        }
        else
        {
            localTargetDate = _timeZoneService.ToLocal(DateTime.UtcNow).Date;
        }

        // Determine UTC range corresponding to the local full day (00:00 to 23:59:59.999)
        var localStartOfDay = new DateTime(localTargetDate.Year, localTargetDate.Month, localTargetDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var localEndOfDay = localStartOfDay.AddDays(1).AddTicks(-1);

        var utcStart = _timeZoneService.ToUtc(localStartOfDay);
        var utcEnd = _timeZoneService.ToUtc(localEndOfDay);

        var nowUtc = DateTime.UtcNow;

        // Query available slots
        var query = _dbContext.AppointmentSlots
            .AsNoTracking()
            .Include(s => s.Service)
            .Include(s => s.ProviderProfile)
                .ThenInclude(p => p.User)
            .Where(s => s.ProviderProfileId == id &&
                        s.IsActive &&
                        s.Service.IsActive &&
                        s.StartUtc >= nowUtc && // Not in past
                        s.StartUtc >= utcStart &&
                        s.StartUtc <= utcEnd &&
                        !_dbContext.SlotReservations.Any(r => r.AppointmentSlotId == s.Id)); // No active reservation

        if (serviceId.HasValue && serviceId.Value > 0)
        {
            query = query.Where(s => s.ServiceId == serviceId.Value);
        }

        var slots = await query.OrderBy(s => s.StartUtc).ToListAsync();

        var dtoList = slots.Select(s => new AvailableSlotApiDto
        {
            SlotId = s.Id,
            ProviderId = s.ProviderProfileId,
            ProviderName = s.ProviderProfile.User.FullName,
            ServiceId = s.ServiceId,
            ServiceName = s.Service.Name,
            StartUtc = s.StartUtc,
            EndUtc = s.EndUtc,
            StartLocal = _timeZoneService.FormatDateTime(s.StartUtc),
            EndLocal = _timeZoneService.FormatDateTime(s.EndUtc),
            TimeZone = _timeZoneService.TimeZoneDisplayName
        }).ToList();

        return Ok(new AvailableSlotsApiResponse
        {
            Date = localTargetDate.ToString("yyyy-MM-dd"),
            InterpretedAsTimeZone = _timeZoneService.TimeZoneDisplayName,
            TotalAvailableSlots = dtoList.Count,
            Slots = dtoList
        });
    }
}

using System.Globalization;
using BookMySlot.Data;
using BookMySlot.Services;
using BookMySlot.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookMySlot.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IServiceCatalogService _catalogService;
    private readonly ITimeZoneService _timeZoneService;

    public HomeController(
        ApplicationDbContext dbContext,
        IServiceCatalogService catalogService,
        ITimeZoneService timeZoneService)
    {
        _dbContext = dbContext;
        _catalogService = catalogService;
        _timeZoneService = timeZoneService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? serviceId, int? providerId, string? date)
    {
        var services = await _catalogService.GetActiveServicesAsync();
        var providers = await _dbContext.ProviderProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.IsActive)
            .OrderBy(p => p.User.FullName)
            .ToListAsync();

        DateTime? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(date) &&
            DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            parsedDate = dt;
        }

        var nowUtc = DateTime.UtcNow;

        // Query available slots: active, not past, provider active, service active, no SlotReservation
        var slotsQuery = _dbContext.AppointmentSlots
            .AsNoTracking()
            .Include(s => s.Service)
            .Include(s => s.ProviderProfile)
                .ThenInclude(p => p.User)
            .Where(s => s.IsActive &&
                        s.Service.IsActive &&
                        s.ProviderProfile.IsActive &&
                        s.StartUtc > nowUtc &&
                        !_dbContext.SlotReservations.Any(r => r.AppointmentSlotId == s.Id));

        if (serviceId.HasValue && serviceId.Value > 0)
        {
            slotsQuery = slotsQuery.Where(s => s.ServiceId == serviceId.Value);
        }

        if (providerId.HasValue && providerId.Value > 0)
        {
            slotsQuery = slotsQuery.Where(s => s.ProviderProfileId == providerId.Value);
        }

        if (parsedDate.HasValue)
        {
            var localDayStart = new DateTime(parsedDate.Value.Year, parsedDate.Value.Month, parsedDate.Value.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var localDayEnd = localDayStart.AddDays(1).AddTicks(-1);
            var utcDayStart = _timeZoneService.ToUtc(localDayStart);
            var utcDayEnd = _timeZoneService.ToUtc(localDayEnd);

            slotsQuery = slotsQuery.Where(s => s.StartUtc >= utcDayStart && s.StartUtc <= utcDayEnd);
        }

        var availableSlotsRaw = await slotsQuery
            .OrderBy(s => s.StartUtc)
            .Take(30)
            .ToListAsync();

        var availableSlotItems = availableSlotsRaw.Select(s => new PublicAvailableSlotItem
        {
            SlotId = s.Id,
            ServiceId = s.ServiceId,
            ServiceName = s.Service.Name,
            Price = s.Service.Price,
            DurationMinutes = s.Service.DurationMinutes,
            ProviderId = s.ProviderProfileId,
            ProviderName = s.ProviderProfile.User.FullName,
            StartUtc = s.StartUtc,
            EndUtc = s.EndUtc,
            FormattedSchedule = _timeZoneService.FormatSlotRange(s.StartUtc, s.EndUtc),
            FormattedDate = _timeZoneService.FormatDate(s.StartUtc),
            FormattedTime = $"{_timeZoneService.FormatTime(s.StartUtc)} - {_timeZoneService.FormatTime(s.EndUtc)}"
        }).ToList();

        var totalBookings = await _dbContext.Appointments.CountAsync(a => a.Status != Models.AppointmentStatus.Cancelled);

        var viewModel = new HomeIndexViewModel
        {
            SelectedServiceId = serviceId,
            SelectedProviderId = providerId,
            SelectedDate = parsedDate,
            FeaturedServices = services,
            AvailableSlots = availableSlotItems,
            TotalServicesCount = services.Count,
            TotalProvidersCount = providers.Count,
            TotalBookingsCount = totalBookings,
            TimeZoneDisplayName = _timeZoneService.TimeZoneDisplayName,
            ServicesList = services.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Name} ({s.DurationMinutes} mins - ₹{s.Price:N0})",
                Selected = s.Id == serviceId
            }).ToList(),
            ProvidersList = providers.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.User.FullName,
                Selected = p.Id == providerId
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ServiceDetails(int id)
    {
        var service = await _catalogService.GetServiceByIdAsync(id);
        if (service == null || !service.IsActive)
        {
            TempData["ErrorMessage"] = "Service not found or is currently inactive.";
            return RedirectToAction(nameof(Index));
        }

        var nowUtc = DateTime.UtcNow;

        var availableSlotsRaw = await _dbContext.AppointmentSlots
            .AsNoTracking()
            .Include(s => s.ProviderProfile)
                .ThenInclude(p => p.User)
            .Where(s => s.ServiceId == id &&
                        s.IsActive &&
                        s.ProviderProfile.IsActive &&
                        s.StartUtc > nowUtc &&
                        !_dbContext.SlotReservations.Any(r => r.AppointmentSlotId == s.Id))
            .OrderBy(s => s.StartUtc)
            .Take(20)
            .ToListAsync();

        var slotItems = availableSlotsRaw.Select(s => new PublicAvailableSlotItem
        {
            SlotId = s.Id,
            ServiceId = service.Id,
            ServiceName = service.Name,
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            ProviderId = s.ProviderProfileId,
            ProviderName = s.ProviderProfile.User.FullName,
            StartUtc = s.StartUtc,
            EndUtc = s.EndUtc,
            FormattedSchedule = _timeZoneService.FormatSlotRange(s.StartUtc, s.EndUtc),
            FormattedDate = _timeZoneService.FormatDate(s.StartUtc),
            FormattedTime = $"{_timeZoneService.FormatTime(s.StartUtc)} - {_timeZoneService.FormatTime(s.EndUtc)}"
        }).ToList();

        var assignedProviders = service.ProviderServices
            .Where(ps => ps.ProviderProfile.IsActive)
            .Select(ps => new ProviderSummaryItem
            {
                ProviderId = ps.ProviderProfileId,
                Name = ps.ProviderProfile.User.FullName,
                Bio = ps.ProviderProfile.Bio
            }).ToList();

        var viewModel = new ServiceDetailViewModel
        {
            Service = service,
            AssignedProviders = assignedProviders,
            AvailableSlots = slotItems,
            TimeZoneDisplayName = _timeZoneService.TimeZoneDisplayName
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}

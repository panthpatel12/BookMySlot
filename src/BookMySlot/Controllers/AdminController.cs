using System.Text.Json;
using BookMySlot.Data;
using BookMySlot.Extensions;
using BookMySlot.Models;
using BookMySlot.Services;
using BookMySlot.ViewModels.Admin;
using BookMySlot.ViewModels.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookMySlot.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IServiceCatalogService _catalogService;
    private readonly ITimeZoneService _timeZoneService;
    private readonly ILogger<AdminController> _logger;

    private const string FilterSessionKey = "Admin_Appointments_Filter";

    public AdminController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IServiceCatalogService catalogService,
        ITimeZoneService timeZoneService,
        ILogger<AdminController> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _catalogService = catalogService;
        _timeZoneService = timeZoneService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var totalCustomers = (await _userManager.GetUsersInRoleAsync("Customer")).Count;
        var totalProviders = await _dbContext.ProviderProfiles.CountAsync();
        var totalServices = await _dbContext.Services.CountAsync();
        var activeServicesCount = await _dbContext.Services.CountAsync(s => s.IsActive);

        var appointments = await _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.Service)
            .Include(a => a.Customer)
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.ProviderProfile)
                    .ThenInclude(p => p.User)
            .ToListAsync();

        var totalAppointments = appointments.Count;
        var bookedCount = appointments.Count(a => a.Status == AppointmentStatus.Booked);
        var completedCount = appointments.Count(a => a.Status == AppointmentStatus.Completed);
        var cancelledCount = appointments.Count(a => a.Status == AppointmentStatus.Cancelled);
        var noShowCount = appointments.Count(a => a.Status == AppointmentStatus.NoShow);

        var totalRevenue = appointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .Sum(a => a.AppointmentSlot.Service.Price);

        var recentAppointments = appointments
            .OrderByDescending(a => a.BookedAtUtc)
            .Take(8)
            .Select(MapToListItem)
            .ToList();

        var viewModel = new AdminDashboardViewModel
        {
            TotalCustomers = totalCustomers,
            TotalProviders = totalProviders,
            TotalServices = totalServices,
            ActiveServicesCount = activeServicesCount,
            TotalAppointments = totalAppointments,
            BookedAppointmentsCount = bookedCount,
            CompletedAppointmentsCount = completedCount,
            CancelledAppointmentsCount = cancelledCount,
            NoShowAppointmentsCount = noShowCount,
            TotalRevenueCompleted = totalRevenue,
            RecentAppointments = recentAppointments,
            TimeZoneDisplayName = _timeZoneService.TimeZoneDisplayName
        };

        return View(viewModel);
    }

    // ==========================================
    // SERVICES MANAGEMENT (CRUD)
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Services()
    {
        var services = await _dbContext.Services
            .AsNoTracking()
            .Include(s => s.ProviderServices)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return View(services);
    }

    [HttpGet]
    public IActionResult CreateService()
    {
        return View(new ServiceFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateService(ServiceFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var service = new Service
        {
            Name = model.Name.Trim(),
            Description = model.Description.Trim(),
            DurationMinutes = model.DurationMinutes,
            Price = model.Price,
            IsActive = model.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Services.Add(service);
        await _dbContext.SaveChangesAsync();

        _catalogService.InvalidateCache();
        TempData["SuccessMessage"] = $"Service '{service.Name}' created successfully.";
        return RedirectToAction(nameof(Services));
    }

    [HttpGet]
    public async Task<IActionResult> EditService(int id)
    {
        var service = await _dbContext.Services.FindAsync(id);
        if (service == null)
        {
            TempData["ErrorMessage"] = "Service not found.";
            return RedirectToAction(nameof(Services));
        }

        var model = new ServiceFormViewModel
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            DurationMinutes = service.DurationMinutes,
            Price = service.Price,
            IsActive = service.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditService(ServiceFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var service = await _dbContext.Services.FindAsync(model.Id);
        if (service == null)
        {
            TempData["ErrorMessage"] = "Service not found.";
            return RedirectToAction(nameof(Services));
        }

        service.Name = model.Name.Trim();
        service.Description = model.Description.Trim();
        service.DurationMinutes = model.DurationMinutes;
        service.Price = model.Price;
        service.IsActive = model.IsActive;

        await _dbContext.SaveChangesAsync();
        _catalogService.InvalidateCache();

        TempData["SuccessMessage"] = $"Service '{service.Name}' updated successfully.";
        return RedirectToAction(nameof(Services));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleServiceStatus(int id)
    {
        var service = await _dbContext.Services.FindAsync(id);
        if (service == null)
        {
            TempData["ErrorMessage"] = "Service not found.";
            return RedirectToAction(nameof(Services));
        }

        service.IsActive = !service.IsActive;
        await _dbContext.SaveChangesAsync();
        _catalogService.InvalidateCache();

        TempData["SuccessMessage"] = $"Service '{service.Name}' is now {(service.IsActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Services));
    }

    // ==========================================
    // PROVIDERS MANAGEMENT
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Providers()
    {
        var providers = await _dbContext.ProviderProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.ProviderServices)
                .ThenInclude(ps => ps.Service)
            .OrderBy(p => p.User.FullName)
            .ToListAsync();

        return View(providers);
    }

    [HttpGet]
    public async Task<IActionResult> CreateProvider()
    {
        var allServices = await _dbContext.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
        ViewBag.Services = allServices;
        return View(new CreateProviderViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProvider(CreateProviderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Services = await _dbContext.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email address already exists.");
            ViewBag.Services = await _dbContext.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.InitialPassword);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }
            ViewBag.Services = await _dbContext.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Provider");

        var profile = new ProviderProfile
        {
            ApplicationUserId = user.Id,
            Bio = model.Bio?.Trim() ?? string.Empty,
            IsActive = true
        };

        _dbContext.ProviderProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        if (model.SelectedServiceIds != null && model.SelectedServiceIds.Any())
        {
            foreach (var sId in model.SelectedServiceIds)
            {
                _dbContext.ProviderServices.Add(new ProviderService
                {
                    ProviderProfileId = profile.Id,
                    ServiceId = sId
                });
            }
            await _dbContext.SaveChangesAsync();
        }

        _logger.LogInformation("Administrator created provider {Email}.", model.Email);
        TempData["SuccessMessage"] = $"Provider '{user.FullName}' created successfully with assigned services.";
        return RedirectToAction(nameof(Providers));
    }

    [HttpGet]
    public async Task<IActionResult> ManageProvider(int id)
    {
        var provider = await _dbContext.ProviderProfiles
            .Include(p => p.User)
            .Include(p => p.ProviderServices)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (provider == null)
        {
            TempData["ErrorMessage"] = "Provider not found.";
            return RedirectToAction(nameof(Providers));
        }

        var allServices = await _dbContext.Services.OrderBy(s => s.Name).ToListAsync();
        var assignedIds = provider.ProviderServices.Select(ps => ps.ServiceId).ToHashSet();

        var serviceItems = allServices.Select(s => new ServiceAssignmentItem
        {
            ServiceId = s.Id,
            ServiceName = s.Name,
            Price = s.Price,
            DurationMinutes = s.DurationMinutes,
            IsAssigned = assignedIds.Contains(s.Id)
        }).ToList();

        var viewModel = new ProviderAssignmentViewModel
        {
            ProviderProfileId = provider.Id,
            ProviderName = provider.User.FullName,
            Email = provider.User.Email ?? "N/A",
            Bio = provider.Bio,
            IsActive = provider.IsActive,
            Services = serviceItems,
            SelectedServiceIds = assignedIds.ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageProvider(ProviderAssignmentViewModel model)
    {
        var provider = await _dbContext.ProviderProfiles
            .Include(p => p.ProviderServices)
            .FirstOrDefaultAsync(p => p.Id == model.ProviderProfileId);

        if (provider == null)
        {
            TempData["ErrorMessage"] = "Provider not found.";
            return RedirectToAction(nameof(Providers));
        }

        provider.Bio = model.Bio?.Trim() ?? string.Empty;
        provider.IsActive = model.IsActive;

        // Update service assignments
        _dbContext.ProviderServices.RemoveRange(provider.ProviderServices);

        if (model.SelectedServiceIds != null)
        {
            foreach (var sId in model.SelectedServiceIds)
            {
                _dbContext.ProviderServices.Add(new ProviderService
                {
                    ProviderProfileId = provider.Id,
                    ServiceId = sId
                });
            }
        }

        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Provider details and service assignments updated successfully.";
        return RedirectToAction(nameof(Providers));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProviderStatus(int id)
    {
        var provider = await _dbContext.ProviderProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (provider == null)
        {
            TempData["ErrorMessage"] = "Provider not found.";
            return RedirectToAction(nameof(Providers));
        }

        provider.IsActive = !provider.IsActive;
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Provider '{provider.User.FullName}' is now {(provider.IsActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Providers));
    }

    // ==========================================
    // APPOINTMENTS MANAGEMENT (With Session filter)
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Appointments(
        AppointmentStatus? status,
        int? serviceId,
        int? providerId,
        string? searchTerm,
        DateTime? fromDate,
        DateTime? toDate,
        bool clear = false)
    {
        AppointmentFilterViewModel filter;

        if (clear)
        {
            HttpContext.Session.Remove(FilterSessionKey);
            filter = new AppointmentFilterViewModel();
        }
        else if (!status.HasValue && !serviceId.HasValue && !providerId.HasValue &&
                 string.IsNullOrEmpty(searchTerm) && !fromDate.HasValue && !toDate.HasValue)
        {
            // Try restoring from session
            var sessionFilterJson = HttpContext.Session.GetString(FilterSessionKey);
            if (!string.IsNullOrEmpty(sessionFilterJson))
            {
                filter = JsonSerializer.Deserialize<AppointmentFilterViewModel>(sessionFilterJson) ?? new AppointmentFilterViewModel();
            }
            else
            {
                filter = new AppointmentFilterViewModel();
            }
        }
        else
        {
            filter = new AppointmentFilterViewModel
            {
                Status = status,
                ServiceId = serviceId,
                ProviderId = providerId,
                SearchTerm = searchTerm,
                FromDate = fromDate,
                ToDate = toDate
            };
            HttpContext.Session.SetString(FilterSessionKey, JsonSerializer.Serialize(filter));
        }

        // Query database using LINQ extensions
        var query = _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.Service)
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.ProviderProfile)
                    .ThenInclude(p => p.User)
            .AsQueryable();

        query = query.FilterByStatus(filter.Status);
        query = query.FilterByService(filter.ServiceId);
        query = query.FilterByProvider(filter.ProviderId);
        query = query.FilterBySearchTerm(filter.SearchTerm);

        DateTime? fromUtc = filter.FromDate.HasValue ? _timeZoneService.ToUtc(filter.FromDate.Value) : null;
        DateTime? toUtc = filter.ToDate.HasValue ? _timeZoneService.ToUtc(filter.ToDate.Value.AddDays(1).AddTicks(-1)) : null;
        query = query.FilterByDateRange(fromUtc, toUtc);

        var appointments = await query
            .OrderByDescending(a => a.AppointmentSlot.StartUtc)
            .ToListAsync();

        var services = await _dbContext.Services.OrderBy(s => s.Name).ToListAsync();
        var providers = await _dbContext.ProviderProfiles.Include(p => p.User).OrderBy(p => p.User.FullName).ToListAsync();

        var viewModel = new AdminAppointmentListViewModel
        {
            Filter = filter,
            Appointments = appointments.Select(MapToListItem).ToList(),
            TimeZoneDisplayName = _timeZoneService.TimeZoneDisplayName,
            ServicesList = services.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name,
                Selected = s.Id == filter.ServiceId
            }).ToList(),
            ProvidersList = providers.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.User.FullName,
                Selected = p.Id == filter.ProviderId
            }).ToList()
        };

        return View(viewModel);
    }

    private AppointmentListItemViewModel MapToListItem(Appointment a)
    {
        var nowUtc = DateTime.UtcNow;
        var isFutureBooked = a.Status == AppointmentStatus.Booked && a.AppointmentSlot.StartUtc > nowUtc;

        return new AppointmentListItemViewModel
        {
            Id = a.Id,
            ServiceName = a.AppointmentSlot.Service.Name,
            ServicePrice = a.AppointmentSlot.Service.Price,
            DurationMinutes = a.AppointmentSlot.Service.DurationMinutes,
            ProviderName = a.AppointmentSlot.ProviderProfile.User.FullName,
            CustomerName = a.Customer.FullName,
            CustomerEmail = a.Customer.Email ?? "N/A",
            StartUtc = a.AppointmentSlot.StartUtc,
            EndUtc = a.AppointmentSlot.EndUtc,
            FormattedTime = _timeZoneService.FormatSlotRange(a.AppointmentSlot.StartUtc, a.AppointmentSlot.EndUtc),
            Status = a.Status,
            Notes = a.Notes,
            BookedAtUtc = a.BookedAtUtc,
            FormattedBookedAt = _timeZoneService.FormatDateTime(a.BookedAtUtc),
            CanCancel = isFutureBooked,
            CanReschedule = isFutureBooked,
            CanMarkStatus = false
        };
    }
}

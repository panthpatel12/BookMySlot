using System.Globalization;
using System.Security.Claims;
using BookMySlot.Data;
using BookMySlot.Models;
using BookMySlot.Services;
using BookMySlot.ViewModels.Common;
using BookMySlot.ViewModels.Provider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookMySlot.Controllers;

[Authorize(Roles = "Provider")]
public class ProviderController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IBookingService _bookingService;
    private readonly ITimeZoneService _timeZoneService;

    public ProviderController(
        ApplicationDbContext dbContext,
        IBookingService bookingService,
        ITimeZoneService timeZoneService)
    {
        _dbContext = dbContext;
        _bookingService = bookingService;
        _timeZoneService = timeZoneService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private async Task<ProviderProfile?> GetCurrentProviderProfileAsync()
    {
        var userId = CurrentUserId;
        return await _dbContext.ProviderProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var provider = await GetCurrentProviderProfileAsync();
        if (provider == null)
        {
            TempData["ErrorMessage"] = "Provider profile not found.";
            return RedirectToAction("Index", "Home");
        }

        var nowUtc = DateTime.UtcNow;
        var todayLocal = _timeZoneService.ToLocal(nowUtc).Date;
        var localStartOfDay = new DateTime(todayLocal.Year, todayLocal.Month, todayLocal.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var localEndOfDay = localStartOfDay.AddDays(1).AddTicks(-1);

        var utcStartOfDay = _timeZoneService.ToUtc(localStartOfDay);
        var utcEndOfDay = _timeZoneService.ToUtc(localEndOfDay);

        var appointments = await _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.Service)
            .Where(a => a.AppointmentSlot.ProviderProfileId == provider.Id)
            .ToListAsync();

        var todayAppointments = appointments
            .Where(a => a.AppointmentSlot.StartUtc >= utcStartOfDay && a.AppointmentSlot.StartUtc <= utcEndOfDay)
            .OrderBy(a => a.AppointmentSlot.StartUtc)
            .Select(a => MapToListItem(a, provider.User.FullName))
            .ToList();

        var upcomingAppointments = appointments
            .Where(a => a.Status == AppointmentStatus.Booked && a.AppointmentSlot.StartUtc > nowUtc)
            .OrderBy(a => a.AppointmentSlot.StartUtc)
            .Take(10)
            .Select(a => MapToListItem(a, provider.User.FullName))
            .ToList();

        var activeSlotsCount = await _dbContext.AppointmentSlots
            .CountAsync(s => s.ProviderProfileId == provider.Id && s.IsActive && s.StartUtc > nowUtc);

        var viewModel = new ProviderDashboardViewModel
        {
            ProviderName = provider.User.FullName,
            Bio = provider.Bio,
            TodayAppointmentsCount = todayAppointments.Count,
            UpcomingAppointmentsCount = appointments.Count(a => a.Status == AppointmentStatus.Booked && a.AppointmentSlot.StartUtc > nowUtc),
            CompletedAppointmentsCount = appointments.Count(a => a.Status == AppointmentStatus.Completed),
            ActiveSlotsCount = activeSlotsCount,
            TodayAppointments = todayAppointments,
            UpcomingAppointments = upcomingAppointments,
            TimeZoneDisplayName = _timeZoneService.TimeZoneDisplayName
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Schedule(string? date)
    {
        var provider = await GetCurrentProviderProfileAsync();
        if (provider == null)
        {
            return RedirectToAction(nameof(Dashboard));
        }

        DateTime localDate;
        if (!string.IsNullOrWhiteSpace(date) &&
            DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            localDate = dt.Date;
        }
        else
        {
            localDate = _timeZoneService.ToLocal(DateTime.UtcNow).Date;
        }

        var localStartOfDay = new DateTime(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var localEndOfDay = localStartOfDay.AddDays(1).AddTicks(-1);

        var utcStart = _timeZoneService.ToUtc(localStartOfDay);
        var utcEnd = _timeZoneService.ToUtc(localEndOfDay);

        var slots = await _dbContext.AppointmentSlots
            .AsNoTracking()
            .Include(s => s.Service)
            .Include(s => s.SlotReservation)
            .Include(s => s.Appointments)
                .ThenInclude(a => a.Customer)
            .Where(s => s.ProviderProfileId == provider.Id &&
                        s.IsActive &&
                        s.StartUtc >= utcStart &&
                        s.StartUtc <= utcEnd)
            .OrderBy(s => s.StartUtc)
            .ToListAsync();

        var slotDisplayItems = slots.Select(s =>
        {
            var activeAppt = s.Appointments.FirstOrDefault(a => a.Status == AppointmentStatus.Booked)
                ?? s.Appointments.OrderByDescending(a => a.BookedAtUtc).FirstOrDefault();

            var isBooked = s.SlotReservation != null || (activeAppt != null && activeAppt.Status == AppointmentStatus.Booked);
            var statusText = isBooked ? "Booked" : (activeAppt != null ? activeAppt.Status.ToString() : "Available");

            return new SlotDisplayItemViewModel
            {
                SlotId = s.Id,
                ServiceName = s.Service.Name,
                DurationMinutes = s.Service.DurationMinutes,
                Price = s.Service.Price,
                StartUtc = s.StartUtc,
                EndUtc = s.EndUtc,
                FormattedTime = $"{_timeZoneService.FormatTime(s.StartUtc)} - {_timeZoneService.FormatTime(s.EndUtc)}",
                IsBooked = isBooked,
                IsActive = s.IsActive,
                AppointmentId = activeAppt?.Id,
                CustomerName = activeAppt?.Customer?.FullName,
                CustomerEmail = activeAppt?.Customer?.Email,
                Status = statusText,
                CanRemoveSlot = !isBooked,
                CanMarkStatus = isBooked && activeAppt != null && activeAppt.Status == AppointmentStatus.Booked
            };
        }).ToList();

        var viewModel = new ProviderScheduleViewModel
        {
            SelectedDate = localDate,
            FormattedDate = localDate.ToString("dddd, dd MMMM yyyy"),
            TimeZoneDisplayName = _timeZoneService.TimeZoneDisplayName,
            Slots = slotDisplayItems
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> CreateSlot()
    {
        var provider = await GetCurrentProviderProfileAsync();
        if (provider == null)
        {
            TempData["ErrorMessage"] = "Provider profile not found.";
            return RedirectToAction(nameof(Dashboard));
        }

        var assignedServices = await _dbContext.ProviderServices
            .AsNoTracking()
            .Include(ps => ps.Service)
            .Where(ps => ps.ProviderProfileId == provider.Id && ps.Service.IsActive)
            .Select(ps => ps.Service)
            .ToListAsync();

        if (!assignedServices.Any())
        {
            TempData["ErrorMessage"] = "You do not have any active services assigned. Please contact the administrator.";
            return RedirectToAction(nameof(Dashboard));
        }

        var tomorrowLocal = _timeZoneService.ToLocal(DateTime.UtcNow).Date.AddDays(1).AddHours(10);

        var viewModel = new CreateSlotViewModel
        {
            LocalStartTime = tomorrowLocal,
            TimeZoneDisplayName = _timeZoneService.TimeZoneDisplayName,
            AssignedServices = assignedServices.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Name} ({s.DurationMinutes} mins - ₹{s.Price:N0})"
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSlot(CreateSlotViewModel model)
    {
        var provider = await GetCurrentProviderProfileAsync();
        if (provider == null)
        {
            TempData["ErrorMessage"] = "Provider profile not found.";
            return RedirectToAction(nameof(Dashboard));
        }

        // Convert provider's local selected start time to UTC
        var startUtc = _timeZoneService.ToUtc(model.LocalStartTime);

        var result = await _bookingService.CreateSlotAsync(provider.Id, model.ServiceId, startUtc);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Schedule), new { date = model.LocalStartTime.ToString("yyyy-MM-dd") });
        }

        TempData["ErrorMessage"] = result.Message;

        // Re-populate assigned services
        var assignedServices = await _dbContext.ProviderServices
            .AsNoTracking()
            .Include(ps => ps.Service)
            .Where(ps => ps.ProviderProfileId == provider.Id && ps.Service.IsActive)
            .Select(ps => ps.Service)
            .ToListAsync();

        model.AssignedServices = assignedServices.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.Name} ({s.DurationMinutes} mins - ₹{s.Price:N0})"
        }).ToList();
        model.TimeZoneDisplayName = _timeZoneService.TimeZoneDisplayName;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSlot(int slotId, string? returnDate)
    {
        var provider = await GetCurrentProviderProfileAsync();
        if (provider == null)
        {
            TempData["ErrorMessage"] = "Provider profile not found.";
            return RedirectToAction(nameof(Dashboard));
        }

        var result = await _bookingService.DeleteOrDeactivateSlotAsync(slotId, provider.Id);
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Schedule), new { date = returnDate });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkStatus(int appointmentId, AppointmentStatus status, string? returnDate)
    {
        var provider = await GetCurrentProviderProfileAsync();
        if (provider == null)
        {
            TempData["ErrorMessage"] = "Provider profile not found.";
            return RedirectToAction(nameof(Dashboard));
        }

        var result = await _bookingService.UpdateAppointmentStatusAsync(appointmentId, status, provider.Id);
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Schedule), new { date = returnDate });
    }

    private AppointmentListItemViewModel MapToListItem(Appointment a, string providerName)
    {
        return new AppointmentListItemViewModel
        {
            Id = a.Id,
            ServiceName = a.AppointmentSlot.Service.Name,
            ServicePrice = a.AppointmentSlot.Service.Price,
            DurationMinutes = a.AppointmentSlot.Service.DurationMinutes,
            ProviderName = providerName,
            CustomerName = a.Customer?.FullName ?? "Customer",
            CustomerEmail = a.Customer?.Email ?? "N/A",
            StartUtc = a.AppointmentSlot.StartUtc,
            EndUtc = a.AppointmentSlot.EndUtc,
            FormattedTime = _timeZoneService.FormatSlotRange(a.AppointmentSlot.StartUtc, a.AppointmentSlot.EndUtc),
            Status = a.Status,
            Notes = a.Notes,
            BookedAtUtc = a.BookedAtUtc,
            FormattedBookedAt = _timeZoneService.FormatDateTime(a.BookedAtUtc),
            CanCancel = false,
            CanReschedule = false,
            CanMarkStatus = a.Status == AppointmentStatus.Booked
        };
    }
}

using System.Security.Claims;
using BookMySlot.Data;
using BookMySlot.Extensions;
using BookMySlot.Models;
using BookMySlot.Services;
using BookMySlot.ViewModels.Common;
using BookMySlot.ViewModels.Customer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookMySlot.Controllers;

[Authorize(Roles = "Customer")]
public class CustomerController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IBookingService _bookingService;
    private readonly ITimeZoneService _timeZoneService;

    public CustomerController(
        ApplicationDbContext dbContext,
        IBookingService bookingService,
        ITimeZoneService timeZoneService)
    {
        _dbContext = dbContext;
        _bookingService = bookingService;
        _timeZoneService = timeZoneService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var customerId = CurrentUserId;
        var nowUtc = DateTime.UtcNow;

        var allCustomerAppointments = await _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.Service)
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.ProviderProfile)
                    .ThenInclude(p => p.User)
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.AppointmentSlot.StartUtc)
            .ToListAsync();

        var upcomingList = allCustomerAppointments
            .Where(a => a.Status == AppointmentStatus.Booked && a.AppointmentSlot.StartUtc > nowUtc)
            .OrderBy(a => a.AppointmentSlot.StartUtc)
            .Select(MapToListItem)
            .ToList();

        var recentHistoryList = allCustomerAppointments
            .Where(a => a.Status != AppointmentStatus.Booked || a.AppointmentSlot.StartUtc <= nowUtc)
            .Take(10)
            .Select(MapToListItem)
            .ToList();

        var viewModel = new CustomerDashboardViewModel
        {
            CustomerName = User.Identity?.Name ?? "Valued Customer",
            UpcomingCount = upcomingList.Count,
            CompletedCount = allCustomerAppointments.Count(a => a.Status == AppointmentStatus.Completed),
            CancelledCount = allCustomerAppointments.Count(a => a.Status == AppointmentStatus.Cancelled),
            TotalBookingsCount = allCustomerAppointments.Count,
            UpcomingAppointments = upcomingList,
            RecentHistory = recentHistoryList,
            TimeZoneDisplayName = _timeZoneService.TimeZoneDisplayName
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> MyAppointments(AppointmentStatus? status)
    {
        var customerId = CurrentUserId;

        var query = _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.Service)
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.ProviderProfile)
                    .ThenInclude(p => p.User)
            .Where(a => a.CustomerId == customerId);

        if (status.HasValue)
        {
            query = query.FilterByStatus(status.Value);
        }

        var appointments = await query
            .OrderByDescending(a => a.AppointmentSlot.StartUtc)
            .ToListAsync();

        ViewBag.SelectedStatus = status;
        ViewBag.TimeZoneDisplayName = _timeZoneService.TimeZoneDisplayName;

        var listItems = appointments.Select(MapToListItem).ToList();
        return View(listItems);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var customerId = CurrentUserId;

        var appointment = await _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.Service)
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.ProviderProfile)
                    .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == customerId);

        if (appointment == null)
        {
            TempData["ErrorMessage"] = "Appointment not found or you are not authorized to view it.";
            return RedirectToAction(nameof(Dashboard));
        }

        var nowUtc = DateTime.UtcNow;
        var isFutureBooked = appointment.Status == AppointmentStatus.Booked && appointment.AppointmentSlot.StartUtc > nowUtc;

        var viewModel = new AppointmentDetailsViewModel
        {
            AppointmentId = appointment.Id,
            Status = appointment.Status,
            ServiceName = appointment.AppointmentSlot.Service.Name,
            ServiceDescription = appointment.AppointmentSlot.Service.Description,
            DurationMinutes = appointment.AppointmentSlot.Service.DurationMinutes,
            Price = appointment.AppointmentSlot.Service.Price,
            ProviderName = appointment.AppointmentSlot.ProviderProfile.User.FullName,
            ProviderEmail = appointment.AppointmentSlot.ProviderProfile.User.Email ?? "N/A",
            CustomerName = appointment.Customer.FullName,
            CustomerEmail = appointment.Customer.Email ?? "N/A",
            StartUtc = appointment.AppointmentSlot.StartUtc,
            EndUtc = appointment.AppointmentSlot.EndUtc,
            FormattedSchedule = _timeZoneService.FormatSlotRange(appointment.AppointmentSlot.StartUtc, appointment.AppointmentSlot.EndUtc),
            TimeZoneDisplayName = _timeZoneService.TimeZoneDisplayName,
            Notes = appointment.Notes,
            BookedAtUtc = appointment.BookedAtUtc,
            FormattedBookedAt = _timeZoneService.FormatDateTime(appointment.BookedAtUtc),
            CancelledAtUtc = appointment.CancelledAtUtc,
            FormattedCancelledAt = appointment.CancelledAtUtc.HasValue ? _timeZoneService.FormatDateTime(appointment.CancelledAtUtc.Value) : null,
            CanCancel = isFutureBooked,
            CanReschedule = isFutureBooked
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Book(int slotId)
    {
        var slot = await _dbContext.AppointmentSlots
            .AsNoTracking()
            .Include(s => s.Service)
            .Include(s => s.ProviderProfile)
                .ThenInclude(p => p.User)
            .Include(s => s.SlotReservation)
            .FirstOrDefaultAsync(s => s.Id == slotId);

        if (slot == null || !slot.IsActive)
        {
            TempData["ErrorMessage"] = "The selected appointment slot is invalid or no longer active.";
            return RedirectToAction("Index", "Home");
        }

        if (slot.StartUtc <= DateTime.UtcNow)
        {
            TempData["ErrorMessage"] = "Cannot book a past slot.";
            return RedirectToAction("Index", "Home");
        }

        if (slot.SlotReservation != null)
        {
            TempData["ErrorMessage"] = "This slot is already reserved by another customer. Please choose a different slot.";
            return RedirectToAction("Index", "Home");
        }

        var viewModel = new BookAppointmentViewModel
        {
            SlotId = slot.Id,
            ServiceId = slot.ServiceId,
            ServiceName = slot.Service.Name,
            ServiceDescription = slot.Service.Description,
            DurationMinutes = slot.Service.DurationMinutes,
            Price = slot.Service.Price,
            ProviderId = slot.ProviderProfileId,
            ProviderName = slot.ProviderProfile.User.FullName,
            ProviderBio = slot.ProviderProfile.Bio,
            StartUtc = slot.StartUtc,
            EndUtc = slot.EndUtc,
            FormattedSchedule = _timeZoneService.FormatSlotRange(slot.StartUtc, slot.EndUtc),
            TimeZoneDisplayName = _timeZoneService.TimeZoneDisplayName
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookAppointmentViewModel model)
    {
        var customerId = CurrentUserId;

        var result = await _bookingService.BookAppointmentAsync(customerId, model.SlotId, model.Notes);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = result.Data!.Id });
        }

        TempData["ErrorMessage"] = result.Message;
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Cancel(int id)
    {
        var customerId = CurrentUserId;

        var appointment = await _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.Service)
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.ProviderProfile)
                    .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == customerId);

        if (appointment == null)
        {
            TempData["ErrorMessage"] = "Appointment not found.";
            return RedirectToAction(nameof(Dashboard));
        }

        if (appointment.Status != AppointmentStatus.Booked)
        {
            TempData["ErrorMessage"] = $"Cannot cancel an appointment that is already {appointment.Status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (appointment.AppointmentSlot.StartUtc <= DateTime.UtcNow)
        {
            TempData["ErrorMessage"] = "Cannot cancel past appointments.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var viewModel = MapToListItem(appointment);
        return View(viewModel);
    }

    [HttpPost]
    [ActionName("Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelConfirmed(int id)
    {
        var customerId = CurrentUserId;

        var result = await _bookingService.CancelAppointmentAsync(id, customerId, isPrivilegedUser: false);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public async Task<IActionResult> Reschedule(int id)
    {
        var customerId = CurrentUserId;

        var appointment = await _dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.Service)
            .Include(a => a.AppointmentSlot)
                .ThenInclude(s => s.ProviderProfile)
                    .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == customerId);

        if (appointment == null)
        {
            TempData["ErrorMessage"] = "Appointment not found.";
            return RedirectToAction(nameof(Dashboard));
        }

        if (appointment.Status != AppointmentStatus.Booked || appointment.AppointmentSlot.StartUtc <= DateTime.UtcNow)
        {
            TempData["ErrorMessage"] = "Only future booked appointments can be rescheduled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var nowUtc = DateTime.UtcNow;
        var serviceId = appointment.AppointmentSlot.ServiceId;

        // Find alternative available future slots for the same service
        var availableSlots = await _dbContext.AppointmentSlots
            .AsNoTracking()
            .Include(s => s.ProviderProfile)
                .ThenInclude(p => p.User)
            .Where(s => s.ServiceId == serviceId &&
                        s.Id != appointment.AppointmentSlotId &&
                        s.IsActive &&
                        s.ProviderProfile.IsActive &&
                        s.StartUtc > nowUtc &&
                        !_dbContext.SlotReservations.Any(r => r.AppointmentSlotId == s.Id))
            .OrderBy(s => s.StartUtc)
            .ToListAsync();

        var slotOptions = availableSlots.Select(s => new RescheduleSlotOption
        {
            SlotId = s.Id,
            ProviderName = s.ProviderProfile.User.FullName,
            StartUtc = s.StartUtc,
            EndUtc = s.EndUtc,
            FormattedSchedule = $"{_timeZoneService.FormatSlotRange(s.StartUtc, s.EndUtc)} (Provider: {s.ProviderProfile.User.FullName})"
        }).ToList();

        var viewModel = new RescheduleAppointmentViewModel
        {
            AppointmentId = appointment.Id,
            CurrentSlotId = appointment.AppointmentSlotId,
            ServiceName = appointment.AppointmentSlot.Service.Name,
            CurrentProviderName = appointment.AppointmentSlot.ProviderProfile.User.FullName,
            CurrentSchedule = _timeZoneService.FormatSlotRange(appointment.AppointmentSlot.StartUtc, appointment.AppointmentSlot.EndUtc),
            AvailableSlots = slotOptions,
            TimeZoneDisplayName = _timeZoneService.TimeZoneDisplayName
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(RescheduleAppointmentViewModel model)
    {
        var customerId = CurrentUserId;

        if (model.SelectedNewSlotId <= 0)
        {
            TempData["ErrorMessage"] = "Please select a valid new appointment slot.";
            return RedirectToAction(nameof(Reschedule), new { id = model.AppointmentId });
        }

        var result = await _bookingService.RescheduleAppointmentAsync(
            model.AppointmentId,
            model.SelectedNewSlotId,
            customerId,
            isPrivilegedUser: false);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = model.AppointmentId });
        }

        TempData["ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Reschedule), new { id = model.AppointmentId });
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
            CustomerName = a.Customer?.FullName ?? "Customer",
            CustomerEmail = a.Customer?.Email ?? "N/A",
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

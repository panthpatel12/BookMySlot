using BookMySlot.Models;
using Microsoft.EntityFrameworkCore;

namespace BookMySlot.Extensions;

public static class AppointmentQueryExtensions
{
    public static IQueryable<Appointment> FilterByStatus(this IQueryable<Appointment> query, AppointmentStatus? status)
    {
        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }
        return query;
    }

    public static IQueryable<Appointment> FilterByCustomer(this IQueryable<Appointment> query, string? customerId)
    {
        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(a => a.CustomerId == customerId);
        }
        return query;
    }

    public static IQueryable<Appointment> FilterByProvider(this IQueryable<Appointment> query, int? providerProfileId)
    {
        if (providerProfileId.HasValue && providerProfileId.Value > 0)
        {
            query = query.Where(a => a.AppointmentSlot.ProviderProfileId == providerProfileId.Value);
        }
        return query;
    }

    public static IQueryable<Appointment> FilterByService(this IQueryable<Appointment> query, int? serviceId)
    {
        if (serviceId.HasValue && serviceId.Value > 0)
        {
            query = query.Where(a => a.AppointmentSlot.ServiceId == serviceId.Value);
        }
        return query;
    }

    public static IQueryable<Appointment> FilterByDateRange(this IQueryable<Appointment> query, DateTime? fromUtc, DateTime? toUtc)
    {
        if (fromUtc.HasValue)
        {
            query = query.Where(a => a.AppointmentSlot.StartUtc >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            query = query.Where(a => a.AppointmentSlot.StartUtc <= toUtc.Value);
        }
        return query;
    }

    public static IQueryable<Appointment> FilterBySearchTerm(this IQueryable<Appointment> query, string? term)
    {
        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim().ToLower();
            query = query.Where(a =>
                a.Customer.FullName.ToLower().Contains(term) ||
                (a.Customer.Email != null && a.Customer.Email.ToLower().Contains(term)) ||
                a.AppointmentSlot.Service.Name.ToLower().Contains(term) ||
                a.AppointmentSlot.ProviderProfile.User.FullName.ToLower().Contains(term));
        }
        return query;
    }
}

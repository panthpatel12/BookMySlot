using Microsoft.Extensions.Configuration;

namespace BookMySlot.Services;

public class TimeZoneService : ITimeZoneService
{
    private readonly TimeZoneInfo _timeZoneInfo;
    public string TimeZoneDisplayName { get; }

    public TimeZoneService(IConfiguration configuration)
    {
        var configuredId = configuration["ApplicationSettings:TimeZoneId"] ?? "Asia/Kolkata";
        try
        {
            _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(configuredId);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Fallback for Windows if IANA name was used
                if (configuredId.Equals("Asia/Kolkata", StringComparison.OrdinalIgnoreCase))
                {
                    _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                }
                else
                {
                    _timeZoneInfo = TimeZoneInfo.Utc;
                }
            }
            catch
            {
                _timeZoneInfo = TimeZoneInfo.Utc;
            }
        }
        catch
        {
            _timeZoneInfo = TimeZoneInfo.Utc;
        }

        TimeZoneDisplayName = $"{_timeZoneInfo.DisplayName}";
    }

    public TimeZoneInfo ApplicationTimeZone => _timeZoneInfo;

    public DateTime ToLocal(DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _timeZoneInfo);
    }

    public DateTime? ToLocal(DateTime? utcDateTime)
    {
        if (!utcDateTime.HasValue) return null;
        return ToLocal(utcDateTime.Value);
    }

    public DateTime ToUtc(DateTime localDateTime)
    {
        if (localDateTime.Kind == DateTimeKind.Utc)
        {
            return localDateTime;
        }
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, _timeZoneInfo);
    }

    public string FormatDateTime(DateTime utcDateTime)
    {
        var local = ToLocal(utcDateTime);
        return local.ToString("dd MMM yyyy, hh:mm tt");
    }

    public string FormatDate(DateTime utcDateTime)
    {
        var local = ToLocal(utcDateTime);
        return local.ToString("dd MMM yyyy");
    }

    public string FormatTime(DateTime utcDateTime)
    {
        var local = ToLocal(utcDateTime);
        return local.ToString("hh:mm tt");
    }

    public string FormatSlotRange(DateTime startUtc, DateTime endUtc)
    {
        var localStart = ToLocal(startUtc);
        var localEnd = ToLocal(endUtc);
        return $"{localStart:dd MMM yyyy, hh:mm tt} - {localEnd:hh:mm tt}";
    }
}

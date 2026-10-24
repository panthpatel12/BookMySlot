namespace BookMySlot.Services;

public interface ITimeZoneService
{
    TimeZoneInfo ApplicationTimeZone { get; }
    string TimeZoneDisplayName { get; }
    DateTime ToLocal(DateTime utcDateTime);
    DateTime? ToLocal(DateTime? utcDateTime);
    DateTime ToUtc(DateTime localDateTime);
    string FormatDateTime(DateTime utcDateTime);
    string FormatDate(DateTime utcDateTime);
    string FormatTime(DateTime utcDateTime);
    string FormatSlotRange(DateTime startUtc, DateTime endUtc);
}

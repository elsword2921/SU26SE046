namespace BLL.Common;

/// <summary>
/// Centralizes the application's time-zone convention.
/// System timestamps are persisted as Vietnam local time (UTC+7).
/// </summary>
public static class VietnamTime
{
    private static readonly Lazy<TimeZoneInfo> ZoneHolder = new(ResolveZone);

    public static TimeZoneInfo Zone => ZoneHolder.Value;
    public static DateTime UtcNow => DateTime.UtcNow;
    public static DateTime Now => FromUtc(UtcNow);
    public static DateTime Today => Now.Date;

    public static DateTime FromUtc(DateTime utc)
    {
        var normalizedUtc = utc.Kind == DateTimeKind.Utc
            ? utc
            : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, Zone);
    }

    public static bool IsSameLocalDate(DateTime storedTime, DateTime localDate) =>
        (storedTime.Kind == DateTimeKind.Utc ? FromUtc(storedTime) : storedTime).Date == localDate.Date;

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        throw new TimeZoneNotFoundException("Vietnam time zone could not be resolved.");
    }
}

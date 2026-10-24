using Salesync.Application.Interfaces.Services;

namespace Salesync.Application.Services;

public sealed class BusinessClock(TimeProvider time, string timeZoneId) : IBusinessClock
{
    private readonly TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    public DateTime Today => TimeZoneInfo.ConvertTime(time.GetUtcNow(), _zone).Date;
    public DateTime UtcStart(DateTime businessDate)
    {
        var local = DateTime.SpecifyKind(businessDate.Date, DateTimeKind.Unspecified);
        // Cairo advances at midnight. A business day starts at its first valid instant.
        while (_zone.IsInvalidTime(local)) local = local.AddMinutes(1);
        if (_zone.IsAmbiguousTime(local))
            return new DateTimeOffset(local, _zone.GetAmbiguousTimeOffsets(local).Max()).UtcDateTime;
        return TimeZoneInfo.ConvertTimeToUtc(local, _zone);
    }
}

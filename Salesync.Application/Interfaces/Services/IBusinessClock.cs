namespace Salesync.Application.Interfaces.Services;

public interface IBusinessClock
{
    DateTime Today { get; }
    DateTime UtcStart(DateTime businessDate);
}

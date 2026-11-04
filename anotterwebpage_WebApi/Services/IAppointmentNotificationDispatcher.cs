namespace anotterwebpage_WebApi.Services;

public interface IAppointmentNotificationDispatcher
{
    Task DispatchPendingAsync(CancellationToken cancellationToken = default);
}

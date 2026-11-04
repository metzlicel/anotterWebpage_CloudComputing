using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Services;

public interface IAppointmentNotificationFactory
{
    Task<IReadOnlyCollection<AppointmentNotification>> CreateAsync(
        Appointment appointment,
        AppointmentNotificationType type,
        AppointmentDetails? previousDetails = null,
        CancellationToken cancellationToken = default);
}

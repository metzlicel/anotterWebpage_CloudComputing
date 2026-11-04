using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Services;

namespace anotterwebpage.Api.Tests.Fakes;

public class FakeAppointmentNotificationFactory : IAppointmentNotificationFactory
{
    public Task<IReadOnlyCollection<AppointmentNotification>> CreateAsync(
        Appointment appointment,
        AppointmentNotificationType type,
        AppointmentDetails? previousDetails = null,
        CancellationToken cancellationToken = default)
    {
        var eventKey = $"test:{type}:{Guid.NewGuid():N}";

        return Task.FromResult<IReadOnlyCollection<AppointmentNotification>>(new[]
        {
            new AppointmentNotification
            {
                Appointment = appointment,
                Type = type,
                EventKey = eventKey,
                RecipientEmail = "patient@example.com",
                Subject = "Test",
                Body = "Patient notification"
            },
            new AppointmentNotification
            {
                Appointment = appointment,
                Type = type,
                EventKey = eventKey,
                RecipientEmail = "doctor@example.com",
                Subject = "Test",
                Body = "Doctor notification"
            }
        });
    }
}

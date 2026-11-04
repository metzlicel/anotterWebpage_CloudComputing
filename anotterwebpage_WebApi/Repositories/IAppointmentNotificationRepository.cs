using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Repositories;

public interface IAppointmentNotificationRepository
{
    void AddRange(IEnumerable<AppointmentNotification> notifications);

    Task<List<AppointmentNotification>> GetPendingAsync(
        DateTime now,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

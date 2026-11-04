using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace anotterwebpage_WebApi.Repositories;

public class AppointmentNotificationRepository : IAppointmentNotificationRepository
{
    private readonly ApplicationDbContext _context;

    public AppointmentNotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public void AddRange(IEnumerable<AppointmentNotification> notifications)
    {
        _context.AppointmentNotifications.AddRange(notifications);
    }

    public Task<List<AppointmentNotification>> GetPendingAsync(
        DateTime now,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return _context.AppointmentNotifications
            .Where(notification => notification.SentAt == null)
            .Where(notification => notification.NextAttemptAt <= now)
            .OrderBy(notification => notification.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}

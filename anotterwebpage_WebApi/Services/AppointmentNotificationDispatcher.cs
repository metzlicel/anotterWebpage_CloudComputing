using anotterwebpage_WebApi.Repositories;
using Microsoft.Extensions.Options;

namespace anotterwebpage_WebApi.Services;

public class AppointmentNotificationDispatcher : BackgroundService,
    IAppointmentNotificationDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentNotificationDispatcher> _logger;
    private readonly AppointmentNotificationOutboxOptions _options;

    public AppointmentNotificationDispatcher(
        IServiceScopeFactory scopeFactory,
        IOptions<AppointmentNotificationOutboxOptions> options,
        ILogger<AppointmentNotificationDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task DispatchPendingAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider
            .GetRequiredService<IAppointmentNotificationRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var now = DateTime.UtcNow;
        var notifications = await repository.GetPendingAsync(
            now,
            Math.Max(1, _options.BatchSize),
            cancellationToken);

        foreach (var notification in notifications)
        {
            notification.AttemptCount++;

            try
            {
                await emailService.SendEmailAsync(
                    notification.RecipientEmail,
                    notification.Subject,
                    notification.Body);
                notification.SentAt = now;
                notification.LastError = null;
            }
            catch (Exception exception)
            {
                notification.LastError = exception.Message;
                notification.NextAttemptAt = now.AddSeconds(
                    Math.Max(1, _options.RetryDelaySeconds));
                _logger.LogError(
                    exception,
                    "Unable to deliver appointment notification {NotificationId}.",
                    notification.Id);
            }
        }

        if (notifications.Count > 0)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(
                    exception,
                    "Unable to process the appointment notification outbox.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(Math.Max(1, _options.PollingIntervalSeconds)),
                stoppingToken);
        }
    }
}

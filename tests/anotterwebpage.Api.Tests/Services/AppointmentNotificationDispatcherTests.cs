using anotterwebpage.Api.Tests.Fakes;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace anotterwebpage.Api.Tests.Services;

public class AppointmentNotificationDispatcherTests
    : IClassFixture<AnotterwebpageApiTests>
{
    private readonly AnotterwebpageApiTests _factory;

    public AppointmentNotificationDispatcherTests(AnotterwebpageApiTests factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DispatchPendingAsync_RetriesAfterTemporaryEmailFailure()
    {
        await _factory.ResetDatabaseAsync();
        var emailService = _factory.Services.GetRequiredService<IEmailService>();
        var fakeEmailService = Assert.IsType<FakeEmailService>(emailService);
        fakeEmailService.Reset();
        fakeEmailService.FailuresRemaining = 1;

        await _factory.ExecuteDbContextAsync(async context =>
        {
            context.AppointmentNotifications.Add(new AppointmentNotification
            {
                AppointmentId = 1,
                Type = AppointmentNotificationType.Creada,
                EventKey = "retry-event",
                RecipientEmail = "patient@example.com",
                Subject = "Test",
                Body = "Test"
            });
            await context.SaveChangesAsync();
        });

        var dispatcher = _factory.Services
            .GetRequiredService<IAppointmentNotificationDispatcher>();
        await dispatcher.DispatchPendingAsync();

        var firstAttempt = await GetNotificationAsync();
        Assert.Null(firstAttempt.SentAt);
        Assert.Equal(1, firstAttempt.AttemptCount);
        Assert.Equal("Temporary SMTP failure.", firstAttempt.LastError);

        await _factory.ExecuteDbContextAsync(async context =>
        {
            var notification = await context.AppointmentNotifications.SingleAsync();
            notification.NextAttemptAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        });

        await dispatcher.DispatchPendingAsync();

        var delivered = await GetNotificationAsync();
        Assert.NotNull(delivered.SentAt);
        Assert.Equal(2, delivered.AttemptCount);
        Assert.Single(fakeEmailService.SentEmails);
    }

    private async Task<AppointmentNotification> GetNotificationAsync()
    {
        AppointmentNotification? notification = null;
        await _factory.ExecuteDbContextAsync(async context =>
        {
            notification = await context.AppointmentNotifications.SingleAsync();
        });

        return notification!;
    }
}

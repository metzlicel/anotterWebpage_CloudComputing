namespace anotterwebpage_WebApi.Services;

public class AppointmentNotificationOutboxOptions
{
    public const string SectionName = "AppointmentNotificationOutbox";

    public int BatchSize { get; set; } = 20;
    public int PollingIntervalSeconds { get; set; } = 30;
    public int RetryDelaySeconds { get; set; } = 60;
}

namespace anotterwebpage_WebApi.Domain;

public class AppointmentNotification
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;

    public AppointmentNotificationType Type { get; set; }

    public string EventKey { get; set; } = "";
    public string RecipientEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}

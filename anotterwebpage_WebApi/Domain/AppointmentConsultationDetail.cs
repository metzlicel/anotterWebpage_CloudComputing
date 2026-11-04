namespace anotterwebpage_WebApi.Domain;

public class AppointmentConsultationDetail
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;

    public string Diagnostico { get; private set; } = "";
    public string Recomendaciones { get; private set; } = "";
    public string? Observaciones { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public static AppointmentConsultationDetail Create(
        int appointmentId,
        string diagnostico,
        string recomendaciones,
        string? observaciones)
    {
        var detail = new AppointmentConsultationDetail
        {
            AppointmentId = appointmentId
        };

        detail.Update(diagnostico, recomendaciones, observaciones);
        detail.CreatedAt = detail.UpdatedAt;

        return detail;
    }

    public void Update(
        string diagnostico,
        string recomendaciones,
        string? observaciones)
    {
        if (string.IsNullOrWhiteSpace(diagnostico))
        {
            throw new ArgumentException(
                "El diagnóstico es obligatorio.",
                nameof(diagnostico));
        }

        if (string.IsNullOrWhiteSpace(recomendaciones))
        {
            throw new ArgumentException(
                "Las recomendaciones son obligatorias.",
                nameof(recomendaciones));
        }

        Diagnostico = diagnostico.Trim();
        Recomendaciones = recomendaciones.Trim();
        Observaciones = string.IsNullOrWhiteSpace(observaciones)
            ? null
            : observaciones.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}

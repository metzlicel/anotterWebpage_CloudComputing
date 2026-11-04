namespace anotterwebpage_WebApi.Domain;

public class Appointment
{
    public int Id { get; set; }

    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public AppointmentStatus Status { get; set; } =
        AppointmentStatus.Confirmada;

    public DateTime? CancelledAt { get; set; }

    public string Motivo { get; set; } = "";
    public Modalidad Modalidad { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AppointmentNotification> Notifications { get; set; } =
        new List<AppointmentNotification>();

    public AppointmentConsultationDetail? ConsultationDetail { get; set; }

    public static Appointment Create(
        int patientId,
        int doctorId,
        DateTime start,
        DateTime end,
        string motivo,
        Modalidad modalidad)
    {
        Validate(patientId, doctorId, start, end, motivo, modalidad);

        return new Appointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            Start = start,
            End = end,
            Motivo = motivo.Trim(),
            Modalidad = modalidad,
            Status = AppointmentStatus.Confirmada
        };
    }

    public void UpdateDetails(
        DateTime start,
        DateTime end,
        string motivo,
        Modalidad modalidad)
    {
        Validate(PatientId, DoctorId, start, end, motivo, modalidad);

        Start = start;
        End = end;
        Motivo = motivo.Trim();
        Modalidad = modalidad;
    }

    public bool Cancel(DateTime cancelledAt)
    {
        if (cancelledAt.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException(
                "La fecha de cancelación debe estar en UTC.");
        }

        if (Status == AppointmentStatus.Cancelada)
        {
            return false;
        }

        Status = AppointmentStatus.Cancelada;
        CancelledAt = cancelledAt;
        return true;
    }

    public AppointmentDetails Snapshot() =>
        new(Start, End, Motivo, Modalidad);

    private static void Validate(
        int patientId,
        int doctorId,
        DateTime start,
        DateTime end,
        string motivo,
        Modalidad modalidad)
    {
        if (patientId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(patientId),
                "La cita debe tener un paciente válido.");
        }

        if (doctorId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(doctorId),
                "La cita debe tener una doctora válida.");
        }

        if (start.Kind != DateTimeKind.Utc || end.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Las fechas de la cita deben estar en UTC.");
        }

        if (end <= start)
        {
            throw new ArgumentException(
                "La fecha final debe ser posterior a la inicial.",
                nameof(end));
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException(
                "El motivo de la cita es obligatorio.",
                nameof(motivo));
        }

        if (!Enum.IsDefined(modalidad))
        {
            throw new ArgumentOutOfRangeException(
                nameof(modalidad),
                "La modalidad de la cita no es válida.");
        }
    }
}

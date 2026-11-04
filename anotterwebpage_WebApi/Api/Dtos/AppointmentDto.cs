using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Api.Dtos;

public class AppointmentDto
{
    public int Id { get; set; }

    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public string Motivo { get; set; } = "";

    public Modalidad Modalidad { get; set; }

    public AppointmentStatus Status { get; set; }
    public DateTime? CancelledAt { get; set; }

    public PatientDto Patient { get; set; } = null!;
    public DoctorDto Doctor { get; set; } = null!;
    public AppointmentConsultationDetailDto? ConsultationDetail { get; set; }
}

public class AppointmentConsultationDetailDto
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public string Diagnostico { get; set; } = "";
    public string Recomendaciones { get; set; } = "";
    public string? Observaciones { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpsertAppointmentConsultationDetailDto
{
    public string Diagnostico { get; set; } = "";
    public string Recomendaciones { get; set; } = "";
    public string? Observaciones { get; set; }
}

public class PatientDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Numero { get; set; } = "";
}

public class DoctorDto
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
}

public class BusyAppointmentDto
{
    public int Id { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsMine { get; set; }
}

public class CreateAppointmentDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public string Motivo { get; set; } = "";

    public Modalidad? Modalidad { get; set; } 
}

public class UpdateAppointmentDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public string Motivo { get; set; } = "";

    public Modalidad? Modalidad { get; set; } 
}

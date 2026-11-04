using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Api.Extensions;

public static class MappingExtensions
{
    public static AppointmentDto ToDto(
        this Appointment appointment)
    {
        return new AppointmentDto
        {
            Id = appointment.Id,
            Start = appointment.Start,
            End = appointment.End,
            Motivo = appointment.Motivo,
            Modalidad = appointment.Modalidad,
            Status = appointment.Status,
            CancelledAt = appointment.CancelledAt,
            Patient = new PatientDto
            {
                Id = appointment.Patient.Id,
                Nombre = appointment.Patient.Nombre,
                Apellido = appointment.Patient.Apellido,
                Numero = appointment.Patient.Numero
            },
            Doctor = new DoctorDto
            {
                Id = appointment.Doctor.Id,
                Email = appointment.Doctor.Email
            },
            ConsultationDetail = appointment.ConsultationDetail?.ToDto()
        };
    }

    public static AppointmentConsultationDetailDto ToDto(
        this AppointmentConsultationDetail detail)
    {
        return new AppointmentConsultationDetailDto
        {
            Id = detail.Id,
            AppointmentId = detail.AppointmentId,
            Diagnostico = detail.Diagnostico,
            Recomendaciones = detail.Recomendaciones,
            Observaciones = detail.Observaciones,
            CreatedAt = detail.CreatedAt,
            UpdatedAt = detail.UpdatedAt
        };
    }

    public static BusyAppointmentDto ToBusyDto(
        this Appointment appointment,
        string? userId)
    {
        return new BusyAppointmentDto
        {
            Id = appointment.Id,
            Start = appointment.Start,
            End = appointment.End,
            IsMine = appointment.Patient.UserId == userId
        };
    }

    public static CollabDto ToDto(
        this Collab collab)
    {
        return new CollabDto
        {
            Id = collab.Id,
            Nombre = collab.Nombre,
            Apellido = collab.Apellido,
            NombreOrg = collab.NombreOrg,
            Email = collab.Email,
            Numero = collab.Numero,
            Motivo = collab.Motivo
        };
    }
}

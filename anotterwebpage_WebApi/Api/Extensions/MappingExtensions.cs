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
            Nombre = appointment.Nombre,
            Apellido = appointment.Apellido,
            NombrePaciente = appointment.NombrePaciente,
            Email = appointment.Email,
            Numero = appointment.Numero,
            Motivo = appointment.Motivo,
            Modalidad = appointment.Modalidad
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
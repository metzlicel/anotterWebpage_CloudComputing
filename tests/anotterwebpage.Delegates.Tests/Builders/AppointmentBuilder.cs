using anotterwebpage_WebApi.Domain;

namespace anotterwebpage.Delegates.Tests.Builders;

public class AppointmentBuilder
{
    private readonly Appointment _appointment = new()
    {
        Id = 1,

        Start = new DateTime(
            2026, 10, 15, 17, 0, 0,
            DateTimeKind.Utc),

        End = new DateTime(
            2026, 10, 15, 18, 0, 0,
            DateTimeKind.Utc),

        Nombre = "Metzli",
        Apellido = "Lopez",
        NombrePaciente = null,
        Email = "metzli@example.com",
        Numero = "6641234567",
        Motivo = "Consulta",
        Modalidad = "Online",
        IsReserved = true,
        UserId = "user-123"
    };

    public AppointmentBuilder WithId(int id)
    {
        _appointment.Id = id;
        return this;
    }

    public AppointmentBuilder WithUserId(string? userId)
    {
        _appointment.UserId = userId;
        return this;
    }

    public AppointmentBuilder WithNombre(string nombre)
    {
        _appointment.Nombre = nombre;
        return this;
    }

    public AppointmentBuilder WithDates(
        DateTime start,
        DateTime end)
    {
        _appointment.Start = start;
        _appointment.End = end;
        return this;
    }

    public Appointment Build()
    {
        return _appointment;
    }
}
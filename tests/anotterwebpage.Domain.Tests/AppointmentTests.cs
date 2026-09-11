using anotterwebpage_WebApi.Domain;

namespace anotterwebpage.Domain.Tests;

public class AppointmentTests
{
    [Fact]
    public void Appointment_CanBeCreatedWithExpectedValues()
    {
        // Arrange
        var start = new DateTime(
            2026, 10, 20, 17, 0, 0,
            DateTimeKind.Utc);

        var end = new DateTime(
            2026, 10, 20, 18, 0, 0,
            DateTimeKind.Utc);

        // Act
        var appointment = new Appointment
        {
            Id = 1,
            Start = start,
            End = end,
            Nombre = "Metzli",
            Apellido = "Lopez",
            NombrePaciente = "Paciente",
            Email = "metzli@example.com",
            Numero = "6641234567",
            Motivo = "Consulta",
            Modalidad = "Online",
            IsReserved = true,
            UserId = "user-123",
            CreatedAt = new DateTime(
                2026, 10, 1, 12, 0, 0,
                DateTimeKind.Utc)
        };

        // Assert
        Assert.Equal(1, appointment.Id);
        Assert.Equal(start, appointment.Start);
        Assert.Equal(end, appointment.End);
        Assert.Equal("Metzli", appointment.Nombre);
        Assert.Equal("Lopez", appointment.Apellido);
        Assert.Equal("Paciente", appointment.NombrePaciente);
        Assert.Equal("metzli@example.com", appointment.Email);
        Assert.Equal("6641234567", appointment.Numero);
        Assert.Equal("Consulta", appointment.Motivo);
        Assert.Equal("Online", appointment.Modalidad);
        Assert.True(appointment.IsReserved);
        Assert.Equal("user-123", appointment.UserId);
    }
    
    // Default
    [Fact]
    public void Appointment_IsReserved_HasExpectedDefaultValue()
    {
        var appointment = new Appointment();

        Assert.True(appointment.IsReserved);
    }
    
    // Optional fields
    [Fact]
    public void Appointment_OptionalFields_CanBeNull()
    {
        // Act
        var appointment = new Appointment
        {
            Id = 1,
            Start = new DateTime(
                2026, 10, 20, 17, 0, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 20, 18, 0, 0,
                DateTimeKind.Utc),

            Nombre = "Metzli",
            Apellido = "Lopez",
            NombrePaciente = null,
            Email = "metzli@example.com",
            Numero = "6641234567",
            Motivo = null,
            Modalidad = "Online"
        };

        // Assert
        Assert.Null(appointment.NombrePaciente);
        Assert.Null(appointment.Motivo);

        // Regla del dominio que ya definimos
        Assert.True(appointment.IsReserved);
    }
}
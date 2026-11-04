using anotterwebpage_WebApi.Domain;

namespace anotterwebpage.Domain.Tests;

public class AppointmentTests
{
    [Fact]
    public void Create_WithValidValues_CreatesConfirmedAppointment()
    {
        var start = DateTime.UtcNow.AddDays(3);

        var appointment = Appointment.Create(
            patientId: 1,
            doctorId: 2,
            start,
            start.AddHours(1),
            "Consulta inicial",
            Modalidad.Online);

        Assert.Equal(1, appointment.PatientId);
        Assert.Equal(2, appointment.DoctorId);
        Assert.Equal(AppointmentStatus.Confirmada, appointment.Status);
        Assert.Null(appointment.CancelledAt);
        Assert.Equal("Consulta inicial", appointment.Motivo);
    }

    [Fact]
    public void Create_WithNonUtcDates_ThrowsArgumentException()
    {
        var start = DateTime.Now.AddDays(3);

        Assert.Throws<ArgumentException>(() => Appointment.Create(
            1,
            2,
            start,
            start.AddHours(1),
            "Consulta",
            Modalidad.Online));
    }

    [Fact]
    public void Create_WithEndBeforeStart_ThrowsArgumentException()
    {
        var start = DateTime.UtcNow.AddDays(3);

        Assert.Throws<ArgumentException>(() => Appointment.Create(
            1,
            2,
            start,
            start.AddMinutes(-1),
            "Consulta",
            Modalidad.Online));
    }

    [Fact]
    public void Create_WithoutPatient_ThrowsArgumentOutOfRangeException()
    {
        var start = DateTime.UtcNow.AddDays(3);

        Assert.Throws<ArgumentOutOfRangeException>(() => Appointment.Create(
            0,
            2,
            start,
            start.AddHours(1),
            "Consulta",
            Modalidad.Online));
    }

    [Fact]
    public void UpdateDetails_WithValidValues_UpdatesAppointment()
    {
        var start = DateTime.UtcNow.AddDays(3);
        var appointment = Appointment.Create(
            1,
            2,
            start,
            start.AddHours(1),
            "Consulta",
            Modalidad.Online);

        var updatedStart = start.AddDays(1);
        appointment.UpdateDetails(
            updatedStart,
            updatedStart.AddHours(2),
            "Seguimiento",
            Modalidad.Presencial);

        Assert.Equal(updatedStart, appointment.Start);
        Assert.Equal("Seguimiento", appointment.Motivo);
        Assert.Equal(Modalidad.Presencial, appointment.Modalidad);
    }

    [Fact]
    public void Cancel_FirstTimeMarksAppointmentAndSecondTimeReturnsFalse()
    {
        var start = DateTime.UtcNow.AddDays(3);
        var appointment = Appointment.Create(
            1,
            2,
            start,
            start.AddHours(1),
            "Consulta",
            Modalidad.Online);
        var cancelledAt = DateTime.UtcNow;

        var firstCancellation = appointment.Cancel(cancelledAt);
        var secondCancellation = appointment.Cancel(cancelledAt.AddMinutes(1));

        Assert.True(firstCancellation);
        Assert.False(secondCancellation);
        Assert.Equal(AppointmentStatus.Cancelada, appointment.Status);
        Assert.Equal(cancelledAt, appointment.CancelledAt);
    }
}

using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace anotterwebpage.Repositories.Tests;

public class AppointmentRepositoryTests
{
    [Fact]
    public async Task GetByPatientIdAsync_ReturnsOnlyThatPatientsAppointments()
    {
        await using var context = CreateContext();
        var (patient, otherPatient, doctor) = await SeedProfilesAsync(context);
        var start = DateTime.UtcNow.AddDays(3);

        context.Appointments.AddRange(
            CreateAppointment(patient, doctor, start),
            CreateAppointment(otherPatient, doctor, start.AddHours(2)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new AppointmentRepository(context)
            .GetByPatientIdAsync(patient.Id);

        var appointment = Assert.Single(result);
        Assert.Equal(patient.Id, appointment.PatientId);
        Assert.Equal("Ana", appointment.Patient.Nombre);
        Assert.Equal("doctora@example.com", appointment.Doctor.Email);
    }

    [Fact]
    public async Task GetByDoctorIdAsync_ReturnsAllAppointmentsIncludingCancelledHistory()
    {
        await using var context = CreateContext();
        var (patient, otherPatient, doctor) = await SeedProfilesAsync(context);
        var start = DateTime.UtcNow.AddDays(3);

        context.Appointments.AddRange(
            CreateAppointment(patient, doctor, start),
            CreateAppointment(
                otherPatient,
                doctor,
                start.AddHours(2),
                AppointmentStatus.Cancelada));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new AppointmentRepository(context)
            .GetByDoctorIdAsync(doctor.Id);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, appointment =>
            appointment.Status == AppointmentStatus.Cancelada);
    }

    [Fact]
    public async Task GetBusyAsync_ExcludesCancelledAppointments()
    {
        await using var context = CreateContext();
        var (patient, otherPatient, doctor) = await SeedProfilesAsync(context);
        var start = DateTime.UtcNow.AddDays(3);

        context.Appointments.AddRange(
            CreateAppointment(patient, doctor, start),
            CreateAppointment(
                otherPatient,
                doctor,
                start,
                AppointmentStatus.Cancelada));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new AppointmentRepository(context)
            .GetBusyAsync(start, start.AddHours(1));

        var appointment = Assert.Single(result);
        Assert.Equal(AppointmentStatus.Confirmada, appointment.Status);
    }

    [Fact]
    public async Task HasOverlapAsync_IgnoresCancelledAppointments()
    {
        await using var context = CreateContext();
        var (patient, _, doctor) = await SeedProfilesAsync(context);
        var start = DateTime.UtcNow.AddDays(3);

        context.Appointments.Add(CreateAppointment(
            patient,
            doctor,
            start,
            AppointmentStatus.Cancelada));
        await context.SaveChangesAsync();

        var result = await new AppointmentRepository(context)
            .HasOverlapAsync(start, start.AddHours(1));

        Assert.False(result);
    }

    [Fact]
    public async Task HasOverlapAsync_WithExcludedId_IgnoresCancelledAppointments()
    {
        await using var context = CreateContext();
        var (patient, otherPatient, doctor) = await SeedProfilesAsync(context);
        var start = DateTime.UtcNow.AddDays(3);
        var appointment = CreateAppointment(patient, doctor, start);

        context.Appointments.AddRange(
            appointment,
            CreateAppointment(
                otherPatient,
                doctor,
                start,
                AppointmentStatus.Cancelada));
        await context.SaveChangesAsync();

        var result = await new AppointmentRepository(context)
            .HasOverlapAsync(start, start.AddHours(1), appointment.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCancelledAppointmentForHistory()
    {
        await using var context = CreateContext();
        var (patient, _, doctor) = await SeedProfilesAsync(context);
        var start = DateTime.UtcNow.AddDays(3);
        var appointment = CreateAppointment(
            patient,
            doctor,
            start,
            AppointmentStatus.Cancelada);
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new AppointmentRepository(context)
            .GetByIdAsync(appointment.Id);

        Assert.NotNull(result);
        Assert.Equal(AppointmentStatus.Cancelada, result.Status);
        Assert.Equal(patient.Id, result.Patient.Id);
        Assert.Equal(doctor.Id, result.Doctor.Id);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Patient Patient, Patient OtherPatient, Doctor Doctor)>
        SeedProfilesAsync(ApplicationDbContext context)
    {
        var patient = new Patient
        {
            UserId = "patient-user",
            Nombre = "Ana",
            Apellido = "Pérez",
            Numero = "6641234567"
        };
        var otherPatient = new Patient
        {
            UserId = "other-patient-user",
            Nombre = "Luis",
            Apellido = "García",
            Numero = "6647654321"
        };
        var doctor = new Doctor
        {
            UserId = "doctor-user",
            Email = "doctora@example.com"
        };

        context.AddRange(patient, otherPatient, doctor);
        await context.SaveChangesAsync();

        return (patient, otherPatient, doctor);
    }

    private static Appointment CreateAppointment(
        Patient patient,
        Doctor doctor,
        DateTime start,
        AppointmentStatus status = AppointmentStatus.Confirmada) =>
        new()
        {
            Start = start,
            End = start.AddHours(1),
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            Status = status,
            CancelledAt = status == AppointmentStatus.Cancelada
                ? DateTime.UtcNow
                : null,
            Motivo = "Consulta",
            Modalidad = Modalidad.Online
        };
}

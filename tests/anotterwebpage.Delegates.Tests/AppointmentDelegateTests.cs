using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Delegate;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Repositories;
using anotterwebpage_WebApi.Services;
using Moq;

namespace anotterwebpage.Delegates.Tests;

public class AppointmentDelegateTests
{
    private readonly Mock<IAppointmentRepository> _appointmentRepository = new();
    private readonly Mock<IPatientRepository> _patientRepository = new();
    private readonly Mock<IDoctorRepository> _doctorRepository = new();
    private readonly Mock<IAppointmentNotificationRepository> _notificationRepository = new();
    private readonly Mock<IAppointmentNotificationFactory> _notificationFactory = new();
    private readonly AppointmentDelegate _delegate;

    public AppointmentDelegateTests()
    {
        _delegate = new AppointmentDelegate(
            _appointmentRepository.Object,
            _patientRepository.Object,
            _doctorRepository.Object,
            _notificationRepository.Object,
            _notificationFactory.Object);

        _notificationFactory.Setup(factory => factory.CreateAsync(
                It.IsAny<Appointment>(),
                It.IsAny<AppointmentNotificationType>(),
                It.IsAny<AppointmentDetails?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment appointment,
                AppointmentNotificationType type,
                AppointmentDetails? previousDetails,
                CancellationToken cancellationToken) => CreateNotifications(appointment, type));
    }

    [Fact]
    public async Task BookAsync_ResolvesProfilesAndCreatesConfirmedAppointment()
    {
        var start = DateTime.UtcNow.AddDays(3);
        var request = CreateRequest(start);
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        _patientRepository.Setup(repository => repository.GetByUserIdAsync("patient-user"))
            .ReturnsAsync(patient);
        _doctorRepository.Setup(repository => repository.GetActiveAsync())
            .ReturnsAsync(doctor);
        _appointmentRepository.Setup(repository => repository.HasOverlapAsync(
                request.Start,
                request.End))
            .ReturnsAsync(false);
        _appointmentRepository.Setup(repository => repository.CreateAsync(It.IsAny<Appointment>()))
            .ReturnsAsync((Appointment appointment) => appointment);

        var result = await _delegate.BookAsync(request, "patient-user");

        Assert.Equal(patient.Id, result.PatientId);
        Assert.Equal(doctor.Id, result.DoctorId);
        Assert.Equal(AppointmentStatus.Confirmada, result.Status);
        _appointmentRepository.Verify(repository => repository.CreateAsync(
            It.Is<Appointment>(appointment =>
                appointment.Patient == patient && appointment.Doctor == doctor)), Times.Once);
        _notificationRepository.Verify(repository => repository.AddRange(
            It.Is<IEnumerable<AppointmentNotification>>(notifications =>
                notifications.Count() == 2 &&
                notifications.All(notification =>
                    notification.Type == AppointmentNotificationType.Creada))), Times.Once);
    }

    [Fact]
    public async Task BookAsync_WhenSlotOverlaps_DoesNotCreateAppointment()
    {
        var start = DateTime.UtcNow.AddDays(3);
        var request = CreateRequest(start);
        _patientRepository.Setup(repository => repository.GetByUserIdAsync("patient-user"))
            .ReturnsAsync(CreatePatient());
        _doctorRepository.Setup(repository => repository.GetActiveAsync())
            .ReturnsAsync(CreateDoctor());
        _appointmentRepository.Setup(repository => repository.HasOverlapAsync(
                request.Start,
                request.End))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _delegate.BookAsync(request, "patient-user"));

        _appointmentRepository.Verify(
            repository => repository.CreateAsync(It.IsAny<Appointment>()),
            Times.Never);
        _notificationRepository.Verify(
            repository => repository.AddRange(It.IsAny<IEnumerable<AppointmentNotification>>()),
            Times.Never);
    }

    [Fact]
    public async Task BookAsync_WithoutPatientProfile_ThrowsBusinessViolation()
    {
        var request = CreateRequest(DateTime.UtcNow.AddDays(3));
        _patientRepository.Setup(repository => repository.GetByUserIdAsync("patient-user"))
            .ReturnsAsync((Patient?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _delegate.BookAsync(request, "patient-user"));

        _doctorRepository.Verify(repository => repository.GetActiveAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenAppointmentIsCancelled_ThrowsBusinessViolation()
    {
        var start = DateTime.UtcNow.AddDays(3);
        var appointment = CreateAppointment(
            AppointmentStatus.Cancelada,
            start);
        _appointmentRepository.Setup(repository => repository.GetByIdAsync(appointment.Id))
            .ReturnsAsync(appointment);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _delegate.UpdateAsync(
            appointment.Id,
            CreateUpdateRequest(start.AddDays(1)),
            "patient-user"));

        _appointmentRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Appointment>()),
            Times.Never);
        _notificationRepository.Verify(
            repository => repository.AddRange(It.IsAny<IEnumerable<AppointmentNotification>>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithEffectiveChange_UpdatesAppointment()
    {
        var appointment = CreateAppointment();
        var request = CreateUpdateRequest(appointment.Start.AddDays(1));
        _appointmentRepository.Setup(repository => repository.GetByIdAsync(appointment.Id))
            .ReturnsAsync(appointment);
        _appointmentRepository.Setup(repository => repository.HasOverlapAsync(
                request.Start,
                request.End,
                appointment.Id))
            .ReturnsAsync(false);
        _appointmentRepository.Setup(repository => repository.UpdateAsync(appointment))
            .ReturnsAsync(appointment);

        var result = await _delegate.UpdateAsync(appointment.Id, request, "patient-user");

        Assert.NotNull(result);
        Assert.Equal(request.Start, result.Start);
        Assert.Equal(request.Motivo, result.Motivo);
        _appointmentRepository.Verify(repository => repository.UpdateAsync(appointment), Times.Once);
        _notificationRepository.Verify(repository => repository.AddRange(
            It.Is<IEnumerable<AppointmentNotification>>(notifications =>
                notifications.Count() == 2 &&
                notifications.All(notification =>
                    notification.Type == AppointmentNotificationType.Modificada))), Times.Once);
    }

    [Fact]
    public async Task GetAppointmentAsync_AllowsTheAppointmentsDoctor()
    {
        var appointment = CreateAppointment();
        _appointmentRepository.Setup(repository => repository.GetByIdAsync(appointment.Id))
            .ReturnsAsync(appointment);
        _doctorRepository.Setup(repository => repository.GetByUserIdAsync("doctor-user"))
            .ReturnsAsync(appointment.Doctor);

        var result = await _delegate.GetAppointmentAsync(appointment.Id, "doctor-user");

        Assert.Same(appointment, result);
    }

    [Fact]
    public async Task UpdateAsync_WithoutEffectiveChange_DoesNotPersistOrNotify()
    {
        var appointment = CreateAppointment();
        var request = CreateUpdateRequest(appointment.Start);
        _appointmentRepository.Setup(repository => repository.GetByIdAsync(appointment.Id))
            .ReturnsAsync(appointment);
        _appointmentRepository.Setup(repository => repository.HasOverlapAsync(
                request.Start,
                request.End,
                appointment.Id))
            .ReturnsAsync(false);

        var result = await _delegate.UpdateAsync(appointment.Id, request, "patient-user");

        Assert.Same(appointment, result);
        _appointmentRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Appointment>()),
            Times.Never);
        _notificationRepository.Verify(
            repository => repository.AddRange(It.IsAny<IEnumerable<AppointmentNotification>>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenCallerDoesNotOwnAppointment_DoesNotPersistOrNotify()
    {
        var appointment = CreateAppointment();
        var request = CreateUpdateRequest(appointment.Start.AddDays(1));
        _appointmentRepository.Setup(repository => repository.GetByIdAsync(appointment.Id))
            .ReturnsAsync(appointment);
        _doctorRepository.Setup(repository => repository.GetByUserIdAsync("other-user"))
            .ReturnsAsync((Doctor?)null);

        var result = await _delegate.UpdateAsync(appointment.Id, request, "other-user");

        Assert.Null(result);
        _appointmentRepository.Verify(
            repository => repository.HasOverlapAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>()),
            Times.Never);
        _appointmentRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Appointment>()),
            Times.Never);
        _notificationRepository.Verify(
            repository => repository.AddRange(It.IsAny<IEnumerable<AppointmentNotification>>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelAsync_FirstCancellationMarksAppointmentAndPersistsIt()
    {
        var appointment = CreateAppointment();
        _appointmentRepository.Setup(repository => repository.GetByIdAsync(appointment.Id))
            .ReturnsAsync(appointment);
        _appointmentRepository.Setup(repository => repository.UpdateAsync(appointment))
            .ReturnsAsync(appointment);

        var result = await _delegate.CancelAsync(appointment.Id, "patient-user");

        Assert.Equal(AppointmentCancellationResult.Cancelled, result);
        Assert.Equal(AppointmentStatus.Cancelada, appointment.Status);
        Assert.NotNull(appointment.CancelledAt);
        _appointmentRepository.Verify(repository => repository.UpdateAsync(appointment), Times.Once);
        _notificationRepository.Verify(repository => repository.AddRange(
            It.Is<IEnumerable<AppointmentNotification>>(notifications =>
                notifications.Count() == 2 &&
                notifications.All(notification =>
                    notification.Type == AppointmentNotificationType.Cancelada))), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_DoesNotPersistAgain()
    {
        var appointment = CreateAppointment(AppointmentStatus.Cancelada);
        _appointmentRepository.Setup(repository => repository.GetByIdAsync(appointment.Id))
            .ReturnsAsync(appointment);

        var result = await _delegate.CancelAsync(appointment.Id, "patient-user");

        Assert.Equal(AppointmentCancellationResult.AlreadyCancelled, result);
        _appointmentRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Appointment>()),
            Times.Never);
        _notificationRepository.Verify(
            repository => repository.AddRange(It.IsAny<IEnumerable<AppointmentNotification>>()),
            Times.Never);
    }

    private static CreateAppointmentDto CreateRequest(DateTime start) =>
        new()
        {
            Start = start,
            End = start.AddHours(1),
            Motivo = "Consulta",
            Modalidad = Modalidad.Online
        };

    private static UpdateAppointmentDto CreateUpdateRequest(DateTime start) =>
        new()
        {
            Start = start,
            End = start.AddHours(1),
            Motivo = "Consulta",
            Modalidad = Modalidad.Online
        };

    private static Patient CreatePatient() =>
        new()
        {
            Id = 1,
            UserId = "patient-user",
            Nombre = "Ana",
            Apellido = "Pérez",
            Numero = "6641234567"
        };

    private static Doctor CreateDoctor() =>
        new()
        {
            Id = 2,
            UserId = "doctor-user",
            Email = "doctora@example.com"
        };

    private static Appointment CreateAppointment(
        AppointmentStatus status = AppointmentStatus.Confirmada,
        DateTime? start = null)
    {
        var appointmentStart = start ?? DateTime.UtcNow.AddDays(3);
        var patient = CreatePatient();
        var doctor = CreateDoctor();
        var appointment = Appointment.Create(
            patient.Id,
            doctor.Id,
            appointmentStart,
            appointmentStart.AddHours(1),
            "Consulta",
            Modalidad.Online);
        appointment.Id = 10;
        appointment.Patient = patient;
        appointment.Doctor = doctor;

        if (status == AppointmentStatus.Cancelada)
        {
            appointment.Cancel(DateTime.UtcNow);
        }

        return appointment;
    }

    private static IReadOnlyCollection<AppointmentNotification> CreateNotifications(
        Appointment appointment,
        AppointmentNotificationType type)
    {
        var eventKey = $"test:{type}:{Guid.NewGuid():N}";
        return new[]
        {
            new AppointmentNotification
            {
                Appointment = appointment,
                Type = type,
                EventKey = eventKey,
                RecipientEmail = "patient@example.com",
                Subject = "Test",
                Body = "Patient"
            },
            new AppointmentNotification
            {
                Appointment = appointment,
                Type = type,
                EventKey = eventKey,
                RecipientEmail = "doctor@example.com",
                Subject = "Test",
                Body = "Doctor"
            }
        };
    }
}

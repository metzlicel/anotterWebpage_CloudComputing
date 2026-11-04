using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Repositories;
using anotterwebpage_WebApi.Services;

namespace anotterwebpage_WebApi.Delegate;

public class AppointmentDelegate : IAppointmentDelegate
{
    private readonly IAppointmentRepository _repository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IAppointmentNotificationRepository _notificationRepository;
    private readonly IAppointmentNotificationFactory _notificationFactory;

    public AppointmentDelegate(
        IAppointmentRepository repository,
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        IAppointmentNotificationRepository notificationRepository,
        IAppointmentNotificationFactory notificationFactory)
    {
        _repository = repository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _notificationRepository = notificationRepository;
        _notificationFactory = notificationFactory;
    }

    public async Task<List<Appointment>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Appointment?> UpdateAsync(
        int id,
        UpdateAppointmentDto request,
        string? userId)
    {
        var appointment = await _repository.GetByIdAsync(id);

        if (appointment is null ||
            !await CanManageAsync(appointment, userId))
            return null;

        if (appointment.Status == AppointmentStatus.Cancelada)
        {
            throw new InvalidOperationException(
                "La cita cancelada no puede modificarse.");
        }

        var overlap = await _repository.HasOverlapAsync(
            request.Start,
            request.End,
            id);

        if (overlap)
        {
            throw new InvalidOperationException(
                "Ese horario ya está reservado.");
        }

        if (!request.Modalidad.HasValue)
        {
            throw new InvalidOperationException(
                "La modalidad es obligatoria.");
        }

        if (HasNoEffectiveChanges(appointment, request))
        {
            return appointment;
        }

        var previousDetails = appointment.Snapshot();
        appointment.UpdateDetails(
            request.Start,
            request.End,
            request.Motivo,
            request.Modalidad.Value);

        var notifications = await _notificationFactory.CreateAsync(
            appointment,
            AppointmentNotificationType.Modificada,
            previousDetails);
        _notificationRepository.AddRange(notifications);

        return await _repository.UpdateAsync(appointment);
    }

    public async Task<List<Appointment>> GetBusyAsync(
        DateTime start,
        DateTime end)
    {
        return await _repository.GetBusyAsync(start, end);
    }

    public async Task<Appointment?> GetAppointmentAsync(
        int id,
        string? userId)
    {
        var appointment = await _repository.GetByIdAsync(id);

        if (appointment is null ||
            !await CanManageAsync(appointment, userId))
            return null;

        return appointment;
    }

    public async Task<Appointment> BookAsync(
        CreateAppointmentDto request,
        string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException(
                "Debes iniciar sesión para reservar una cita.");
        }

        if (!request.Modalidad.HasValue)
        {
            throw new InvalidOperationException(
                "La modalidad es obligatoria.");
        }

        var patient = await _patientRepository.GetByUserIdAsync(userId);

        if (patient is null)
        {
            throw new InvalidOperationException(
                "La cuenta autenticada no tiene un perfil de paciente.");
        }

        var doctor = await _doctorRepository.GetActiveAsync();

        if (doctor is null)
        {
            throw new InvalidOperationException(
                "No hay una doctora activa para atender la cita.");
        }

        var overlap = await _repository.HasOverlapAsync(
            request.Start,
            request.End);

        if (overlap)
        {
            throw new InvalidOperationException(
                "Ese horario ya está reservado.");
        }

        var appointment = Appointment.Create(
            patient.Id,
            doctor.Id,
            request.Start,
            request.End,
            request.Motivo,
            request.Modalidad.Value);

        appointment.Patient = patient;
        appointment.Doctor = doctor;

        var notifications = await _notificationFactory.CreateAsync(
            appointment,
            AppointmentNotificationType.Creada);
        _notificationRepository.AddRange(notifications);

        return await _repository.CreateAsync(appointment);
    }

    public async Task<AppointmentCancellationResult> CancelAsync(
        int id,
        string? userId)
    {
        var appointment = await _repository.GetByIdAsync(id);

        if (appointment is null ||
            !await CanManageAsync(appointment, userId))
        {
            return AppointmentCancellationResult.NotFound;
        }

        if (!appointment.Cancel(DateTime.UtcNow))
        {
            return AppointmentCancellationResult.AlreadyCancelled;
        }

        var notifications = await _notificationFactory.CreateAsync(
            appointment,
            AppointmentNotificationType.Cancelada);
        _notificationRepository.AddRange(notifications);

        await _repository.UpdateAsync(appointment);

        return AppointmentCancellationResult.Cancelled;
    }

    private async Task<bool> CanManageAsync(
        Appointment appointment,
        string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        if (appointment.Patient.UserId == userId)
        {
            return true;
        }

        var doctor = await _doctorRepository.GetByUserIdAsync(userId);
        return doctor?.Id == appointment.DoctorId;
    }

    private static bool HasNoEffectiveChanges(
        Appointment appointment,
        UpdateAppointmentDto request)
    {
        return appointment.Start == request.Start &&
               appointment.End == request.End &&
               request.Motivo is not null &&
               appointment.Motivo == request.Motivo.Trim() &&
               appointment.Modalidad == request.Modalidad;
    }
}

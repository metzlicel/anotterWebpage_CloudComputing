using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Repositories;

namespace anotterwebpage_WebApi.Services;

public class AppointmentConsultationDetailService
    : IAppointmentConsultationDetailService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IAppointmentConsultationDetailRepository _detailRepository;

    public AppointmentConsultationDetailService(
        IAppointmentRepository appointmentRepository,
        IAppointmentConsultationDetailRepository detailRepository)
    {
        _appointmentRepository = appointmentRepository;
        _detailRepository = detailRepository;
    }

    public async Task<AppointmentConsultationDetail?> GetAsync(
        int appointmentId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);

        if (appointment is null || !CanRead(appointment, userId))
        {
            return null;
        }

        return appointment.ConsultationDetail;
    }

    public async Task<AppointmentConsultationDetail?> SaveAsync(
        int appointmentId,
        UpsertAppointmentConsultationDetailDto request,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);

        if (appointment is null || !CanEdit(appointment, userId))
        {
            return null;
        }

        if (appointment.Status == AppointmentStatus.Cancelada)
        {
            throw new InvalidOperationException(
                "No se pueden registrar detalles para una cita cancelada.");
        }

        var detail = appointment.ConsultationDetail;

        if (detail is null)
        {
            detail = AppointmentConsultationDetail.Create(
                appointment.Id,
                request.Diagnostico,
                request.Recomendaciones,
                request.Observaciones);
            _detailRepository.Add(detail);
        }
        else
        {
            detail.Update(
                request.Diagnostico,
                request.Recomendaciones,
                request.Observaciones);
        }

        await _detailRepository.SaveChangesAsync(cancellationToken);

        return detail;
    }

    private static bool CanRead(Appointment appointment, string? userId)
    {
        return !string.IsNullOrWhiteSpace(userId) &&
               (appointment.Patient.UserId == userId ||
                appointment.Doctor.UserId == userId);
    }

    private static bool CanEdit(Appointment appointment, string? userId)
    {
        return !string.IsNullOrWhiteSpace(userId) &&
               appointment.Doctor.UserId == userId;
    }
}

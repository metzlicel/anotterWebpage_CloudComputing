using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Services;

public interface IAppointmentConsultationDetailService
{
    Task<AppointmentConsultationDetail?> GetAsync(
        int appointmentId,
        string? userId,
        CancellationToken cancellationToken = default);

    Task<AppointmentConsultationDetail?> SaveAsync(
        int appointmentId,
        UpsertAppointmentConsultationDetailDto request,
        string? userId,
        CancellationToken cancellationToken = default);
}

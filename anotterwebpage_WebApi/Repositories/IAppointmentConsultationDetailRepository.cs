using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Repositories;

public interface IAppointmentConsultationDetailRepository
{
    void Add(AppointmentConsultationDetail detail);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

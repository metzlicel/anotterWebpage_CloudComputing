using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Repositories;

public class AppointmentConsultationDetailRepository
    : IAppointmentConsultationDetailRepository
{
    private readonly ApplicationDbContext _context;

    public AppointmentConsultationDetailRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public void Add(AppointmentConsultationDetail detail)
    {
        _context.AppointmentConsultationDetails.Add(detail);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}

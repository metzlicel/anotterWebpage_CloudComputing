using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Repositories;

public interface IAppointmentRepository
{
    Task<List<Appointment>> GetBusyAsync(
        DateTime start,
        DateTime end);

    Task<Appointment?> GetByIdAsync(int id);

    Task<bool> HasOverlapAsync(
        DateTime start,
        DateTime end);

    Task<Appointment> CreateAsync(
        Appointment appointment);
}
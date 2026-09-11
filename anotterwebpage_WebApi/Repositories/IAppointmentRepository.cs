using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Repositories;

public interface IAppointmentRepository
{
    Task<List<Appointment>> GetAllAsync();
    Task<List<Appointment>> GetBusyAsync(
        DateTime start,
        DateTime end);

    Task<Appointment?> GetByIdAsync(int id);
    Task<bool> HasOverlapAsync(
        DateTime start,
        DateTime end);
    Task<bool> HasOverlapAsync(
        DateTime start,
        DateTime end,
        int excludeAppointmentId);

    Task<Appointment> CreateAsync(Appointment appointment);
    
    Task<Appointment> UpdateAsync(Appointment appointment);
    
    Task DeleteAsync(Appointment appointment);
}
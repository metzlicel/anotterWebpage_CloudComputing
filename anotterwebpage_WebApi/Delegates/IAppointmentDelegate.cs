using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Delegate;

public interface IAppointmentDelegate
{
    Task<List<Appointment>> GetAllAsync();
    Task<List<Appointment>> GetBusyAsync(
        DateTime start,
        DateTime end);

    Task<Appointment?> GetAppointmentAsync(
        int id,
        string? userId);

    Task<Appointment> BookAsync(
        CreateAppointmentDto request,
        string? userId);
    
    Task<Appointment?> UpdateAsync(
        int id,
        UpdateAppointmentDto request,
        string? userId);
    
    Task<bool> DeleteAsync(
        int id,
        string? userId);
}
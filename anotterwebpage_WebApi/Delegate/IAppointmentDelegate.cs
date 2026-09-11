using anotterwebpage_WebApi.Api.Requests;
using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Delegate;

public interface IAppointmentDelegate
{
    Task<List<Appointment>> GetBusyAsync(
        DateTime start,
        DateTime end);

    Task<Appointment?> GetAppointmentAsync(
        int id,
        string? userId);

    Task<Appointment> BookAsync(
        BookAppointmentRequest request,
        string? userId);
}
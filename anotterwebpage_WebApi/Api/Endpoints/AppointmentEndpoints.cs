using System.Security.Claims;
using anotterwebpage_WebApi.Api.Requests;
using anotterwebpage_WebApi.Delegate;
using anotterwebpage_WebApi.Api.Errors;
using FluentValidation;
namespace anotterwebpage_WebApi.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static RouteGroupBuilder MapAppointmentEndpoints(
        this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/appointments")
            .WithTags("Appointments");

        group.MapGet("/busy", GetBusy);

        group.MapGet("/{id:int}", GetAppointment);
           // .RequireAuthorization();

        group.MapPost("/", Book);
            //.RequireAuthorization();

        return group;
    }

    private static async Task<IResult> GetBusy(
        DateTime start,
        DateTime end,
        IAppointmentDelegate appointmentDelegate)
    {
        var appointments =
            await appointmentDelegate.GetBusyAsync(start, end);

        var response = appointments.Select(a => new
        {
            id = a.Id,
            title = "Reservado",
            start = a.Start,
            end = a.End
        });

        return Results.Ok(response);
    }

    private static async Task<IResult> GetAppointment(
        int id,
        ClaimsPrincipal user,
        IAppointmentDelegate appointmentDelegate)
    {
        var userId =
            user.FindFirstValue(ClaimTypes.NameIdentifier);

        var appointment =
            await appointmentDelegate.GetAppointmentAsync(
                id,
                userId);

        if (appointment == null)
            return ApiErrorResults.NotFound("Appointment not found.");

        return Results.Ok(new
        {
            appointment.Nombre,
            appointment.Apellido,
            appointment.Email,
            appointment.Numero,
            appointment.Motivo,
            appointment.Modalidad
        });
    }

    private static async Task<IResult> Book(
        BookAppointmentRequest request,
        ClaimsPrincipal user,
        IAppointmentDelegate appointmentDelegate,
        IValidator<BookAppointmentRequest> validator)
    {
        var validationResult =
            await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            return ApiErrorResults.Validation(errors);
        }

        var userId =
            user.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            var appointment =
                await appointmentDelegate.BookAsync(
                    request,
                    userId);

            return Results.Created(
                $"/api/appointments/{appointment.Id}",
                appointment);
        }
        catch (InvalidOperationException ex)
        {
            return ApiErrorResults.BusinessViolation(ex.Message);
        }
    }
}
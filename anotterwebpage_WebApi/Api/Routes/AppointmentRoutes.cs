using System.Security.Claims;
using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Delegate;
using anotterwebpage_WebApi.Api.Errors;
using anotterwebpage_WebApi.Api.Extensions;
using FluentValidation;

namespace anotterwebpage_WebApi.Api.Endpoints;

public static class AppointmentRoutes
{
    public static RouteGroupBuilder MapAppointmentEndpoints(
        this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/appointments")
            .WithTags("Appointments");

        group.MapGet("/", GetAll);
        
        group.MapGet("/busy", GetBusy);

        group.MapGet("/{id:int}", GetAppointment);
           // .RequireAuthorization();

        group.MapPost("/", Book);
            //.RequireAuthorization();
            
        group.MapPut("/{id:int}", Update);

        group.MapDelete("/{id:int}", Delete);
        
        return group;
    }

    private static async Task<IResult> GetAll(
        IAppointmentDelegate appointmentDelegate)
    {
        var appointments =
            await appointmentDelegate.GetAllAsync();

        return Results.Ok(
            appointments.Select(a => a.ToDto()).ToList());
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

        return Results.Ok(appointment.ToDto());
    }

    private static async Task<IResult> Book(
        CreateAppointmentDto request,
        ClaimsPrincipal user,
        IAppointmentDelegate appointmentDelegate,
        IValidator<CreateAppointmentDto> validator)
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
                appointment.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return ApiErrorResults.BusinessViolation(ex.Message);
        }
    }
    
    private static async Task<IResult> Update(
        int id,
        UpdateAppointmentDto request,
        ClaimsPrincipal user,
        IAppointmentDelegate appointmentDelegate,
        IValidator<UpdateAppointmentDto> validator)
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
                await appointmentDelegate.UpdateAsync(
                    id,
                    request,
                    userId);

            if (appointment == null)
            {
                return ApiErrorResults.NotFound(
                    "Appointment not found.");
            }

            return Results.Ok(appointment.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return ApiErrorResults.BusinessViolation(
                ex.Message);
        }
    }
    
    private static async Task<IResult> Delete(
        int id,
        ClaimsPrincipal user,
        IAppointmentDelegate appointmentDelegate)
    {
        var userId =
            user.FindFirstValue(ClaimTypes.NameIdentifier);

        var deleted =
            await appointmentDelegate.DeleteAsync(
                id,
                userId);

        if (!deleted)
        {
            return ApiErrorResults.NotFound(
                "Appointment not found.");
        }

        return Results.NoContent();
    }
}
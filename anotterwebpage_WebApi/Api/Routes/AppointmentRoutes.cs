using System.Security.Claims;
using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Delegate;
using anotterwebpage_WebApi.Api.Errors;
using anotterwebpage_WebApi.Api.Extensions;
using anotterwebpage_WebApi.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using anotterwebpage_WebApi.Services;

namespace anotterwebpage_WebApi.Api.Routes;

public static class AppointmentRoutes
{
    public static RouteGroupBuilder MapAppointmentRoutes(
        this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/appointments")
            .WithTags("Appointments");

        group.MapGet("/", GetAll)
            .RequireAuthorization(DoctorOnly());
        
        group.MapGet("/busy", GetBusy)
            .RequireAuthorization();

        group.MapGet("/{id}", GetAppointment)
            .RequireAuthorization(PatientOrDoctor());

        group.MapGet("/{id}/consultation-detail", GetConsultationDetail)
            .RequireAuthorization(PatientOrDoctor());

        group.MapPut("/{id}/consultation-detail", SaveConsultationDetail)
            .RequireAuthorization(DoctorOnly());

        group.MapPost("/", Book)
            .RequireAuthorization(PatientOnly());

        group.MapPut("/{id}", Update)
            .RequireAuthorization(PatientOrDoctor());


        group.MapDelete("/{id}", Delete)
            .RequireAuthorization(PatientOrDoctor());
        
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
        ClaimsPrincipal user,
        IAppointmentDelegate appointmentDelegate)
    {
        var appointments =
            await appointmentDelegate.GetBusyAsync(start, end);

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        var response = appointments.Select(a => a.ToBusyDto(userId));

        return Results.Ok(response);
    }

    private static async Task<IResult> GetAppointment(
        string id,
        ClaimsPrincipal user,
        IAppointmentDelegate appointmentDelegate)
    {
        if (!int.TryParse(id, out var appointmentId))
        {
            var errors = new Dictionary<string, string[]>
            {
                ["id"] = new[]
                {
                    "El formato del ID no es válido."
                }
            };

            return ApiErrorResults.Validation(errors);
        }

        var userId =
            user.FindFirstValue(ClaimTypes.NameIdentifier);

        var appointment =
            await appointmentDelegate.GetAppointmentAsync(
                appointmentId,
                userId);

        if (appointment == null)
        {
            return ApiErrorResults.NotFound(
                "Appointment not found.");
        }

        return Results.Ok(
            appointment.ToDto());
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

    private static async Task<IResult> GetConsultationDetail(
        string id,
        ClaimsPrincipal user,
        IAppointmentConsultationDetailService consultationDetailService)
    {
        if (!int.TryParse(id, out var appointmentId))
        {
            return InvalidAppointmentId();
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var detail = await consultationDetailService.GetAsync(
            appointmentId,
            userId);

        if (detail is null)
        {
            return ApiErrorResults.NotFound(
                "Consultation detail not found.");
        }

        return Results.Ok(detail.ToDto());
    }

    private static async Task<IResult> SaveConsultationDetail(
        string id,
        UpsertAppointmentConsultationDetailDto request,
        ClaimsPrincipal user,
        IAppointmentConsultationDetailService consultationDetailService,
        IValidator<UpsertAppointmentConsultationDetailDto> validator)
    {
        if (!int.TryParse(id, out var appointmentId))
        {
            return InvalidAppointmentId();
        }

        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return ApiErrorResults.Validation(
                validationResult.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.ErrorMessage).ToArray()));
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            var detail = await consultationDetailService.SaveAsync(
                appointmentId,
                request,
                userId);

            if (detail is null)
            {
                return ApiErrorResults.NotFound(
                    "Appointment not found.");
            }

            return Results.Ok(detail.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return ApiErrorResults.BusinessViolation(ex.Message);
        }
    }
    
    private static async Task<IResult> Update(
        string id,
        UpdateAppointmentDto request,
        ClaimsPrincipal user,
        IAppointmentDelegate appointmentDelegate,
        IValidator<UpdateAppointmentDto> validator)
    {
        if (!int.TryParse(id, out var appointmentId))
        {
            var errors = new Dictionary<string, string[]>
            {
                ["id"] = new[]
                {
                    "El formato del ID no es válido."
                }
            };

            return ApiErrorResults.Validation(errors);
        }

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
                    appointmentId,
                    request,
                    userId);

            if (appointment == null)
            {
                return ApiErrorResults.NotFound(
                    "Appointment not found.");
            }

            return Results.Ok(
                appointment.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return ApiErrorResults.BusinessViolation(
                ex.Message);
        }
    }
    
    private static async Task<IResult> Delete(
        string id,
        ClaimsPrincipal user,
        IAppointmentDelegate appointmentDelegate)
    {
        if (!int.TryParse(id, out var appointmentId))
        {
            var errors = new Dictionary<string, string[]>
            {
                ["id"] = new[]
                {
                    "El formato del ID no es válido."
                }
            };

            return ApiErrorResults.Validation(errors);
        }

        var userId =
            user.FindFirstValue(ClaimTypes.NameIdentifier);

        var cancellation =
            await appointmentDelegate.CancelAsync(
                appointmentId,
                userId);

        if (cancellation == AppointmentCancellationResult.NotFound)
        {
            return ApiErrorResults.NotFound(
                "Appointment not found.");
        }

        if (cancellation == AppointmentCancellationResult.AlreadyCancelled)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Appointment already cancelled",
                detail: "La cita ya estaba cancelada.");
        }

        return Results.NoContent();
    }

    private static AuthorizeAttribute PatientOnly() =>
        new()
        {
            Roles = IdentityRoles.Patient
        };

    private static AuthorizeAttribute DoctorOnly() =>
        new()
        {
            Roles = IdentityRoles.Doctor
        };

    private static AuthorizeAttribute PatientOrDoctor() =>
        new()
        {
            Roles = $"{IdentityRoles.Patient},{IdentityRoles.Doctor}"
        };

    private static IResult InvalidAppointmentId()
    {
        return ApiErrorResults.Validation(new Dictionary<string, string[]>
        {
            ["id"] = new[]
            {
                "El formato del ID no es válido."
            }
        });
    }
}

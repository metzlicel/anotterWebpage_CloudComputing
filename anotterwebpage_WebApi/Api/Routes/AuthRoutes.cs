using System.Security.Claims;
using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Api.Errors;
using anotterwebpage_WebApi.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Options;

namespace anotterwebpage_WebApi.Api.Routes;

public static class AuthRoutes
{
    public static RouteGroupBuilder MapAuthRoutes(
        this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", Register);
        group.MapPost("/login", Login);
        group.MapPost("/refresh", Refresh);

        group.MapPost("/patient-profile", CompletePatientProfile)
            .RequireAuthorization();

        return group;
    }

    private static async Task<IResult> Register(
        RegisterPatientDto request,
        IPatientRegistrationService registrationService,
        IValidator<RegisterPatientDto> validator)
    {
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
        {
            return ApiErrorResults.Validation(ToErrors(validation));
        }

        var result = await registrationService.RegisterAsync(request);

        if (!result.Succeeded)
        {
            return ApiErrorResults.Validation(result.Errors);
        }

        return Results.Created(
            "/api/auth/patient-profile",
            ToDto(result.Patient!));
    }

    private static async Task<IResult> CompletePatientProfile(
        CompletePatientProfileDto request,
        ClaimsPrincipal user,
        IPatientRegistrationService registrationService,
        IValidator<CompletePatientProfileDto> validator)
    {
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
        {
            return ApiErrorResults.Validation(ToErrors(validation));
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var result = await registrationService.CompleteProfileAsync(
            userId,
            request);

        if (!result.Succeeded)
        {
            return ApiErrorResults.Validation(result.Errors);
        }

        return Results.Ok(ToDto(result.Patient!));
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        bool? useCookies,
        bool? useSessionCookies,
        SignInManager<IdentityUser> signInManager)
    {
        var useCookieScheme = useCookies == true || useSessionCookies == true;
        var isPersistent = useCookies == true && useSessionCookies != true;
        signInManager.AuthenticationScheme = useCookieScheme
            ? IdentityConstants.ApplicationScheme
            : IdentityConstants.BearerScheme;

        var result = await signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent,
            lockoutOnFailure: true);

        if (result.RequiresTwoFactor)
        {
            if (!string.IsNullOrEmpty(request.TwoFactorCode))
            {
                result = await signInManager.TwoFactorAuthenticatorSignInAsync(
                    request.TwoFactorCode,
                    isPersistent,
                    rememberClient: isPersistent);
            }
            else if (!string.IsNullOrEmpty(request.TwoFactorRecoveryCode))
            {
                result = await signInManager.TwoFactorRecoveryCodeSignInAsync(
                    request.TwoFactorRecoveryCode);
            }
        }

        return result.Succeeded
            ? Results.Empty
            : Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");
    }

    private static async Task<IResult> Refresh(
        RefreshRequest request,
        SignInManager<IdentityUser> signInManager,
        IOptionsMonitor<BearerTokenOptions> bearerTokenOptions,
        TimeProvider timeProvider)
    {
        var refreshTokenProtector = bearerTokenOptions
            .Get(IdentityConstants.BearerScheme)
            .RefreshTokenProtector;
        var refreshTicket = refreshTokenProtector.Unprotect(
            request.RefreshToken);

        if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc ||
            timeProvider.GetUtcNow() >= expiresUtc ||
            await signInManager.ValidateSecurityStampAsync(
                refreshTicket.Principal) is not IdentityUser user)
        {
            return Results.Challenge();
        }

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        return Results.SignIn(
            principal,
            authenticationScheme: IdentityConstants.BearerScheme);
    }

    private static Dictionary<string, string[]> ToErrors(
        FluentValidation.Results.ValidationResult validation) =>
        validation.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

    private static PatientProfileDto ToDto(Domain.Patient patient) =>
        new()
        {
            Id = patient.Id,
            Nombre = patient.Nombre,
            Apellido = patient.Apellido,
            Numero = patient.Numero
        };
}

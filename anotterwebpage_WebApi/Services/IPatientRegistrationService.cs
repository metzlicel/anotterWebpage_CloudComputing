using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Domain;

namespace anotterwebpage_WebApi.Services;

public interface IPatientRegistrationService
{
    Task<PatientRegistrationResult> RegisterAsync(
        RegisterPatientDto request,
        CancellationToken cancellationToken = default);

    Task<PatientRegistrationResult> CompleteProfileAsync(
        string userId,
        CompletePatientProfileDto request,
        CancellationToken cancellationToken = default);
}

public sealed class PatientRegistrationResult
{
    private PatientRegistrationResult(
        Patient? patient,
        Dictionary<string, string[]> errors)
    {
        Patient = patient;
        Errors = errors;
    }

    public Patient? Patient { get; }

    public Dictionary<string, string[]> Errors { get; }

    public bool Succeeded => Patient is not null;

    public static PatientRegistrationResult Success(Patient patient) =>
        new(patient, new Dictionary<string, string[]>());

    public static PatientRegistrationResult Failure(
        string key,
        params string[] errors) =>
        new(null, new Dictionary<string, string[]> { [key] = errors });

    public static PatientRegistrationResult Failure(
        Dictionary<string, string[]> errors) =>
        new(null, errors);
}

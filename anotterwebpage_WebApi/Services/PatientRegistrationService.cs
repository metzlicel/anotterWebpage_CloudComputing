using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace anotterwebpage_WebApi.Services;

public class PatientRegistrationService : IPatientRegistrationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public PatientRegistrationService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<PatientRegistrationResult> RegisterAsync(
        RegisterPatientDto request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await BeginTransactionAsync(cancellationToken);

        var user = new IdentityUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var createUserResult = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!createUserResult.Succeeded)
        {
            await RollbackAsync(transaction, cancellationToken);
            return PatientRegistrationResult.Failure(
                ToErrors(createUserResult));
        }

        var addRoleResult = await _userManager.AddToRoleAsync(
            user,
            IdentityRoles.Patient);

        if (!addRoleResult.Succeeded)
        {
            await RollbackAsync(transaction, cancellationToken);
            return PatientRegistrationResult.Failure(
                ToErrors(addRoleResult));
        }

        var patient = new Patient
        {
            UserId = user.Id,
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Numero = request.Numero
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync(cancellationToken);
        await CommitAsync(transaction, cancellationToken);

        return PatientRegistrationResult.Success(patient);
    }

    public async Task<PatientRegistrationResult> CompleteProfileAsync(
        string userId,
        CompletePatientProfileDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return PatientRegistrationResult.Failure(
                "user",
                "La cuenta autenticada no existe.");
        }

        await using var transaction = await BeginTransactionAsync(cancellationToken);

        var patient = await _context.Patients
            .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (patient is null)
        {
            patient = new Patient
            {
                UserId = user.Id,
                Nombre = request.Nombre,
                Apellido = request.Apellido,
                Numero = request.Numero
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (!await _userManager.IsInRoleAsync(user, IdentityRoles.Patient))
        {
            var addRoleResult = await _userManager.AddToRoleAsync(
                user,
                IdentityRoles.Patient);

            if (!addRoleResult.Succeeded)
            {
                await RollbackAsync(transaction, cancellationToken);
                return PatientRegistrationResult.Failure(
                    ToErrors(addRoleResult));
            }
        }

        await CommitAsync(transaction, cancellationToken);
        return PatientRegistrationResult.Success(patient);
    }

    private async Task<IDbContextTransaction?> BeginTransactionAsync(
        CancellationToken cancellationToken)
    {
        return _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;
    }

    private static Task CommitAsync(
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken) =>
        transaction?.CommitAsync(cancellationToken) ?? Task.CompletedTask;

    private static Task RollbackAsync(
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken) =>
        transaction?.RollbackAsync(cancellationToken) ?? Task.CompletedTask;

    private static Dictionary<string, string[]> ToErrors(
        IdentityResult result)
    {
        return result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());
    }
}

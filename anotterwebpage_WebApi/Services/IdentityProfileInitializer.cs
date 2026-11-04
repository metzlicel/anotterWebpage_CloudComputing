using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace anotterwebpage_WebApi.Services;

public class IdentityProfileInitializer : IIdentityProfileInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityProfileInitializer> _logger;

    public IdentityProfileInitializer(
        ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        IConfiguration configuration,
        ILogger<IdentityProfileInitializer> logger)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureRoleAsync(IdentityRoles.Patient);
        await EnsureRoleAsync(IdentityRoles.Doctor);

        var userId = _configuration["DoctorBootstrap:UserId"];
        var email = _configuration["DoctorBootstrap:Email"];

        if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning(
                "No doctor bootstrap configuration was found. " +
                "Appointment booking remains unavailable until a Doctor profile is configured.");
            return;
        }

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "DoctorBootstrap:UserId and DoctorBootstrap:Email must both be configured.");
        }

        await InitializeDoctorProfileAsync(userId, email, cancellationToken);
    }

    public async Task InitializeDoctorAsync(
        string userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "A Doctor user ID and email must both be provided.");
        }

        await EnsureRoleAsync(IdentityRoles.Patient);
        await EnsureRoleAsync(IdentityRoles.Doctor);

        await InitializeDoctorProfileAsync(userId, email, cancellationToken);
    }

    private async Task InitializeDoctorProfileAsync(
        string userId,
        string email,
        CancellationToken cancellationToken)
    {

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            throw new InvalidOperationException(
                "The configured doctor Identity account does not exist.");
        }

        var doctorProfiles = await _context.Doctors
            .ToListAsync(cancellationToken);

        if (doctorProfiles.Any(doctor => doctor.UserId != userId))
        {
            throw new InvalidOperationException(
                "A different Doctor profile already exists. Only one active doctor is supported.");
        }

        var doctor = doctorProfiles.SingleOrDefault();

        if (doctor is null)
        {
            doctor = new Doctor
            {
                UserId = userId,
                Email = email
            };

            _context.Doctors.Add(doctor);
        }
        else if (!string.Equals(doctor.Email, email, StringComparison.Ordinal))
        {
            doctor.Email = email;
        }

        if (!await _userManager.IsInRoleAsync(user, IdentityRoles.Doctor))
        {
            var addRoleResult = await _userManager.AddToRoleAsync(
                user,
                IdentityRoles.Doctor);

            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to assign the Doctor role: {FormatErrors(addRoleResult)}");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Unable to create the {roleName} role: {FormatErrors(result)}");
        }
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join(
            "; ",
            result.Errors.Select(error => error.Description));
}

using anotterwebpage_WebApi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace anotterwebpage_WebApi.Services;

public class DoctorBootstrapService : IDoctorBootstrapService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IIdentityProfileInitializer _profileInitializer;
    private readonly IConfiguration _configuration;

    public DoctorBootstrapService(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IIdentityProfileInitializer profileInitializer,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _profileInitializer = profileInitializer;
        _configuration = configuration;
    }

    public async Task<DoctorBootstrapResult> BootstrapAsync(
        CancellationToken cancellationToken = default)
    {
        var email = _configuration["DoctorBootstrap:Email"];

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "DoctorBootstrap:Email must be configured.");
        }

        var configuredUserId = _configuration["DoctorBootstrap:UserId"];
        IdentityUser? user;

        if (!string.IsNullOrWhiteSpace(configuredUserId))
        {
            user = await _userManager.FindByIdAsync(configuredUserId);

            if (user is null)
            {
                throw new InvalidOperationException(
                    "DoctorBootstrap:UserId does not exist. Remove it before creating the initial doctor account.");
            }

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "DoctorBootstrap:UserId does not belong to the configured email address.");
            }
        }
        else
        {
            user = await _userManager.FindByEmailAsync(email);
        }

        var existingDoctor = await _context.Doctors
            .SingleOrDefaultAsync(cancellationToken);

        if (existingDoctor is not null &&
            (user is null || existingDoctor.UserId != user.Id))
        {
            throw new InvalidOperationException(
                "A different Doctor profile already exists. Only one active doctor is supported.");
        }

        var accountCreated = false;

        if (user is null)
        {
            var password = _configuration["DoctorBootstrap:Password"];

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "DoctorBootstrap:Password must be configured to create the initial doctor account.");
            }

            user = new IdentityUser
            {
                UserName = email,
                Email = email
            };

            var createResult = await _userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create the Doctor account: {FormatErrors(createResult)}");
            }

            accountCreated = true;
        }

        await _profileInitializer.InitializeDoctorAsync(
            user.Id,
            email,
            cancellationToken);

        return new DoctorBootstrapResult(user.Id, accountCreated);
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join(
            "; ",
            result.Errors.Select(error => error.Description));
}

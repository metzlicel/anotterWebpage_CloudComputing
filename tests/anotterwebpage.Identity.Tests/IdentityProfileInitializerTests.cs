using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace anotterwebpage.Identity.Tests;

public class IdentityProfileInitializerTests
{
    [Fact]
    public async Task InitializeAsync_CreatesRolesAndDoctorOnlyOnce()
    {
        const string doctorUserId = "doctor-user";
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["DoctorBootstrap:UserId"] = doctorUserId,
            ["DoctorBootstrap:Email"] = "doctora@example.com"
        });

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<IdentityUser>>();
        var doctorUser = new IdentityUser
        {
            Id = doctorUserId,
            UserName = "doctora@example.com",
            Email = "doctora@example.com"
        };

        Assert.True((await userManager.CreateAsync(doctorUser, "Secure1!")).Succeeded);

        var initializer = scope.ServiceProvider
            .GetRequiredService<IIdentityProfileInitializer>();
        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        Assert.Equal(2, await context.Roles.CountAsync());
        var doctor = Assert.Single(await context.Doctors.ToListAsync());
        Assert.Equal(doctorUserId, doctor.UserId);
        Assert.Equal("doctora@example.com", doctor.Email);
        Assert.True(await userManager.IsInRoleAsync(doctorUser, IdentityRoles.Doctor));
    }

    [Fact]
    public async Task BootstrapAsync_CreatesInitialDoctorAccountProfileAndRoleIdempotently()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["DoctorBootstrap:Email"] = "doctora@example.com",
            ["DoctorBootstrap:Password"] = "Secure1!"
        });

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var bootstrapService = scope.ServiceProvider
            .GetRequiredService<IDoctorBootstrapService>();
        var firstBootstrap = await bootstrapService.BootstrapAsync();
        var secondBootstrap = await bootstrapService.BootstrapAsync();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<IdentityUser>>();
        var doctor = Assert.Single(await context.Doctors.ToListAsync());

        Assert.True(firstBootstrap.AccountCreated);
        Assert.False(secondBootstrap.AccountCreated);
        Assert.Equal(firstBootstrap.UserId, secondBootstrap.UserId);
        Assert.Equal(firstBootstrap.UserId, doctor.UserId);
        Assert.Equal("doctora@example.com", doctor.Email);
        Assert.True(await userManager.IsInRoleAsync(
            (await userManager.FindByIdAsync(firstBootstrap.UserId))!,
            IdentityRoles.Doctor));
        Assert.Equal(1, await context.Users.CountAsync());
        Assert.Equal(2, await context.Roles.CountAsync());
    }

    [Fact]
    public async Task BootstrapAsync_WithUnknownConfiguredUserId_DoesNotCreateAccount()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["DoctorBootstrap:UserId"] = "missing-user",
            ["DoctorBootstrap:Email"] = "doctora@example.com",
            ["DoctorBootstrap:Password"] = "Secure1!"
        });

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        var bootstrapService = scope.ServiceProvider
            .GetRequiredService<IDoctorBootstrapService>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => bootstrapService.BootstrapAsync());

        Assert.Empty(await context.Users.ToListAsync());
        Assert.Empty(await context.Doctors.ToListAsync());
    }

    [Fact]
    public async Task RegisterAsync_CreatesAccountProfileAndPatientRole()
    {
        var provider = BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var initializer = scope.ServiceProvider
            .GetRequiredService<IIdentityProfileInitializer>();
        await initializer.InitializeAsync();

        var registration = scope.ServiceProvider
            .GetRequiredService<IPatientRegistrationService>();
        var result = await registration.RegisterAsync(new()
        {
            Email = "paciente@example.com",
            Password = "Secure1!",
            Nombre = "Ana",
            Apellido = "Pérez",
            Numero = "6641234567"
        });

        Assert.True(result.Succeeded);
        var patient = Assert.IsType<Patient>(result.Patient);
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync("paciente@example.com");

        Assert.NotNull(user);
        Assert.Equal(user!.Id, patient.UserId);
        Assert.True(await userManager.IsInRoleAsync(user, IdentityRoles.Patient));
        Assert.Equal(1, await context.Patients.CountAsync());
    }

    [Fact]
    public async Task CompleteProfileAsync_LinksExistingIdentityAccountToPatientRole()
    {
        var provider = BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var initializer = scope.ServiceProvider
            .GetRequiredService<IIdentityProfileInitializer>();
        await initializer.InitializeAsync();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser
        {
            UserName = "legacy@example.com",
            Email = "legacy@example.com"
        };
        Assert.True((await userManager.CreateAsync(user, "Secure1!")).Succeeded);

        var registration = scope.ServiceProvider
            .GetRequiredService<IPatientRegistrationService>();
        var result = await registration.CompleteProfileAsync(user.Id, new()
        {
            Nombre = "Luis",
            Apellido = "García",
            Numero = "6647654321"
        });

        Assert.True(result.Succeeded);
        Assert.Equal(user.Id, result.Patient!.UserId);
        Assert.True(await userManager.IsInRoleAsync(user, IdentityRoles.Patient));
    }

    [Fact]
    public async Task NotificationFactory_UsesPatientIdentityEmailAndDoctorProfileEmail()
    {
        var provider = BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<IdentityUser>>();
        var patientUser = new IdentityUser
        {
            Id = "patient-user",
            UserName = "patient@example.com",
            Email = "patient@example.com"
        };
        Assert.True((await userManager.CreateAsync(patientUser, "Secure1!")).Succeeded);

        var start = DateTime.UtcNow.AddDays(3);
        var patient = new Patient
        {
            Id = 1,
            UserId = patientUser.Id,
            Nombre = "Ana",
            Apellido = "Pérez",
            Numero = "6641234567"
        };
        var doctor = new Doctor
        {
            Id = 2,
            UserId = "doctor-user",
            Email = "doctora@example.com"
        };
        var appointment = Appointment.Create(
            patient.Id,
            doctor.Id,
            start,
            start.AddHours(1),
            "Consulta",
            Modalidad.Online);
        appointment.Patient = patient;
        appointment.Doctor = doctor;

        var factory = scope.ServiceProvider
            .GetRequiredService<IAppointmentNotificationFactory>();
        var notifications = await factory.CreateAsync(
            appointment,
            AppointmentNotificationType.Modificada,
            appointment.Snapshot());

        Assert.Equal(2, notifications.Count);
        Assert.Contains(notifications, notification =>
            notification.RecipientEmail == "patient@example.com");
        Assert.Contains(notifications, notification =>
            notification.RecipientEmail == "doctora@example.com");
        Assert.All(notifications, notification =>
            Assert.Contains("Horario anterior", notification.Body));
    }

    private static ServiceProvider BuildServiceProvider(
        Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<IdentityUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<IPatientRegistrationService, PatientRegistrationService>();
        services.AddScoped<IIdentityProfileInitializer, IdentityProfileInitializer>();
        services.AddScoped<IDoctorBootstrapService, DoctorBootstrapService>();
        services.AddScoped<IAppointmentNotificationFactory, AppointmentNotificationFactory>();

        return services.BuildServiceProvider();
    }
}

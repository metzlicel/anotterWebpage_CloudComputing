using System.Net;
using System.Net.Http.Json;
using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace anotterwebpage.Identity.Tests;

public class AuthRoutesTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AuthRoutesTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_CreatesOnlyTheCompletePatientAccount()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterPatientDto
        {
            Email = "patient.route@example.com",
            Password = "Secure1!",
            Nombre = "María",
            Apellido = "López",
            Numero = "6641112222"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<PatientProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal("María", profile!.Nombre);

        await _factory.AssertPatientAccountAsync("patient.route@example.com");

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "patient.route@example.com",
            password = "Secure1!"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var tokenResponse = await loginResponse.Content.ReadAsStringAsync();
        Assert.Contains("accessToken", tokenResponse);
    }
}

public class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"IdentityTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DoctorBootstrap:UserId"] = string.Empty,
                ["DoctorBootstrap:Email"] = string.Empty
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var initializer = scope.ServiceProvider
            .GetRequiredService<IIdentityProfileInitializer>();
        await initializer.InitializeAsync();
    }

    public async Task AssertPatientAccountAsync(string email)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(email);

        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user!, IdentityRoles.Patient));
        var patient = await context.Patients
            .SingleOrDefaultAsync(profile => profile.UserId == user!.Id);
        Assert.NotNull(patient);
    }
}

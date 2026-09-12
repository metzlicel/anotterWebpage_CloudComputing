using anotterwebpage.Api.Tests.Fakes;
using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace anotterwebpage.Api.Tests;

public class AnotterwebpageApiTests
    : WebApplicationFactory<Program>
{
    private readonly string _databaseName =
        $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Eliminar completamente la configuración real de PostgreSQL
            services.RemoveAll<
                DbContextOptions<ApplicationDbContext>>();

            services.RemoveAll<
                IDbContextOptionsConfiguration<ApplicationDbContext>>();

            services.RemoveAll<ApplicationDbContext>();

            // Registrar únicamente EF Core InMemory para tests
            services.AddDbContext<ApplicationDbContext>(
                options =>
                {
                    options.UseInMemoryDatabase(
                        _databaseName);
                });

            // Evitar SMTP real durante integration tests
            services.RemoveAll<IEmailService>();

            services.AddSingleton<
                IEmailService,
                FakeEmailService>();
        });
    }

    public async Task ExecuteDbContextAsync(
        Func<ApplicationDbContext, Task> action)
    {
        using var scope =
            Services.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        await action(context);
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope =
            Services.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }
}
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using anotterwebpage_WebApi.Data;

using anotterwebpage_WebApi.Services;
using anotterwebpage.Api.Tests.Fakes;
using anotterwebpage_WebApi.Services;
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
        	var descriptor = services
           		.SingleOrDefault(d =>
                	d.ServiceType ==
                	typeof(DbContextOptions<ApplicationDbContext>));

        	if (descriptor != null)
        	{
            	services.Remove(descriptor);
        	}

        	services.AddDbContext<ApplicationDbContext>(
            	options =>
            	{
                	options.UseInMemoryDatabase(
                    	_databaseName);
            });

        	// Quitar el servicio real de correo
        	services.RemoveAll<IEmailService>();

        	// Usar un correo falso en integration tests
        	services.AddSingleton<
            	IEmailService,
            	FakeEmailService>();
    		});
		}

    public async Task ExecuteDbContextAsync(
        Func<ApplicationDbContext, Task> action)
    {
        using var scope = Services.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        await action(context);
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }
}
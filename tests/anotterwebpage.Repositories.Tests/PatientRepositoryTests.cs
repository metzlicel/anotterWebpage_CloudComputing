using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace anotterwebpage.Repositories.Tests;

public class PatientRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ReturnsMatchingPatient()
    {
        await using var context = CreateContext();
        var patient = new Patient
        {
            UserId = "patient-user",
            Nombre = "Ana",
            Apellido = "Pérez",
            Numero = "6641234567"
        };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var result = await new PatientRepository(context).GetByIdAsync(patient.Id);

        Assert.NotNull(result);
        Assert.Equal("patient-user", result.UserId);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsMatchingPatient()
    {
        await using var context = CreateContext();
        context.Patients.AddRange(
            new Patient
            {
                UserId = "patient-user",
                Nombre = "Ana",
                Apellido = "Pérez",
                Numero = "6641234567"
            },
            new Patient
            {
                UserId = "other-user",
                Nombre = "Luis",
                Apellido = "García",
                Numero = "6647654321"
            });
        await context.SaveChangesAsync();

        var result = await new PatientRepository(context)
            .GetByUserIdAsync("other-user");

        Assert.NotNull(result);
        Assert.Equal("Luis", result.Nombre);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsNullWhenProfileDoesNotExist()
    {
        await using var context = CreateContext();

        var result = await new PatientRepository(context)
            .GetByUserIdAsync("missing-user");

        Assert.Null(result);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}

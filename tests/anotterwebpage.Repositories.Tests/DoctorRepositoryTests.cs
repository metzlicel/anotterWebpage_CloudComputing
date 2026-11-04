using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace anotterwebpage.Repositories.Tests;

public class DoctorRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ReturnsMatchingDoctor()
    {
        await using var context = CreateContext();
        var doctor = new Doctor
        {
            UserId = "doctor-user",
            Email = "doctora@example.com"
        };
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();

        var result = await new DoctorRepository(context).GetByIdAsync(doctor.Id);

        Assert.NotNull(result);
        Assert.Equal("doctora@example.com", result.Email);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsMatchingDoctor()
    {
        await using var context = CreateContext();
        context.Doctors.Add(new Doctor
        {
            UserId = "doctor-user",
            Email = "doctora@example.com"
        });
        await context.SaveChangesAsync();

        var result = await new DoctorRepository(context)
            .GetByUserIdAsync("doctor-user");

        Assert.NotNull(result);
        Assert.Equal("doctora@example.com", result.Email);
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsTheOnlyConfiguredDoctor()
    {
        await using var context = CreateContext();
        context.Doctors.Add(new Doctor
        {
            UserId = "doctor-user",
            Email = "doctora@example.com"
        });
        await context.SaveChangesAsync();

        var result = await new DoctorRepository(context).GetActiveAsync();

        Assert.NotNull(result);
        Assert.Equal("doctor-user", result.UserId);
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsNullWithoutConfiguredDoctor()
    {
        await using var context = CreateContext();

        var result = await new DoctorRepository(context).GetActiveAsync();

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

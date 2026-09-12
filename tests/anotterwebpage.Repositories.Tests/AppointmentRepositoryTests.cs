using Microsoft.EntityFrameworkCore;
using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Repositories;

namespace anotterwebpage.Repositories.Tests;

public class AppointmentRepositoryTests
{
    private ApplicationDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        return new ApplicationDbContext(options);
    }
    
    private static Appointment CreateAppointment(
        int id,
        string nombre)
    {
        return new Appointment
        {
            Id = id,

            Start = new DateTime(
                2026, 10, 15, 17, 0, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 15, 18, 0, 0,
                DateTimeKind.Utc),

            Nombre = nombre,
            Apellido = "Lopez",
            Email = $"{nombre.ToLower()}@example.com",
            Numero = "6641234567",
            Motivo = "Consulta",
            Modalidad = Modalidad.Online,
            IsReserved = true,
            UserId = "user-123"
        };
    }
    
    // CreateAsync: Generates ID, saves via repo
    [Fact]
    public async Task CreateAsync_AddsAppointmentToDatabase()
    {
        // Arrange
        await using var context = CreateContext();

        var repository =
            new AppointmentRepository(context);

        var appointment = new Appointment
        {
            Start = new DateTime(
                2026, 10, 15, 17, 0, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 15, 18, 0, 0,
                DateTimeKind.Utc),

            Nombre = "Metzli",
            Apellido = "Lopez",
            Email = "metzli@example.com",
            Numero = "6641234567",
            Motivo = "Consulta",
            Modalidad = Modalidad.Online,
            IsReserved = true,
            UserId = "user-123"
        };

        // Act
        var result =
            await repository.CreateAsync(appointment);

        // Assert
        Assert.NotNull(result);

        var saved =
            await context.Appointments
                .FirstOrDefaultAsync();

        Assert.NotNull(saved);
        Assert.Equal("Metzli", saved.Nombre);
        Assert.Equal("metzli@example.com", saved.Email);
    }
    
    // GetAllAsync: Returns empty list
    [Fact]
    public async Task GetAllAsync_WhenDatabaseIsEmpty_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();

        var repository =
            new AppointmentRepository(context);

        // Act
        var result =
            await repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    // GetAllAsync: Returns multiple with correct mapping 
    [Fact]
    public async Task GetAllAsync_WhenAppointmentsExist_ReturnsAll()
    {
        // Arrange
        await using var context = CreateContext();

        context.Appointments.AddRange(
            CreateAppointment(1, "Metzli"),
            CreateAppointment(2, "Ana"));

        await context.SaveChangesAsync();

        var repository =
            new AppointmentRepository(context);

        // Act
        var result =
            await repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }
    
    // GetByIdAsync: Returns DTO when found
    [Fact]
    public async Task GetByIdAsync_WhenAppointmentExists_ReturnsAppointment()
    {
        // Arrange
        await using var context = CreateContext();

        context.Appointments.Add(
            CreateAppointment(1, "Metzli"));

        await context.SaveChangesAsync();

        var repository =
            new AppointmentRepository(context);

        // Act
        var result =
            await repository.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Metzli", result.Nombre);
    }
    
    // GetByIdAsync: null when not found
    [Fact]
    public async Task GetByIdAsync_WhenAppointmentDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();

        var repository =
            new AppointmentRepository(context);

        // Act
        var result =
            await repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }
    
    // Overlaps
    [Fact]
    public async Task HasOverlapAsync_WhenAppointmentOverlaps_ReturnsTrue()
    {
        // Arrange
        await using var context = CreateContext();

        context.Appointments.Add(
            CreateAppointment(1, "Metzli"));

        await context.SaveChangesAsync();

        var repository =
            new AppointmentRepository(context);

        var start = new DateTime(
            2026, 10, 15, 17, 30, 0,
            DateTimeKind.Utc);

        var end = new DateTime(
            2026, 10, 15, 18, 30, 0,
            DateTimeKind.Utc);

        // Act
        var result =
            await repository.HasOverlapAsync(
                start,
                end);

        // Assert
        Assert.True(result);
    }
    
    // No overlaps
    [Fact]
    public async Task HasOverlapAsync_WhenNoAppointmentOverlaps_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();

        context.Appointments.Add(
            CreateAppointment(1, "Metzli"));

        await context.SaveChangesAsync();

        var repository =
            new AppointmentRepository(context);

        var start = new DateTime(
            2026, 10, 15, 19, 0, 0,
            DateTimeKind.Utc);

        var end = new DateTime(
            2026, 10, 15, 20, 0, 0,
            DateTimeKind.Utc);

        // Act
        var result =
            await repository.HasOverlapAsync(
                start,
                end);

        // Assert
        Assert.False(result);
    }
    
    // UpdateAsync: When same appointment exists
    [Fact]
    public async Task HasOverlapAsync_WhenOnlySameAppointmentExists_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();

        var appointment =
            CreateAppointment(1, "Metzli");

        context.Appointments.Add(appointment);

        await context.SaveChangesAsync();

        var repository =
            new AppointmentRepository(context);

        // Act
        var result =
            await repository.HasOverlapAsync(
                appointment.Start,
                appointment.End,
                1);

        // Assert
        Assert.False(result);
    }
    
    // UpdateAsync:  Updates all fields
    [Fact]
    public async Task UpdateAsync_UpdatesAppointmentInDatabase()
    {
        // Arrange
        await using var context = CreateContext();

        var appointment =
            CreateAppointment(1, "Metzli");

        context.Appointments.Add(appointment);

        await context.SaveChangesAsync();

        var repository =
            new AppointmentRepository(context);

        appointment.Nombre = "Ana";
        appointment.Email = "ana@example.com";
        appointment.Motivo = "Seguimiento";

        // Act
        var result =
            await repository.UpdateAsync(
                appointment);

        // Assert
        Assert.Equal("Ana", result.Nombre);

        var saved =
            await context.Appointments
                .FirstAsync(a => a.Id == 1);

        Assert.Equal("Ana", saved.Nombre);
        Assert.Equal("ana@example.com", saved.Email);
        Assert.Equal("Seguimiento", saved.Motivo);
    }
    
    // DeleteAsync: Deletes entity
    [Fact]
    public async Task DeleteAsync_RemovesAppointmentFromDatabase()
    {
        // Arrange
        await using var context = CreateContext();

        var appointment =
            CreateAppointment(1, "Metzli");

        context.Appointments.Add(appointment);

        await context.SaveChangesAsync();

        var repository =
            new AppointmentRepository(context);

        // Act
        await repository.DeleteAsync(
            appointment);

        // Assert
        var exists =
            await context.Appointments
                .AnyAsync(a => a.Id == 1);

        Assert.False(exists);
    }
}
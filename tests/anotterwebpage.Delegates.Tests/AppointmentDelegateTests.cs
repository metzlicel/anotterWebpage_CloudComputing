using Moq;
using anotterwebpage_WebApi.Delegate;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Repositories;
using anotterwebpage_WebApi.Services;
using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage.Delegates.Tests.Builders;

namespace anotterwebpage.Delegates.Tests;

public class AppointmentDelegateTests
{
    private readonly Mock<IAppointmentRepository> _repoMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly IAppointmentDelegate _delegate;

    public AppointmentDelegateTests()
    {
        _repoMock = new Mock<IAppointmentRepository>();
        _emailMock = new Mock<IEmailService>();

        _delegate = new AppointmentDelegate(
            _repoMock.Object,
            _emailMock.Object);
    }

    //GetAllAsync: Returns empty list
    [Fact]
    public async Task GetAllAsync_WhenNoAppointments_ReturnsEmptyList()
    {
        // Arrange
        _repoMock
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<Appointment>());

        // Act
        var result = await _delegate.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _repoMock.Verify(
            repo => repo.GetAllAsync(),
            Times.Once);
    }
    
    //GetAllAsync: returns multiple with correct mapping
    [Fact]
    public async Task GetAllAsync_WhenAppointmentsExist_ReturnsAllAppointments()
    {
        // Arrange
        var appointments = new List<Appointment>
        {
            new AppointmentBuilder()
                .WithId(1)
                .WithNombre("Metzli")
                .Build(),

            new AppointmentBuilder()
                .WithId(2)
                .WithNombre("Ana")
                .Build()
        };

        _repoMock
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(appointments);

        // Act
        var result = await _delegate.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        Assert.Equal(1, result[0].Id);
        Assert.Equal("Metzli", result[0].Nombre);

        Assert.Equal(2, result[1].Id);
        Assert.Equal("Ana", result[1].Nombre);

        _repoMock.Verify(
            repo => repo.GetAllAsync(),
            Times.Once);
    }
    
     //GetBusy: Returns DTO when found
     // quitar hardcode fecha DateTime.now + 3 dias
     [Fact]
     public async Task GetBusyAsync_ReturnsAppointmentsFromRepository()
     {
         var start = new DateTime(
             2026, 10, 20, 8, 0, 0,
             DateTimeKind.Utc);

         var end = new DateTime(
             2026, 10, 20, 20, 0, 0,
             DateTimeKind.Utc);

         var appointments = new List<Appointment>
         {
             new AppointmentBuilder()
                 .WithId(1)
                 .Build(),

             new AppointmentBuilder()
                 .WithId(2)
                 .Build()
         };

         _repoMock
             .Setup(r => r.GetBusyAsync(
                 start,
                 end))
             .ReturnsAsync(appointments);

         var result =
             await _delegate.GetBusyAsync(
                 start,
                 end);

         Assert.Equal(2, result.Count);

         _repoMock.Verify(
             r => r.GetBusyAsync(
                 start,
                 end),
             Times.Once);
     }
    
    //GetBusyAsync: Send Emails
    [Fact]
    public async Task CreateAppointment_WithPatientName_SendsEmails()
    {
        // Arrange
        var request = new CreateAppointmentDto
        {
            Start = new DateTime(
                2026, 10, 20, 17, 0, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 20, 18, 0, 0,
                DateTimeKind.Utc),

            Nombre = "Metzli",
            Apellido = "Lopez",
            NombrePaciente = "Ana",
            Email = "metzli@example.com",
            Numero = "6641234567",
            Motivo = "Consulta",
            Modalidad = Modalidad.Online
        };

        _repoMock
            .Setup(r => r.HasOverlapAsync(
                request.Start,
                request.End,
                It.IsAny<int>()))
            .ReturnsAsync(false);

        // Act
        await _delegate.BookAsync(
            request,
            null);

        // Assert
        _emailMock.Verify(
            e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(
                    body => body.Contains("Ana"))),
            Times.AtLeastOnce);
    }
    
    // GetBusyAsync: Send Email, name null
    [Fact]
    public async Task CreateAppointment_WithoutPatientName_SendsEmailsUsingDash()
    {
        // Arrange
        var request = new CreateAppointmentDto
        {
            Start = new DateTime(
                2026, 10, 21, 17, 0, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 21, 18, 0, 0,
                DateTimeKind.Utc),

            Nombre = "Metzli",
            Apellido = "Lopez",
            NombrePaciente = null,
            Email = "metzli@example.com",
            Numero = "6641234567",
            Motivo = "Consulta",
            Modalidad = Modalidad.Online
        };

        _repoMock
            .Setup(r => r.HasOverlapAsync(
                request.Start,
                request.End,
                It.IsAny<int>()))
            .ReturnsAsync(false);

        // Act
        await _delegate.BookAsync(
            request,
            null);

        // Assert
        _emailMock.Verify(
            e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(
                    body =>
                        body.Contains("Paciente") &&
                        body.Contains("—"))),
            Times.AtLeastOnce);
    }
    
    
    // GetByIdAsync: Returns DTO when found
    [Fact]
    public async Task GetAppointmentAsync_WhenAppointmentExistsAndBelongsToUser_ReturnsAppointment()
    {
        // Arrange
        var appointment = new AppointmentBuilder()
            .WithId(1)
            .WithUserId("user-123")
            .Build();

        _repoMock
            .Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(appointment);

        // Act
        var result = await _delegate.GetAppointmentAsync(
            1,
            "user-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("user-123", result.UserId);

        _repoMock.Verify(
            repo => repo.GetByIdAsync(1),
            Times.Once);
    }
    
    // GetByIdAsync: Returns null when not found
    [Fact]
    public async Task GetAppointmentAsync_WhenAppointmentDoesNotExist_ReturnsNull()
    {
        // Arrange
        _repoMock
            .Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Appointment?)null);

        // Act
        var result = await _delegate.GetAppointmentAsync(
            999,
            "user-123");

        // Assert
        Assert.Null(result);

        _repoMock.Verify(
            repo => repo.GetByIdAsync(999),
            Times.Once);
    }
    
    // GetByIdAsync: Returns null when different user
    [Fact]
    public async Task GetAppointmentAsync_WhenAppointmentBelongsToDifferentUser_ReturnsNull()
    {
        // Arrange
        var appointment = new AppointmentBuilder()
            .WithId(1)
            .WithUserId("user-other")
            .Build();

        _repoMock
            .Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(appointment);

        // Act
        var result = await _delegate.GetAppointmentAsync(
            1,
            "user-123");

        // Assert
        Assert.Null(result);

        _repoMock.Verify(
            repo => repo.GetByIdAsync(1),
            Times.Once);
    }
    
    // BookAsync: Generates ID, saves via repo
    [Fact]
    public async Task BookAsync_WhenSlotIsAvailable_CreatesAppointment()
    {
        var request = new CreateAppointmentDto
        {
            Start = new DateTime(
                2026, 10, 20, 17, 0, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 20, 18, 0, 0,
                DateTimeKind.Utc),

            Nombre = "Metzli",
            Apellido = "Lopez",
            NombrePaciente = "Ana",
            Email = "metzli@example.com",
            Numero = "6641234567",
            Motivo = "Consulta",
            Modalidad = Modalidad.Online
        };

        _repoMock
            .Setup(r => r.HasOverlapAsync(
                request.Start,
                request.End))
            .ReturnsAsync(false);

        _repoMock
            .Setup(r => r.CreateAsync(
                It.IsAny<Appointment>()))
            .ReturnsAsync(
                (Appointment appointment) => appointment);

        var result =
            await _delegate.BookAsync(
                request,
                "user-123");

        Assert.NotNull(result);
        Assert.Equal("Metzli", result.Nombre);

        _repoMock.Verify(
            r => r.CreateAsync(
                It.IsAny<Appointment>()),
            Times.Once);
    }
    
    // BookAsync: validates unique constraints
    [Fact]
    public async Task BookAsync_WhenSlotOverlaps_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateAppointmentDto
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
            Modalidad = Modalidad.Online
        };

        _repoMock
            .Setup(repo => repo.HasOverlapAsync(
                request.Start,
                request.End))
            .ReturnsAsync(true);

        // Act
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _delegate.BookAsync(
                    request,
                    "user-123"));

        // Assert
        Assert.Equal(
            "Ese horario ya está reservado.",
            exception.Message);

        _repoMock.Verify(
            repo => repo.CreateAsync(
                It.IsAny<Appointment>()),
            Times.Never);

        _emailMock.Verify(
            email => email.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }
    
    // BookAsync: Patient name and email
    [Fact]
    public async Task BookAsync_WithPatientName_SendsEmails()
    {
        var request = new CreateAppointmentDto
        {
            Start = new DateTime(
                2026, 10, 21, 17, 0, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 21, 18, 0, 0,
                DateTimeKind.Utc),

            Nombre = "Metzli",
            Apellido = "Lopez",
            NombrePaciente = "Ana",
            Email = "metzli@example.com",
            Numero = "6641234567",
            Motivo = "Consulta",
            Modalidad = Modalidad.Online
        };

        _repoMock
            .Setup(r => r.HasOverlapAsync(
                request.Start,
                request.End))
            .ReturnsAsync(false);

        _repoMock
            .Setup(r => r.CreateAsync(
                It.IsAny<Appointment>()))
            .ReturnsAsync(
                (Appointment appointment) => appointment);

        var result = await _delegate.BookAsync(
            request,
            "user-123");

        _emailMock.Verify(
            e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(
                    body => body.Contains("Ana"))),
            Times.AtLeastOnce);
    }
    
    //BookAsync: Sends email
    [Fact]
    public async Task BookAsync_WithoutPatientName_SendsEmailsUsingDash()
    {
        var request = new CreateAppointmentDto
        {
            Start = new DateTime(
                2026, 10, 22, 17, 0, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 22, 18, 0, 0,
                DateTimeKind.Utc),

            Nombre = "Metzli",
            Apellido = "Lopez",
            NombrePaciente = null,
            Email = "metzli@example.com",
            Numero = "6641234567",
            Motivo = "Consulta",
            Modalidad = Modalidad.Online
        };

        _repoMock
            .Setup(r => r.HasOverlapAsync(
                request.Start,
                request.End))
            .ReturnsAsync(false);

        _repoMock
            .Setup(r => r.CreateAsync(
                It.IsAny<Appointment>()))
            .ReturnsAsync(
                (Appointment appointment) => appointment);

        await _delegate.BookAsync(
            request,
            "user-123");

        _emailMock.Verify(
            e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(
                    body =>
                        body.Contains("Paciente") &&
                        body.Contains("—"))),
            Times.AtLeastOnce);
    }
    
    // UpdateAsync: Updates all fields
    [Fact]
    public async Task UpdateAsync_WhenAppointmentExistsAndSlotIsAvailable_UpdatesAllFields()
    {
        // Arrange
        var appointment = new AppointmentBuilder()
            .WithId(1)
            .WithUserId("user-123")
            .Build();

        var request = new UpdateAppointmentDto
        {
            Start = new DateTime(
                2026, 10, 20, 17, 0, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 20, 18, 0, 0,
                DateTimeKind.Utc),

            Nombre = "Ana",
            Apellido = "Perez",
            NombrePaciente = "Paciente actualizado",
            Email = "ana@example.com",
            Numero = "6649999999",
            Motivo = "Seguimiento",
            Modalidad = Modalidad.Presencial
        };

        _repoMock
            .Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(appointment);

        _repoMock
            .Setup(repo => repo.HasOverlapAsync(
                request.Start,
                request.End,
                1))
            .ReturnsAsync(false);

        _repoMock
            .Setup(repo => repo.UpdateAsync(
                It.IsAny<Appointment>()))
            .ReturnsAsync((Appointment a) => a);

        // Act
        var result = await _delegate.UpdateAsync(
            1,
            request,
            "user-123");

        // Assert
        Assert.NotNull(result);

        Assert.Equal("Ana", result.Nombre);
        Assert.Equal("Perez", result.Apellido);
        Assert.Equal("Paciente actualizado", result.NombrePaciente);
        Assert.Equal("ana@example.com", result.Email);
        Assert.Equal("6649999999", result.Numero);
        Assert.Equal("Seguimiento", result.Motivo);
        Assert.Equal(Modalidad.Presencial, result.Modalidad);
        Assert.Equal(request.Start, result.Start);
        Assert.Equal(request.End, result.End);

        _repoMock.Verify(
            repo => repo.UpdateAsync(
                It.Is<Appointment>(a =>
                    a.Id == 1 &&
                    a.Nombre == "Ana" &&
                    a.Apellido == "Perez")),
            Times.Once);
    }
    
    // UpdateAsync: throws if not found 
    [Fact]
    public async Task UpdateAsync_WhenAppointmentDoesNotExist_ReturnsNull()
    {
        // Arrange
        var request = new UpdateAppointmentDto
        {
            Start = new DateTime(
                2026, 10, 20, 17, 0, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 20, 18, 0, 0,
                DateTimeKind.Utc),

            Nombre = "Ana",
            Apellido = "Perez",
            Email = "ana@example.com",
            Numero = "6649999999",
            Motivo = "Seguimiento",
            Modalidad = Modalidad.Online
        };

        _repoMock
            .Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Appointment?)null);

        // Act
        var result = await _delegate.UpdateAsync(
            999,
            request,
            "user-123");

        // Assert
        Assert.Null(result);

        _repoMock.Verify(
            repo => repo.UpdateAsync(
                It.IsAny<Appointment>()),
            Times.Never);
    }
    
    // UpdateAsync: Returns null different user
    [Fact]
    public async Task UpdateAsync_WhenAppointmentBelongsToDifferentUser_ReturnsNull()
    {
        // Arrange
        var appointment = new AppointmentBuilder()
            .WithId(1)
            .WithUserId("other-user")
            .Build();

        var request = new UpdateAppointmentDto
        {
            Start = new DateTime(
                2026, 10, 20, 17, 0, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 20, 18, 0, 0,
                DateTimeKind.Utc),

            Nombre = "Ana",
            Apellido = "Perez",
            Email = "ana@example.com",
            Numero = "6649999999",
            Motivo = "Seguimiento",
            Modalidad = Modalidad.Online
        };

        _repoMock
            .Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(appointment);

        // Act
        var result = await _delegate.UpdateAsync(
            1,
            request,
            "user-123");

        // Assert
        Assert.Null(result);

        _repoMock.Verify(
            repo => repo.UpdateAsync(
                It.IsAny<Appointment>()),
            Times.Never);
    }
    
    // UpdateAsync: throws if overlaps
    [Fact]
    public async Task UpdateAsync_WhenSlotOverlaps_ThrowsInvalidOperationException()
    {
        // Arrange
        var appointment = new AppointmentBuilder()
            .WithId(1)
            .WithUserId("user-123")
            .Build();

        var request = new UpdateAppointmentDto
        {
            Start = new DateTime(
                2026, 10, 20, 17, 0, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 20, 18, 0, 0,
                DateTimeKind.Utc),

            Nombre = "Ana",
            Apellido = "Perez",
            Email = "ana@example.com",
            Numero = "6649999999",
            Motivo = "Seguimiento",
            Modalidad = Modalidad.Online
        };

        _repoMock
            .Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(appointment);

        _repoMock
            .Setup(repo => repo.HasOverlapAsync(
                request.Start,
                request.End,
                1))
            .ReturnsAsync(true);

        // Act
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _delegate.UpdateAsync(
                    1,
                    request,
                    "user-123"));

        // Assert
        Assert.Equal(
            "Ese horario ya está reservado.",
            exception.Message);

        _repoMock.Verify(
            repo => repo.UpdateAsync(
                It.IsAny<Appointment>()),
            Times.Never);
    }
    
    // DeleteAsync: Deletes entity
    [Fact]
    public async Task DeleteAsync_WhenAppointmentExistsAndBelongsToUser_DeletesAppointment()
    {
        // Arrange
        var appointment = new AppointmentBuilder()
            .WithId(1)
            .WithUserId("user-123")
            .Build();

        _repoMock
            .Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(appointment);

        _repoMock
            .Setup(repo => repo.DeleteAsync(appointment))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _delegate.DeleteAsync(
            1,
            "user-123");

        // Assert
        Assert.True(result);

        _repoMock.Verify(
            repo => repo.GetByIdAsync(1),
            Times.Once);

        _repoMock.Verify(
            repo => repo.DeleteAsync(appointment),
            Times.Once);
    }
    
    // DeleteAsync: throws if not found
    [Fact]
    public async Task DeleteAsync_WhenAppointmentDoesNotExist_ReturnsFalse()
    {
        // Arrange
        _repoMock
            .Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Appointment?)null);

        // Act
        var result = await _delegate.DeleteAsync(
            999,
            "user-123");

        // Assert
        Assert.False(result);

        _repoMock.Verify(
            repo => repo.GetByIdAsync(999),
            Times.Once);

        _repoMock.Verify(
            repo => repo.DeleteAsync(
                It.IsAny<Appointment>()),
            Times.Never);
    }
    
    // DeleteAsync: handles referential integrity (different user)
    [Fact]
    public async Task DeleteAsync_WhenAppointmentBelongsToDifferentUser_ReturnsFalse()
    {
        // Arrange
        var appointment = new AppointmentBuilder()
            .WithId(1)
            .WithUserId("other-user")
            .Build();

        _repoMock
            .Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(appointment);

        // Act
        var result = await _delegate.DeleteAsync(
            1,
            "user-123");

        // Assert
        Assert.False(result);

        _repoMock.Verify(
            repo => repo.GetByIdAsync(1),
            Times.Once);

        _repoMock.Verify(
            repo => repo.DeleteAsync(
                It.IsAny<Appointment>()),
            Times.Never);
    }
}

using Moq;
using anotterwebpage_WebApi.Delegate;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Repositories;
using anotterwebpage_WebApi.Services;
using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage.Delegates.Tests.Builders;

namespace anotterwebpage.Delegates.Tests;

public class CollabDelegateTests
{
    private readonly Mock<ICollabRepository> _repoMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly ICollabDelegate _delegate;

    public CollabDelegateTests()
    {
        _repoMock = new Mock<ICollabRepository>();
        _emailMock = new Mock<IEmailService>();

        _delegate = new CollabDelegate(
            _repoMock.Object,
            _emailMock.Object);
    }
    
    // GetAllAsync: Returns empty list
    [Fact]
    public async Task GetAllAsync_WhenNoCollaborations_ReturnsEmptyList()
    {
        // Arrange
        _repoMock
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<Collab>());

        // Act
        var result = await _delegate.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _repoMock.Verify(
            repo => repo.GetAllAsync(),
            Times.Once);
    }
    
    // GetAllAsync: returns multiple with correct mapping
    [Fact]
    public async Task GetAllAsync_WhenCollaborationsExist_ReturnsAll()
    {
        // Arrange
        var collaborations = new List<Collab>
        {
            new CollabBuilder()
                .WithId(1)
                .WithNombre("Metzli")
                .Build(),

            new CollabBuilder()
                .WithId(2)
                .WithNombre("Ana")
                .Build()
        };

        _repoMock
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(collaborations);

        // Act
        var result = await _delegate.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(1, result[0].Id);
        Assert.Equal("Metzli", result[0].Nombre);

        Assert.Equal(2, result[1].Id);
        Assert.Equal("Ana", result[1].Nombre);
    }
    
    // GetByIdAsync: Returns DTO when found
    [Fact]
    public async Task GetByIdAsync_WhenCollaborationExists_ReturnsCollaboration()
    {
        // Arrange
        var collab = new CollabBuilder()
            .WithId(1)
            .Build();

        _repoMock
            .Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(collab);

        // Act
        var result = await _delegate.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Metzli", result.Nombre);

        _repoMock.Verify(
            repo => repo.GetByIdAsync(1),
            Times.Once);
    }
    
    // GetByIdAsync: null when not found
    [Fact]
    public async Task GetByIdAsync_WhenCollaborationDoesNotExist_ReturnsNull()
    {
        // Arrange
        _repoMock
            .Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Collab?)null);

        // Act
        var result = await _delegate.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }
    
    // CreateAsync: Generates ID, saves via repo
    [Fact]
    public async Task CreateAsync_WithValidData_CreatesCollaboration()
    {
        // Arrange
        var request = new CreateCollabDto
        {
            Nombre = "Metzli",
            Apellido = "Lopez",
            NombreOrg = "CETYS",
            Email = "metzli@example.com",
            Numero = "6641234567",
            Motivo = "Nueva colaboración"
        };

        _repoMock
            .Setup(repo => repo.CreateAsync(It.IsAny<Collab>()))
            .ReturnsAsync((Collab collab) =>
            {
                collab.Id = 10;
                return collab;
            });

        // Act
        var result = await _delegate.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal("Metzli", result.Nombre);
        Assert.Equal("metzli@example.com", result.Email);

        _repoMock.Verify(
            repo => repo.CreateAsync(
                It.Is<Collab>(c =>
                    c.Nombre == "Metzli" &&
                    c.Apellido == "Lopez" &&
                    c.Email == "metzli@example.com")),
            Times.Once);
    }

[Fact]
public async Task CreateAsync_ValidCollaboration_SendsEmails()
{
    var request = new CreateCollabDto
    {
        Nombre = "Metzli",
        Apellido = "Lopez",
        NombreOrg = "CETYS",
        Email = "metzli@example.com",
        Numero = "6641234567",
        Motivo = "Propuesta de colaboración"
    };

    _repoMock
        .Setup(r => r.CreateAsync(It.IsAny<Collab>()))
        .ReturnsAsync((Collab collab) => collab);

    await _delegate.CreateAsync(request);

    _emailMock.Verify(
        e => e.SendEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()),
        Times.Exactly(2));
}

    
    // UpdateAsync: Updates all fields
    [Fact]
    public async Task UpdateAsync_WhenCollaborationExists_UpdatesAllFields()
    {
        // Arrange
        var collab = new CollabBuilder()
            .WithId(1)
            .Build();

        var request = new UpdateCollabDto
        {
            Nombre = "Ana",
            Apellido = "Perez",
            NombreOrg = "Organización Nueva",
            Email = "ana@example.com",
            Numero = "6649999999",
            Motivo = "Motivo actualizado"
        };

        _repoMock
            .Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(collab);

        _repoMock
            .Setup(repo => repo.UpdateAsync(It.IsAny<Collab>()))
            .ReturnsAsync((Collab c) => c);

        // Act
        var result = await _delegate.UpdateAsync(
            1,
            request);

        // Assert
        Assert.NotNull(result);

        Assert.Equal("Ana", result.Nombre);
        Assert.Equal("Perez", result.Apellido);
        Assert.Equal("Organización Nueva", result.NombreOrg);
        Assert.Equal("ana@example.com", result.Email);
        Assert.Equal("6649999999", result.Numero);
        Assert.Equal("Motivo actualizado", result.Motivo);

        _repoMock.Verify(
            repo => repo.UpdateAsync(
                It.Is<Collab>(c =>
                    c.Id == 1 &&
                    c.Nombre == "Ana" &&
                    c.Email == "ana@example.com")),
            Times.Once);
    }
    
    // UpdateAsync: throws if not found
    [Fact]
    public async Task UpdateAsync_WhenCollaborationDoesNotExist_ReturnsNull()
    {
        // Arrange
        var request = new UpdateCollabDto
        {
            Nombre = "Ana",
            Apellido = "Perez",
            NombreOrg = "CETYS",
            Email = "ana@example.com",
            Numero = "6649999999",
            Motivo = "Prueba"
        };

        _repoMock
            .Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Collab?)null);

        // Act
        var result = await _delegate.UpdateAsync(
            999,
            request);

        // Assert
        Assert.Null(result);

        _repoMock.Verify(
            repo => repo.UpdateAsync(
                It.IsAny<Collab>()),
            Times.Never);
    }
    
    // DeleteAsync: Deletes entity
    [Fact]
    public async Task DeleteAsync_WhenCollaborationExists_DeletesCollaboration()
    {
        // Arrange
        var collab = new CollabBuilder()
            .WithId(1)
            .Build();

        _repoMock
            .Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(collab);

        _repoMock
            .Setup(repo => repo.DeleteAsync(collab))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _delegate.DeleteAsync(1);

        // Assert
        Assert.True(result);

        _repoMock.Verify(
            repo => repo.DeleteAsync(collab),
            Times.Once);
    }
    
    // DeleteAsync: throws if not found
    [Fact]
    public async Task DeleteAsync_WhenCollaborationDoesNotExist_ReturnsFalse()
    {
        // Arrange
        _repoMock
            .Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Collab?)null);

        // Act
        var result = await _delegate.DeleteAsync(999);

        // Assert
        Assert.False(result);

        _repoMock.Verify(
            repo => repo.DeleteAsync(
                It.IsAny<Collab>()),
            Times.Never);
    }
}
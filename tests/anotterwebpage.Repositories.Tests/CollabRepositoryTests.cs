using Microsoft.EntityFrameworkCore;
using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Repositories;

namespace anotterwebpage.Repositories.Tests;

public class CollabRepositoryTests
{
    private ApplicationDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        return new ApplicationDbContext(options);
    }

    private static Collab CreateCollab(
        int id,
        string nombre)
    {
        return new Collab
        {
            Id = id,
            Nombre = nombre,
            Apellido = "Lopez",
            NombreOrg = "CETYS",
            Email = $"{nombre.ToLower()}@example.com",
            Numero = "6641234567",
            Motivo = "Colaboración"
        };
    }

    // CreateAsync: Generates ID, saves via repo
    [Fact]
    public async Task CreateAsync_AddsCollaborationToDatabase()
    {
        // Arrange
        await using var context = CreateContext();

        var repository =
            new CollabRepository(context);

        var collab =
            CreateCollab(1, "Metzli");

        // Act
        var result =
            await repository.CreateAsync(collab);

        // Assert
        Assert.NotNull(result);

        var saved =
            await context.Collabs
                .FirstOrDefaultAsync();

        Assert.NotNull(saved);
        Assert.Equal("Metzli", saved.Nombre);
        Assert.Equal("metzli@example.com", saved.Email);
    }
    
    // GetAllAsync: Returns empty list
    [Fact]
    public async Task GetAllAsync_WhenDatabaseIsEmpty_ReturnsEmptyList()
    {
        await using var context = CreateContext();
        var repository = new CollabRepository(context);

        var result = await repository.GetAllAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    // GetAllAsync: returns multiple with correct mapping
    [Fact]
    public async Task GetAllAsync_WhenCollaborationsExist_ReturnsAll()
    {
        await using var context = CreateContext();

        context.Collabs.AddRange(
            CreateCollab(1, "Metzli"),
            CreateCollab(2, "Ana"));

        await context.SaveChangesAsync();

        var repository = new CollabRepository(context);

        var result = await repository.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Nombre == "Metzli");
        Assert.Contains(result, c => c.Nombre == "Ana");
    }
    
    // GetByIdAsync: Returns DTO when found
    [Fact]
    public async Task GetByIdAsync_WhenCollaborationExists_ReturnsCollaboration()
    {
        await using var context = CreateContext();

        context.Collabs.Add(CreateCollab(1, "Metzli"));
        await context.SaveChangesAsync();

        var repository = new CollabRepository(context);

        var result = await repository.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Metzli", result.Nombre);
    }
    
    // GetByIdAsync: null when not found
    [Fact]
    public async Task GetByIdAsync_WhenCollaborationDoesNotExist_ReturnsNull()
    {
        await using var context = CreateContext();
        var repository = new CollabRepository(context);

        var result = await repository.GetByIdAsync(999);

        Assert.Null(result);
    }
    
    // UpdateAsync: Updates all fields
    [Fact]
    public async Task UpdateAsync_UpdatesCollaborationInDatabase()
    {
        await using var context = CreateContext();

        var collab = CreateCollab(1, "Metzli");

        context.Collabs.Add(collab);
        await context.SaveChangesAsync();

        var repository = new CollabRepository(context);

        collab.Nombre = "Ana";
        collab.Email = "ana@example.com";
        collab.Motivo = "Motivo actualizado";

        var result = await repository.UpdateAsync(collab);

        Assert.Equal("Ana", result.Nombre);

        var saved = await context.Collabs.FirstAsync(c => c.Id == 1);

        Assert.Equal("Ana", saved.Nombre);
        Assert.Equal("ana@example.com", saved.Email);
        Assert.Equal("Motivo actualizado", saved.Motivo);
    }
    
    // DeleteAsync: Deletes entity
    [Fact]
    public async Task DeleteAsync_RemovesCollaborationFromDatabase()
    {
        await using var context = CreateContext();

        var collab = CreateCollab(1, "Metzli");

        context.Collabs.Add(collab);
        await context.SaveChangesAsync();

        var repository = new CollabRepository(context);

        await repository.DeleteAsync(collab);

        var exists = await context.Collabs.AnyAsync(c => c.Id == 1);

        Assert.False(exists);
    }
}



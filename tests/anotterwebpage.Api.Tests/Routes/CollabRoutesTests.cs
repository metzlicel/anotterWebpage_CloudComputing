using System.Net;
using System.Net.Http.Json;
using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace anotterwebpage.Api.Tests.Routes;

public class CollabRoutesTests : IClassFixture<AnotterwebpageApiTests>
{
    private readonly AnotterwebpageApiTests _factory;
    private readonly HttpClient _client;

    public CollabRoutesTests(AnotterwebpageApiTests factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // GET /items` returns 200 with empty array | No items in DB
    [Fact]
    public async Task GetCollaborations_WhenDatabaseIsEmpty_Returns200WithEmptyArray()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        var response =
            await _client.GetAsync(
                "/api/collaborations");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var collaborations =
            await response.Content
                .ReadFromJsonAsync<List<CollabDto>>();

        Assert.NotNull(collaborations);
        Assert.Empty(collaborations);
    }

    // GET /items` returns 200 with items | Multiple items exist
    [Fact]
    public async Task GetCollaborations_WhenCollaborationsExist_Returns200WithItems()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                context.Collabs.AddRange(
                    CreateCollab(1, "Metzli"),
                    CreateCollab(2, "Ana"));

                await context.SaveChangesAsync();
            });

        // Act
        var response =
            await _client.GetAsync(
                "/api/collaborations");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        var collaborations =
            await response.Content
                .ReadFromJsonAsync<List<CollabDto>>();

        Assert.NotNull(collaborations);
        Assert.Equal(2, collaborations.Count);

        Assert.Contains(
            collaborations,
            c => c.Id == 1 && c.Nombre == "Metzli");

        Assert.Contains(
            collaborations,
            c => c.Id == 2 && c.Nombre == "Ana");
    }
    
    // GET /items/{id}` returns 200 with item | Valid ID exists
    [Fact]
    public async Task GetCollaborationById_WhenCollaborationExists_Returns200()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                context.Collabs.Add(
                    CreateCollab(1, "Metzli"));

                await context.SaveChangesAsync();
            });

        // Act
        var response =
            await _client.GetAsync(
                "/api/collaborations/1");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var collaboration =
            await response.Content
                .ReadFromJsonAsync<CollabDto>();

        Assert.NotNull(collaboration);
        Assert.Equal(1, collaboration.Id);
        Assert.Equal("Metzli", collaboration.Nombre);
        Assert.Equal("Lopez", collaboration.Apellido);
        Assert.Equal("CETYS", collaboration.NombreOrg);
    }
    
    // GET /items/{id}` returns 404 | Valid ID not found
    [Fact]
    public async Task GetCollaborationById_WhenCollaborationDoesNotExist_Returns404()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        var response =
            await _client.GetAsync(
                "/api/collaborations/999");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
    
    // GET /items/{id}` returns 400 | Invalid ID format (non-alphanumeric)
    [Fact]
    public async Task GetCollaborationById_WhenIdFormatIsInvalid_Returns400()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        var response =
            await _client.GetAsync(
                "/api/collaborations/abc");

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains("\"status\":400", body);
        Assert.Contains("\"id\"", body);
    }
    
    // POST /items` returns 201 + Location | Valid CreateItemDto
    [Fact]
    public async Task PostCollaboration_WithValidRequest_Returns201WithLocation()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new CreateCollabDto
        {
            Nombre = "Metzli",
            Apellido = "Lopez",
            NombreOrg = "CETYS",
            Email = "metzli@example.com",
            Numero = "6641234567",
            Motivo = "Propuesta de colaboración"
        };

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/collaborations",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        Assert.NotNull(
            response.Headers.Location);

        Assert.StartsWith(
            "/api/collaborations/",
            response.Headers.Location!.ToString());

        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        var collaboration =
            await response.Content
                .ReadFromJsonAsync<CollabDto>();

        Assert.NotNull(collaboration);
        Assert.True(collaboration.Id > 0);
        Assert.Equal("Metzli", collaboration.Nombre);
        Assert.Equal("CETYS", collaboration.NombreOrg);
    }
    
    // POST /items` returns 400 | Missing/invalid fields, duplicate constraint violations
    [Fact]
    public async Task PostCollaboration_WithInvalidRequest_Returns400WithProblemDetails()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new CreateCollabDto
        {
            Nombre = "",
            Apellido = "",
            NombreOrg = null,
            Email = "correo-invalido",
            Numero = "",
            Motivo = ""
        };

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/collaborations",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "\"title\":\"Validation Failed\"",
            body);

        Assert.Contains(
            "\"status\":400",
            body);

        Assert.Contains(
            "\"Nombre\"",
            body);

        Assert.Contains(
            "\"Apellido\"",
            body);

        Assert.Contains(
            "\"Email\"",
            body);
    }
    
    // PUT /items/{id}` returns 200 | Valid UpdateItemDto
    [Fact]
    public async Task PutCollaboration_WithValidRequest_Returns200()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                context.Collabs.Add(
                    CreateCollab(1, "Metzli"));

                await context.SaveChangesAsync();
            });

        var request = new UpdateCollabDto
        {
            Nombre = "Ana",
            Apellido = "Perez",
            NombreOrg = "OpenAI",
            Email = "ana@example.com",
            Numero = "6649999999",
            Motivo = "Colaboración actualizada"
        };

        // Act
        var response =
            await _client.PutAsJsonAsync(
                "/api/collaborations/1",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        var collaboration =
            await response.Content
                .ReadFromJsonAsync<CollabDto>();

        Assert.NotNull(collaboration);

        Assert.Equal(1, collaboration.Id);
        Assert.Equal("Ana", collaboration.Nombre);
        Assert.Equal("Perez", collaboration.Apellido);
        Assert.Equal("OpenAI", collaboration.NombreOrg);
        Assert.Equal("ana@example.com", collaboration.Email);
        Assert.Equal("6649999999", collaboration.Numero);
        Assert.Equal(
            "Colaboración actualizada",
            collaboration.Motivo);
    }
    
    // PUT /items/{id}` returns 404 | Valid ID not found
    [Fact]
    public async Task PutCollaboration_WhenCollaborationDoesNotExist_Returns404()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new UpdateCollabDto
        {
            Nombre = "Ana",
            Apellido = "Perez",
            NombreOrg = "CETYS",
            Email = "ana@example.com",
            Numero = "6649999999",
            Motivo = "Colaboración"
        };

        // Act
        var response =
            await _client.PutAsJsonAsync(
                "/api/collaborations/999",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
    
    // PUT /items/{id}` returns 400 Invalid Request
    [Fact]
    public async Task PutCollaboration_WithInvalidRequest_Returns400()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                context.Collabs.Add(
                    CreateCollab(1, "Metzli"));

                await context.SaveChangesAsync();
            });

        var request = new UpdateCollabDto
        {
            Nombre = "",
            Apellido = "",
            NombreOrg = null,
            Email = "correo-invalido",
            Numero = "",
            Motivo = ""
        };

        // Act
        var response =
            await _client.PutAsJsonAsync(
                "/api/collaborations/1",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
    
    // PUT /items/{id}` returns 400 | Infalid Format 
    [Fact]
    public async Task PutCollaboration_WhenIdFormatIsInvalid_Returns400()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new UpdateCollabDto
        {
            Nombre = "Ana",
            Apellido = "Perez",
            NombreOrg = "CETYS",
            Email = "ana@example.com",
            Numero = "6649999999",
            Motivo = "Colaboración"
        };

        // Act
        var response =
            await _client.PutAsJsonAsync(
                "/api/collaborations/abc",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
    
    // DELETE /items/{id}` returns 204 | Valid ID exists 
    [Fact]
    public async Task DeleteCollaboration_WhenCollaborationExists_Returns204()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                context.Collabs.Add(
                    CreateCollab(1, "Metzli"));

                await context.SaveChangesAsync();
            });

        // Act
        var response =
            await _client.DeleteAsync(
                "/api/collaborations/1");

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                var exists =
                    await context.Collabs
                        .AnyAsync(c => c.Id == 1);

                Assert.False(exists);
            });
    }
    
    // DELETE /items/{id}` returns 404 | Valid ID not found
    [Fact]
    public async Task DeleteCollaboration_WhenCollaborationDoesNotExist_Returns404()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        var response =
            await _client.DeleteAsync(
                "/api/collaborations/999");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
    
    // DELETE /items/{id}` returns 400 | Invalid Format
    [Fact]
    public async Task DeleteCollaboration_WhenIdFormatIsInvalid_Returns400()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        var response =
            await _client.DeleteAsync(
                "/api/collaborations/abc");

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
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
            Motivo = "Colaboración",
            CreatedAt = new DateTime(
                2026, 10, 1, 12, 0, 0,
                DateTimeKind.Utc)
        };
    }
}
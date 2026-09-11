using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;

namespace anotterwebpage.Api.Tests.Middleware;

public class ValidationMiddlewareTests
    : IClassFixture<AnotterwebpageApiTests>
{
    private readonly AnotterwebpageApiTests _factory;
    private readonly HttpClient _client;

    public ValidationMiddlewareTests(
        AnotterwebpageApiTests factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    // Request validation | FluentValidation errors return 400 with ProblemDetails
    // Appointments
    [Fact]
    public async Task InvalidAppointmentRequest_Returns400WithProblemDetails()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new
        {
            start = "2026-10-20T18:00:00Z",
            end = "2026-10-20T17:00:00Z",
            nombre = "",
            apellido = "",
            email = "correo-invalido",
            numero = "",
            motivo = "Consulta",
            modalidad = ""
        };

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/appointments",
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
            "\"Email\"",
            body);

        Assert.Contains(
            "\"End\"",
            body);
    }
    
    // COllaborations
    [Fact]
    public async Task InvalidCollaborationRequest_Returns400WithProblemDetails()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new
        {
            nombre = "",
            apellido = "",
            nombreOrg = "CETYS",
            email = "correo-invalido",
            numero = "",
            motivo = ""
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
            "\"Email\"",
            body);
    }
    
    // Content-Type
    // Malformed Json
    [Fact]
    public async Task MalformedJson_Returns400()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var malformedJson = """
                            {
                                "nombre": "Metzli",
                                "apellido": "Lopez",
                                "email":
                            """;

        using var content =
            new StringContent(
                malformedJson,
                Encoding.UTF8,
                "application/json");

        // Act
        var response =
            await _client.PostAsync(
                "/api/collaborations",
                content);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
    
    // All endpoints require/return `application/json
    [Fact]
    public async Task MalformedJson_Returns400ProblemDetails()
    {
        await _factory.ResetDatabaseAsync();

        var malformedJson = """
                            {
                                "nombre": "Metzli",
                                "email":
                            """;

        using var content =
            new StringContent(
                malformedJson,
                Encoding.UTF8,
                "application/json");

        var response =
            await _client.PostAsync(
                "/api/collaborations",
                content);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
}
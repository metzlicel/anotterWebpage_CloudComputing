using System.Net;
using System.Net.Http.Json;
using anotterwebpage_WebApi.Api.Dtos;

namespace anotterwebpage.Api.Tests.Concurrency;

public class ConcurrencyTests
    : IClassFixture<AnotterwebpageApiTests>
{
    private readonly AnotterwebpageApiTests _factory;
    private readonly HttpClient _client;

    public ConcurrencyTests(
        AnotterwebpageApiTests factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // Concurrency | Parallel requests don't cause data corruption
    // Collaborations
    [Fact]
    public async Task ParallelCollaborationRequests_DoNotLoseData()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var requests =
            Enumerable.Range(1, 10)
                .Select(i =>
                    new CreateCollabDto
                    {
                        Nombre = $"Usuario{i}",
                        Apellido = "Prueba",
                        NombreOrg = $"Organizacion{i}",
                        Email = $"usuario{i}@example.com",
                        Numero = $"66412345{i:00}",
                        Motivo = $"Colaboracion {i}"
                    })
                .ToList();

        // Act
        var tasks =
            requests.Select(request =>
                _client.PostAsJsonAsync(
                    "/api/collaborations",
                    request));

        var responses =
            await Task.WhenAll(tasks);

        // Assert
        Assert.All(
            responses,
            response =>
                Assert.Equal(
                    HttpStatusCode.Created,
                    response.StatusCode));

        var getResponse =
            await _client.GetAsync(
                "/api/collaborations");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var collaborations =
            await getResponse.Content
                .ReadFromJsonAsync<List<CollabDto>>();

        Assert.NotNull(collaborations);
        Assert.Equal(10, collaborations.Count);
    }
    
    // Appointments
    [Fact]
    public async Task ParallelAppointmentRequests_WithDifferentSlots_DoNotLoseData()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var requests =
            Enumerable.Range(0, 5)
                .Select(i =>
                {
                    var start =
                        new DateTime(
                            2026,
                            11,
                            10,
                            10 + i,
                            0,
                            0,
                            DateTimeKind.Utc);

                    return new CreateAppointmentDto
                    {
                        Start = start,
                        End = start.AddHours(1),
                        Nombre = $"Paciente{i}",
                        Apellido = "Prueba",
                        NombrePaciente = null,
                        Email = $"paciente{i}@example.com",
                        Numero = $"66455500{i:00}",
                        Motivo = "Consulta",
                        Modalidad = "Online"
                    };
                })
                .ToList();

        // Act
        var tasks =
            requests.Select(request =>
                _client.PostAsJsonAsync(
                    "/api/appointments",
                    request));

        var responses =
            await Task.WhenAll(tasks);

        // Assert
        Assert.All(
            responses,
            response =>
                Assert.Equal(
                    HttpStatusCode.Created,
                    response.StatusCode));

        var getResponse =
            await _client.GetAsync(
                "/api/appointments");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        var appointments =
            await getResponse.Content
                .ReadFromJsonAsync<List<AppointmentDto>>();

        Assert.NotNull(appointments);
        Assert.Equal(5, appointments.Count);
    }
}
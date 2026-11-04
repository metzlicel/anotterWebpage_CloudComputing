using System.Net;
using System.Net.Http.Json;
using anotterwebpage_WebApi.Api.Dtos;

namespace anotterwebpage.Api.Tests.Concurrency;

public class ConcurrencyTests : IClassFixture<AnotterwebpageApiTests>
{
    private readonly AnotterwebpageApiTests _factory;
    private readonly HttpClient _client;

    public ConcurrencyTests(AnotterwebpageApiTests factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ParallelCollaborationRequests_DoNotLoseData()
    {
        await _factory.ResetDatabaseAsync();
        var requests = Enumerable.Range(1, 10)
            .Select(i => new CreateCollabDto
            {
                Nombre = $"Usuario{i}",
                Apellido = "Prueba",
                NombreOrg = $"Organizacion{i}",
                Email = $"usuario{i}@example.com",
                Numero = $"66412345{i:00}",
                Motivo = $"Colaboracion {i}"
            });

        var responses = await Task.WhenAll(requests.Select(request =>
            _client.PostAsJsonAsync("/api/collaborations", request)));

        Assert.All(responses, response =>
            Assert.Equal(HttpStatusCode.Created, response.StatusCode));
    }
}

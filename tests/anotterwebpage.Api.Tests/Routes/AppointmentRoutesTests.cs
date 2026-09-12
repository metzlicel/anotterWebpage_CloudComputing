using System.Net;
using Microsoft.EntityFrameworkCore;
using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Domain;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace anotterwebpage.Api.Tests.Routes;

public class AppointmentRoutesTests
    : IClassFixture<AnotterwebpageApiTests>
{
    private readonly AnotterwebpageApiTests _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
    
    public AppointmentRoutesTests(
        AnotterwebpageApiTests factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // GET /items returns 200 with empty array
    [Fact]
    public async Task GetAppointments_WhenDatabaseIsEmpty_Returns200WithEmptyArray()
    {
        await _factory.ResetDatabaseAsync();
        
        // Act
        var response =
            await _client.GetAsync(
                "/api/appointments");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var appointments =
            await response.Content
                .ReadFromJsonAsync<List<AppointmentDto>>(JsonOptions);

        Assert.NotNull(appointments);
        Assert.Empty(appointments);
    }
    
    
    // GET /items: returns 200 with items 
    [Fact]
    public async Task GetAppointments_WhenAppointmentsExist_Returns200WithItems()
    {
        await _factory.ResetDatabaseAsync();
        
        // Arrange
        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                context.Appointments.AddRange(
                    CreateAppointment(
                        1,
                        "Metzli"),

                    CreateAppointment(
                        2,
                        "Marlon"));

                await context.SaveChangesAsync();
            });

        // Act
        var response =
            await _client.GetAsync(
                "/api/appointments");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var appointments =
            await response.Content
                .ReadFromJsonAsync<List<AppointmentDto>>(JsonOptions);

        Assert.NotNull(appointments);
        Assert.Equal(2, appointments.Count);

        Assert.Contains(
            appointments,
            a =>
                a.Id == 1 &&
                a.Nombre == "Metzli");

        Assert.Contains(
            appointments,
            a =>
                a.Id == 2 &&
                a.Nombre == "Marlon");
    }
    
    // GET /items/{id}` returns 200 with item 
    [Fact]
    public async Task GetAppointmentById_WhenAppointmentExists_Returns200WithAppointment()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                context.Appointments.Add(
                    CreateAppointment(1, "Metzli"));

                await context.SaveChangesAsync();
            });

        // Act
        var response =
            await _client.GetAsync(
                "/api/appointments/1");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var appointment =
            await response.Content
                .ReadFromJsonAsync<AppointmentDto>(JsonOptions);

        Assert.NotNull(appointment);
        Assert.Equal(1, appointment.Id);
        Assert.Equal("Metzli", appointment.Nombre);
    }
    
    // GET /items/{id}` returns 404 | Valid ID not found 
    [Fact]
    public async Task GetAppointmentById_WhenAppointmentDoesNotExist_Returns404()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        var response =
            await _client.GetAsync(
                "/api/appointments/999");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
    
    // GET /items/{id}` returns 400 | Invalid ID format (non-alphanumeric) |
    [Fact]
    public async Task GetAppointmentById_WhenIdFormatIsInvalid_Returns400WithProblemDetails()
    {
        await _factory.ResetDatabaseAsync();

        var response =
            await _client.GetAsync(
                "/api/appointments/abc");

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

    // GetBusy: return 200
	    [Fact]
    public async Task GetBusyAppointments_WhenAppointmentsExist_Returns200WithBusySlots()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                context.Appointments.AddRange(
                    new Appointment
                    {
                        Id = 1,
                        Start = new DateTime(
                            2026, 10, 20, 17, 0, 0,
                            DateTimeKind.Utc),
                        End = new DateTime(
                            2026, 10, 20, 18, 0, 0,
                            DateTimeKind.Utc),
                        Nombre = "Metzli",
                        Apellido = "Lopez",
                        Email = "metzli@example.com",
                        Numero = "6641234567",
                        Motivo = "Consulta",
                        Modalidad = Modalidad.Online,
                        IsReserved = true
                    },
                    new Appointment
                    {
                        Id = 2,
                        Start = new DateTime(
                            2026, 10, 20, 19, 0, 0,
                            DateTimeKind.Utc),
                        End = new DateTime(
                            2026, 10, 20, 20, 0, 0,
                            DateTimeKind.Utc),
                        Nombre = "Ana",
                        Apellido = "Perez",
                        Email = "ana@example.com",
                        Numero = "6649999999",
                        Motivo = "Seguimiento",
                        Modalidad = Modalidad.Presencial,
                        IsReserved = true
                    });

                await context.SaveChangesAsync();
            });

        var start =
            "2026-10-20T00:00:00Z";

        var end =
            "2026-10-21T00:00:00Z";

        // Act
        var response =
            await _client.GetAsync(
                $"/api/appointments/busy?start={start}&end={end}");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains("\"id\":1", body);
        Assert.Contains("\"id\":2", body);
        Assert.Contains("\"title\":\"Reservado\"", body);
    }

    //GetBusy: No appointments returns empty array
    [Fact]
    public async Task GetBusyAppointments_WhenNoAppointmentsExist_Returns200WithEmptyArray()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        var response =
            await _client.GetAsync(
                "/api/appointments/busy" +
                "?start=2026-10-20T00:00:00Z" +
                "&end=2026-10-21T00:00:00Z");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Equal("[]", body);
    }

    // Enum as string
    [Fact]
    public async Task AppointmentResponse_SerializesModalidadAsString()
    {
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteDbContextAsync(async context =>
        {
            context.Appointments.Add(
                new Appointment
                {
                    Id = 1,
                    Start = new DateTime(
                        2026, 10, 20, 17, 0, 0,
                        DateTimeKind.Utc),

                    End = new DateTime(
                        2026, 10, 20, 18, 0, 0,
                        DateTimeKind.Utc),

                    Nombre = "Metzli",
                    Apellido = "Lopez",
                    Email = "metzli@example.com",
                    Numero = "6641234567",
                    Motivo = "Consulta",
                    Modalidad = Modalidad.Online
                });

            await context.SaveChangesAsync();
        });

        var response =
            await _client.GetAsync(
                "/api/appointments/1");

        var body =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "\"modalidad\":\"Online\"",
            body);
    }

    
    // POST /items` returns 201 + Location | Valid CreateItemDto 
    [Fact]
    public async Task PostAppointment_WithValidRequest_Returns201WithLocation()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

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
            NombrePaciente = null,
            Email = "metzli@example.com",
            Numero = "6641234567",
            Motivo = "Consulta",
            Modalidad = Modalidad.Online
        };

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/appointments",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        Assert.NotNull(response.Headers.Location);

        Assert.StartsWith(
            "/api/appointments/",
            response.Headers.Location!.ToString());

        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        var appointment =
            await response.Content
                .ReadFromJsonAsync<AppointmentDto>(JsonOptions);

        Assert.NotNull(appointment);
        Assert.True(appointment.Id > 0);
        Assert.Equal("Metzli", appointment.Nombre);
        Assert.Equal(Modalidad.Online, appointment.Modalidad);
    }
    
    // POST /items` returns 400 | Missing/invalid fields, duplicate constraint violations 
    [Fact]
    public async Task PostAppointment_WithInvalidRequest_Returns400WithProblemDetails()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new CreateAppointmentDto
        {
            Start = new DateTime(
                2026, 10, 20, 18, 0, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 20, 17, 0, 0,
                DateTimeKind.Utc),

            Nombre = "",
            Apellido = "",
            Email = "correo-invalido",
            Numero = "",
            Motivo = "Consulta",
            Modalidad = Modalidad.Online
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
    
    // POST /items` returns 420 | Overlapping
    [Fact]
    public async Task PostAppointment_WhenSlotOverlaps_Returns422()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                context.Appointments.Add(
                    CreateAppointment(1, "Existing"));

                await context.SaveChangesAsync();
            });

        var request = new CreateAppointmentDto
        {
            Start = new DateTime(
                2026, 10, 15, 17, 30, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 15, 18, 30, 0,
                DateTimeKind.Utc),

            Nombre = "Ana",
            Apellido = "Perez",
            Email = "ana@example.com",
            Numero = "6649999999",
            Motivo = "Consulta",
            Modalidad = Modalidad.Online
        };

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/appointments",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
    
    // PUT /items/{id}` returns 200 | Valid UpdateItemDto
    [Fact]
    public async Task PutAppointment_WithValidRequest_Returns200()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                context.Appointments.Add(
                    CreateAppointment(1, "Metzli"));

                await context.SaveChangesAsync();
            });

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

        // Act
        var response =
            await _client.PutAsJsonAsync(
                "/api/appointments/1",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        var appointment =
            await response.Content
                .ReadFromJsonAsync<AppointmentDto>(JsonOptions);

        Assert.NotNull(appointment);
        Assert.Equal(1, appointment.Id);
        Assert.Equal("Ana", appointment.Nombre);
        Assert.Equal("Perez", appointment.Apellido);
        Assert.Equal("Paciente actualizado", appointment.NombrePaciente);
        Assert.Equal("ana@example.com", appointment.Email);
        Assert.Equal("6649999999", appointment.Numero);
        Assert.Equal("Seguimiento", appointment.Motivo);
        Assert.Equal(Modalidad.Presencial, appointment.Modalidad);
        Assert.Equal(request.Start, appointment.Start);
        Assert.Equal(request.End, appointment.End);
    }
    
    // PUT /items/{id}` returns 404 | Valid ID not found
    [Fact]
    public async Task PutAppointment_WhenAppointmentDoesNotExist_Returns404()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

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
            NombrePaciente = null,
            Email = "ana@example.com",
            Numero = "6649999999",
            Motivo = "Seguimiento",
            Modalidad = Modalidad.Online
        };

        // Act
        var response =
            await _client.PutAsJsonAsync(
                "/api/appointments/999",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
    
    // PUT /items/{id}` returns 400 | Invalid request
    [Fact]
    public async Task PutAppointment_WithInvalidRequest_Returns400WithProblemDetails()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                context.Appointments.Add(
                    CreateAppointment(1, "Metzli"));

                await context.SaveChangesAsync();
            });

        var request = new UpdateAppointmentDto
        {
            Start = new DateTime(
                2026, 10, 20, 18, 0, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 20, 17, 0, 0,
                DateTimeKind.Utc),

            Nombre = "",
            Apellido = "",
            NombrePaciente = null,
            Email = "correo-invalido",
            Numero = "",
            Motivo = "Prueba",
            Modalidad = Modalidad.Online
        };

        // Act
        var response =
            await _client.PutAsJsonAsync(
                "/api/appointments/1",
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
    
    // PUT /items/{id}` returns 422 | Overlapping 
    [Fact]
    public async Task PutAppointment_WhenSlotOverlaps_Returns422()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                var appointmentToUpdate =
                    CreateAppointment(1, "Metzli");

                appointmentToUpdate.Start =
                    new DateTime(
                        2026, 10, 20, 15, 0, 0,
                        DateTimeKind.Utc);

                appointmentToUpdate.End =
                    new DateTime(
                        2026, 10, 20, 16, 0, 0,
                        DateTimeKind.Utc);

                var conflictingAppointment =
                    CreateAppointment(2, "Ana");

                conflictingAppointment.Start =
                    new DateTime(
                        2026, 10, 20, 17, 0, 0,
                        DateTimeKind.Utc);

                conflictingAppointment.End =
                    new DateTime(
                        2026, 10, 20, 18, 0, 0,
                        DateTimeKind.Utc);

                context.Appointments.AddRange(
                    appointmentToUpdate,
                    conflictingAppointment);

                await context.SaveChangesAsync();
            });

        var request = new UpdateAppointmentDto
        {
            Start = new DateTime(
                2026, 10, 20, 17, 30, 0,
                DateTimeKind.Utc),

            End = new DateTime(
                2026, 10, 20, 18, 30, 0,
                DateTimeKind.Utc),

            Nombre = "Metzli",
            Apellido = "Lopez",
            NombrePaciente = null,
            Email = "metzli@example.com",
            Numero = "6641234567",
            Motivo = "Consulta actualizada",
            Modalidad = Modalidad.Online
        };

        // Act
        var response =
            await _client.PutAsJsonAsync(
                "/api/appointments/1",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
    
    // PUT /items/{id}` returns 400 | Invalid format
    [Fact]
    public async Task PutAppointment_WhenIdFormatIsInvalid_Returns400()
    {
        await _factory.ResetDatabaseAsync();

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
            Motivo = "Prueba",
            Modalidad = Modalidad.Online
        };

        var response =
            await _client.PutAsJsonAsync(
                "/api/appointments/abc",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
    
    // DELETE /items/{id}` returns 204 | Valid ID exists
    [Fact]
    public async Task DeleteAppointment_WhenAppointmentExists_Returns204()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                context.Appointments.Add(
                    CreateAppointment(1, "Metzli"));

                await context.SaveChangesAsync();
            });

        // Act
        var response =
            await _client.DeleteAsync(
                "/api/appointments/1");

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        Assert.Equal(
            0,
            response.Content.Headers.ContentLength ?? 0);

        await _factory.ExecuteDbContextAsync(
            async context =>
            {
                var exists =
                    await context.Appointments
                        .AnyAsync(a => a.Id == 1);

                Assert.False(exists);
            });
    }
    
    // DELETE /items/{id}` returns 404 | Valid ID not found
    [Fact]
    public async Task DeleteAppointment_WhenAppointmentDoesNotExist_Returns404()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act
        var response =
            await _client.DeleteAsync(
                "/api/appointments/999");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }
    
    // DELETE /items/{id}` returns 400 | Invalid format
    [Fact]
    public async Task DeleteAppointment_WhenIdFormatIsInvalid_Returns400()
    {
        await _factory.ResetDatabaseAsync();

        var response =
            await _client.DeleteAsync(
                "/api/appointments/abc");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
    
    // Helper
    private static Appointment CreateAppointment(
        int id,
        string nombre,
        string? userId = null)
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
            NombrePaciente = null,
            Email = $"{nombre.ToLower()}@example.com",
            Numero = "6641234567",
            Motivo = "Consulta",
            Modalidad = Modalidad.Online,
            IsReserved = true,
            UserId = userId
        };
    }
}



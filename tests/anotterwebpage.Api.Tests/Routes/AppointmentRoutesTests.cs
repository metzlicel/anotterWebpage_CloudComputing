using System.Net;
using System.Net.Http.Json;
using anotterwebpage.Api.Tests.Fakes;
using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Services;
using Microsoft.EntityFrameworkCore;

namespace anotterwebpage.Api.Tests.Routes;

public class AppointmentRoutesTests : IClassFixture<AnotterwebpageApiTests>
{
    private readonly AnotterwebpageApiTests _factory;

    public AppointmentRoutesTests(AnotterwebpageApiTests factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AppointmentRoutes_RequireAuthentication()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();
        var start = DateTime.UtcNow.AddDays(3);

        var busyResponse = await client.GetAsync(
            $"/api/appointments/busy?start={start:O}&end={start.AddHours(1):O}");
        var bookingResponse = await client.PostAsJsonAsync(
            "/api/appointments",
            CreateRequest(start));

        Assert.Equal(HttpStatusCode.Unauthorized, busyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, bookingResponse.StatusCode);
    }

    [Fact]
    public async Task Patient_CannotListAllAppointments()
    {
        await _factory.ResetDatabaseAsync();
        using var client = CreateClient("patient-a", IdentityRoles.Patient);

        var response = await client.GetAsync("/api/appointments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Patient_CanReadOwnAppointmentButNotAnotherPatients()
    {
        var start = DateTime.UtcNow.AddDays(3);
        var appointmentId = await SeedAppointmentAsync(start);
        using var ownerClient = CreateClient("patient-a", IdentityRoles.Patient);
        using var otherPatientClient = CreateClient("patient-b", IdentityRoles.Patient);

        var ownResponse = await ownerClient.GetAsync($"/api/appointments/{appointmentId}");
        var otherResponse = await otherPatientClient.GetAsync($"/api/appointments/{appointmentId}");

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
    }

    [Fact]
    public async Task Patient_CannotModifyAnotherPatientsAppointment()
    {
        var start = DateTime.UtcNow.AddDays(3);
        var appointmentId = await SeedAppointmentAsync(start);
        using var client = CreateClient("patient-b", IdentityRoles.Patient);

        var response = await client.PutAsJsonAsync(
            $"/api/appointments/{appointmentId}",
            CreateUpdateRequest(start.AddDays(1)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Doctor_CanListAndModifyAnyAppointment()
    {
        var start = DateTime.UtcNow.AddDays(3);
        var appointmentId = await SeedAppointmentAsync(start);
        using var client = CreateClient("doctor-user", IdentityRoles.Doctor);

        var listResponse = await client.GetAsync("/api/appointments");
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/appointments/{appointmentId}",
            CreateUpdateRequest(start.AddDays(1)));

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
    }

    [Fact]
    public async Task BusyAppointments_ExposeNoPatientData()
    {
        var start = DateTime.UtcNow.AddDays(3);
        await SeedAppointmentAsync(start);
        using var client = CreateClient("patient-a", IdentityRoles.Patient);

        var response = await client.GetAsync(
            $"/api/appointments/busy?start={start:O}&end={start.AddHours(1):O}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("isMine", content);
        Assert.DoesNotContain("Ana", content);
        Assert.DoesNotContain("6641234567", content);
    }

    [Fact]
    public async Task Patient_CanBookForOwnProfile()
    {
        await SeedProfilesAsync();
        using var client = CreateClient("patient-a", IdentityRoles.Patient);
        var start = DateTime.UtcNow.AddDays(3);

        var response = await client.PostAsJsonAsync(
            "/api/appointments",
            CreateRequest(start));

        var notificationCount = 0;
        await _factory.ExecuteDbContextAsync(async context =>
        {
            notificationCount = await context.AppointmentNotifications.CountAsync();
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, notificationCount);
    }

    [Fact]
    public async Task SecondCancellation_ReturnsConflict()
    {
        var start = DateTime.UtcNow.AddDays(3);
        var appointmentId = await SeedAppointmentAsync(start);
        using var client = CreateClient("doctor-user", IdentityRoles.Doctor);

        var firstResponse = await client.DeleteAsync($"/api/appointments/{appointmentId}");
        var secondResponse = await client.DeleteAsync($"/api/appointments/{appointmentId}");

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Doctor_CannotBookAnAppointmentForAPatient()
    {
        await _factory.ResetDatabaseAsync();
        using var client = CreateClient("doctor-user", IdentityRoles.Doctor);
        var start = DateTime.UtcNow.AddDays(3);

        var response = await client.PostAsJsonAsync(
            "/api/appointments",
            CreateRequest(start));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Patient_CannotCancelAnotherPatientsAppointment()
    {
        var start = DateTime.UtcNow.AddDays(3);
        var appointmentId = await SeedAppointmentAsync(start);
        using var client = CreateClient("patient-b", IdentityRoles.Patient);

        var response = await client.DeleteAsync($"/api/appointments/{appointmentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelledAppointment_NoLongerAppearsAsBusy()
    {
        var start = DateTime.UtcNow.AddDays(3);
        var appointmentId = await SeedAppointmentAsync(start);
        using var doctorClient = CreateClient("doctor-user", IdentityRoles.Doctor);
        using var patientClient = CreateClient("patient-a", IdentityRoles.Patient);

        var cancellationResponse = await doctorClient.DeleteAsync(
            $"/api/appointments/{appointmentId}");
        var busyResponse = await patientClient.GetAsync(
            $"/api/appointments/busy?start={start:O}&end={start.AddHours(1):O}");
        var busyAppointments = await busyResponse.Content
            .ReadFromJsonAsync<List<BusyAppointmentDto>>();

        Assert.Equal(HttpStatusCode.NoContent, cancellationResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, busyResponse.StatusCode);
        Assert.Empty(busyAppointments!);
    }

    [Fact]
    public async Task Doctor_CanSaveConsultationDetail_AndPatientCanReadIt()
    {
        var appointmentId = await SeedAppointmentAsync(
            DateTime.UtcNow.AddDays(3));
        using var doctorClient = CreateClient("doctor-user", IdentityRoles.Doctor);
        using var patientClient = CreateClient("patient-a", IdentityRoles.Patient);
        var request = new UpsertAppointmentConsultationDetailDto
        {
            Diagnostico = "Sobrepeso",
            Recomendaciones = "Aumentar el consumo de verduras.",
            Observaciones = "Seguimiento en cuatro semanas."
        };

        var saveResponse = await doctorClient.PutAsJsonAsync(
            $"/api/appointments/{appointmentId}/consultation-detail",
            request);
        var detailResponse = await patientClient.GetAsync(
            $"/api/appointments/{appointmentId}/consultation-detail");
        var appointmentResponse = await patientClient.GetAsync(
            $"/api/appointments/{appointmentId}");

        var detail = await detailResponse.Content
            .ReadFromJsonAsync<AppointmentConsultationDetailDto>();
        var appointmentContent = await appointmentResponse.Content
            .ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.Equal("Sobrepeso", detail!.Diagnostico);
        Assert.Equal("Aumentar el consumo de verduras.", detail.Recomendaciones);
        Assert.Equal(HttpStatusCode.OK, appointmentResponse.StatusCode);
        Assert.Contains("\"consultationDetail\":{", appointmentContent);
        Assert.Contains("\"diagnostico\":\"Sobrepeso\"", appointmentContent);
    }

    [Fact]
    public async Task Patient_CannotSaveConsultationDetail()
    {
        var appointmentId = await SeedAppointmentAsync(
            DateTime.UtcNow.AddDays(3));
        using var client = CreateClient("patient-a", IdentityRoles.Patient);

        var response = await client.PutAsJsonAsync(
            $"/api/appointments/{appointmentId}/consultation-detail",
            new UpsertAppointmentConsultationDetailDto
            {
                Diagnostico = "Sobrepeso",
                Recomendaciones = "Aumentar el consumo de verduras."
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient CreateClient(string userId, params string[] roles)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RolesHeader, string.Join(',', roles));
        return client;
    }

    private async Task<int> SeedAppointmentAsync(DateTime start)
    {
        await SeedProfilesAsync();
        var appointmentId = 0;

        await _factory.ExecuteDbContextAsync(async context =>
        {
            var patient = context.Patients.Single(profile => profile.UserId == "patient-a");
            var doctor = context.Doctors.Single(profile => profile.UserId == "doctor-user");
            var appointment = Appointment.Create(
                patient.Id,
                doctor.Id,
                start,
                start.AddHours(1),
                "Consulta",
                Modalidad.Online);
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync();
            appointmentId = appointment.Id;
        });

        return appointmentId;
    }

    private async Task SeedProfilesAsync()
    {
        await _factory.ResetDatabaseAsync();
        await _factory.ExecuteDbContextAsync(async context =>
        {
            context.Patients.AddRange(
                new Patient
                {
                    UserId = "patient-a",
                    Nombre = "Ana",
                    Apellido = "Pérez",
                    Numero = "6641234567"
                },
                new Patient
                {
                    UserId = "patient-b",
                    Nombre = "Luis",
                    Apellido = "García",
                    Numero = "6647654321"
                });
            context.Doctors.Add(new Doctor
            {
                UserId = "doctor-user",
                Email = "doctora@example.com"
            });
            await context.SaveChangesAsync();
        });
    }

    private static CreateAppointmentDto CreateRequest(DateTime start) =>
        new()
        {
            Start = start,
            End = start.AddHours(1),
            Motivo = "Consulta",
            Modalidad = Modalidad.Online
        };

    private static UpdateAppointmentDto CreateUpdateRequest(DateTime start) =>
        new()
        {
            Start = start,
            End = start.AddHours(1),
            Motivo = "Seguimiento",
            Modalidad = Modalidad.Presencial
        };
}

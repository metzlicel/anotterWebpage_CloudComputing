using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using anotterwebpage_WebApi.Api.Routes;
using anotterwebpage_WebApi.Data;
using anotterwebpage_WebApi.Delegate;
using anotterwebpage_WebApi.Repositories;
using anotterwebpage_WebApi.Services;
using anotterwebpage_WebApi.Api.Validators;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var bootstrapDoctor = args.Contains(
    "--bootstrap-doctor",
    StringComparer.OrdinalIgnoreCase);

// DATABASE
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//SYSTEM.TEXT.JSON
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy =
        JsonNamingPolicy.CamelCase;

    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});

// IDENTITY
builder.Services
    .AddIdentityApiEndpoints<IdentityUser>(
        options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;

            // Por ahora no exigiremos confirmación de email
            options.SignIn.RequireConfirmedEmail = false;
        })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization();

// REPOSITORIES
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IAppointmentConsultationDetailRepository, AppointmentConsultationDetailRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IAppointmentNotificationRepository, AppointmentNotificationRepository>();

builder.Services.AddScoped<ICollabRepository, CollabRepository>();

// DELEGATES 
builder.Services.AddScoped<IAppointmentDelegate, AppointmentDelegate>();
builder.Services.AddScoped<ICollabDelegate, CollabDelegate>();

// SERVICES 
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAppointmentNotificationFactory, AppointmentNotificationFactory>();
builder.Services.AddScoped<IAppointmentConsultationDetailService, AppointmentConsultationDetailService>();
builder.Services.AddScoped<IPatientRegistrationService, PatientRegistrationService>();
builder.Services.AddScoped<IIdentityProfileInitializer, IdentityProfileInitializer>();
builder.Services.AddScoped<IDoctorBootstrapService, DoctorBootstrapService>();
builder.Services.Configure<AppointmentNotificationOutboxOptions>(
    builder.Configuration.GetSection(AppointmentNotificationOutboxOptions.SectionName));
builder.Services.AddSingleton<AppointmentNotificationDispatcher>();
builder.Services.AddSingleton<IAppointmentNotificationDispatcher>(
    serviceProvider => serviceProvider.GetRequiredService<AppointmentNotificationDispatcher>());
builder.Services.AddHostedService(
    serviceProvider => serviceProvider.GetRequiredService<AppointmentNotificationDispatcher>());
// VALIDATORS
builder.Services.AddValidatorsFromAssemblyContaining<BookAppointmentValidator>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    if (bootstrapDoctor)
    {
        var bootstrapService = scope.ServiceProvider
            .GetRequiredService<IDoctorBootstrapService>();

        var result = await bootstrapService.BootstrapAsync();

        Console.WriteLine(
            $"Doctor bootstrap completed. UserId: {result.UserId}. " +
            $"Account created: {result.AccountCreated}.");

        return;
    }

    var initializer = scope.ServiceProvider
        .GetRequiredService<IIdentityProfileInitializer>();

    await initializer.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// IDENTITY ENDPOINTS
app.MapAuthRoutes();


// APPLICATION ROUTES
app.MapAppointmentRoutes();
app.MapCollaborationRoutes();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature =
            context.Features
                .Get<IExceptionHandlerFeature>();

        var exception =
            exceptionFeature?.Error;

      
        if (exception is BadHttpRequestException ||
            exception is JsonException)
        {
            context.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            context.Response.ContentType =
                "application/problem+json";

            var problem = new
            {
                type =
                    "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Invalid Request",
                status = 400,
                errors = new Dictionary<string, string[]>
                {
                    ["body"] = new[]
                    {
                        "El JSON enviado no es válido."
                    }
                }
            };

            var json =
                JsonSerializer.Serialize(problem);

            await context.Response.WriteAsync(json);

            return;
        }
        
        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        context.Response.ContentType =
            "application/problem+json";

        await context.Response.WriteAsJsonAsync(
            new
            {
                type =
                    "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500
            });
    });
});
//
// var summaries = new[]
// {
//     "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
// };
//
// app.MapGet("/weatherforecast", () =>
//     {
//         var forecast = Enumerable.Range(1, 5).Select(index =>
//                 new WeatherForecast
//                 (
//                     DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//                     Random.Shared.Next(-20, 55),
//                     summaries[Random.Shared.Next(summaries.Length)]
//                 ))
//             .ToArray();
//         return forecast;
//     })
//     .WithName("GetWeatherForecast")
//     .WithOpenApi();

app.Run();

public partial class Program
{
}

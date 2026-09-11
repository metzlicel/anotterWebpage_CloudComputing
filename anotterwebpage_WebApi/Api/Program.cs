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
    .AddIdentityApiEndpoints<IdentityUser>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization();

// REPOSITORIES
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();

builder.Services.AddScoped<ICollabRepository, CollabRepository>();

// DELEGATES 
builder.Services.AddScoped<IAppointmentDelegate, AppointmentDelegate>();
builder.Services.AddScoped<ICollabDelegate, CollabDelegate>();

// SERVICES 
builder.Services.AddScoped<IEmailService, EmailService>();
// VALIDATORS
builder.Services.AddValidatorsFromAssemblyContaining<BookAppointmentValidator>();

var app = builder.Build();

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
app.MapGroup("/api/auth")
    .MapIdentityApi<IdentityUser>();

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
using anotterwebpage_WebApi.Domain;
using Microsoft.AspNetCore.Identity;

namespace anotterwebpage_WebApi.Services;

public class AppointmentNotificationFactory : IAppointmentNotificationFactory
{
    private readonly UserManager<IdentityUser> _userManager;

    public AppointmentNotificationFactory(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyCollection<AppointmentNotification>> CreateAsync(
        Appointment appointment,
        AppointmentNotificationType type,
        AppointmentDetails? previousDetails = null,
        CancellationToken cancellationToken = default)
    {
        var patientUser = await _userManager.FindByIdAsync(appointment.Patient.UserId);

        if (string.IsNullOrWhiteSpace(patientUser?.Email))
        {
            throw new InvalidOperationException(
                "El paciente no tiene un correo electrónico configurado.");
        }

        if (string.IsNullOrWhiteSpace(appointment.Doctor.Email))
        {
            throw new InvalidOperationException(
                "La doctora no tiene un correo electrónico configurado.");
        }

        var eventKey = $"appointment:{appointment.Id}:{type}:{Guid.NewGuid():N}";
        var patientMessage = BuildMessage(appointment, type, previousDetails, true);
        var doctorMessage = BuildMessage(appointment, type, previousDetails, false);

        return new[]
        {
            CreateNotification(
                appointment,
                type,
                eventKey,
                patientUser.Email,
                Subject(type),
                patientMessage),
            CreateNotification(
                appointment,
                type,
                eventKey,
                appointment.Doctor.Email,
                Subject(type),
                doctorMessage)
        };
    }

    private static AppointmentNotification CreateNotification(
        Appointment appointment,
        AppointmentNotificationType type,
        string eventKey,
        string recipientEmail,
        string subject,
        string body) =>
        new()
        {
            Appointment = appointment,
            Type = type,
            EventKey = eventKey,
            RecipientEmail = recipientEmail,
            Subject = subject,
            Body = body
        };

    private static string Subject(AppointmentNotificationType type) => type switch
    {
        AppointmentNotificationType.Creada => "Cita confirmada",
        AppointmentNotificationType.Modificada => "Cita modificada",
        AppointmentNotificationType.Cancelada => "Cita cancelada",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static string BuildMessage(
        Appointment appointment,
        AppointmentNotificationType type,
        AppointmentDetails? previousDetails,
        bool isPatient)
    {
        var recipient = isPatient
            ? appointment.Patient.Nombre
            : "Doctora";
        var current = Details(appointment.Snapshot());
        var action = type switch
        {
            AppointmentNotificationType.Creada => "confirmada",
            AppointmentNotificationType.Modificada => "modificada",
            AppointmentNotificationType.Cancelada => "cancelada",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        var previous = type == AppointmentNotificationType.Modificada &&
                       previousDetails is not null
            ? $"<p>Horario anterior: {Details(previousDetails)}</p>"
            : string.Empty;

        return $"<p>Hola, {recipient}.</p>" +
               $"<p>La cita ha sido {action}.</p>" +
               previous +
               $"<p>Horario actual: {current}</p>";
    }

    private static string Details(AppointmentDetails details) =>
        $"{details.Start:O} a {details.End:O}; " +
        $"modalidad: {details.Modalidad}; motivo: {details.Motivo}";
}

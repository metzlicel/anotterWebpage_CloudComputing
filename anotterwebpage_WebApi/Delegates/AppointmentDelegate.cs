using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Repositories;
using anotterwebpage_WebApi.Services;

namespace anotterwebpage_WebApi.Delegate;

public class AppointmentDelegate : IAppointmentDelegate
{
    private readonly IAppointmentRepository _repository;
    private readonly IEmailService _email;

    public AppointmentDelegate(
        IAppointmentRepository repository,
        IEmailService email)
    {
        _repository = repository;
        _email = email;
    }
    
    public async Task<List<Appointment>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }    
    public async Task<Appointment?> UpdateAsync(
        int id,
        UpdateAppointmentDto request,
        string? userId)
    {
        var appointment =
            await _repository.GetByIdAsync(id);

        if (appointment == null)
            return null;

        if (appointment.UserId != userId)
            return null;

        var overlap =
            await _repository.HasOverlapAsync(
                request.Start,
                request.End,
                id);

        if (overlap)
        {
            throw new InvalidOperationException(
                "Ese horario ya está reservado.");
        }

        appointment.Start = request.Start;
        appointment.End = request.End;
        appointment.Nombre = request.Nombre;
        appointment.Apellido = request.Apellido;
        appointment.NombrePaciente = request.NombrePaciente;
        appointment.Email = request.Email;
        appointment.Numero = request.Numero;
        appointment.Motivo = request.Motivo;
        appointment.Modalidad = request.Modalidad!.Value;

        return await _repository.UpdateAsync(appointment);
    }
    

    public async Task<List<Appointment>> GetBusyAsync(
        DateTime start,
        DateTime end)
    {
        return await _repository.GetBusyAsync(start, end);
    }

    // Tablas de roles user y admin
    public async Task<Appointment?> GetAppointmentAsync(
        int id,
        string? userId)
    {
        var appointment =
            await _repository.GetByIdAsync(id);

        if (appointment == null)
            return null;

        if (appointment.UserId != userId)
            return null;

        return appointment;
    }

    public async Task<Appointment> BookAsync(
        CreateAppointmentDto request,
        string? userId)
    {
        var overlap = await _repository.HasOverlapAsync(
            request.Start,
            request.End);

        if (overlap)
        {
            throw new InvalidOperationException(
                "Ese horario ya está reservado.");
        }

        var appointment = new Appointment
        {
            Start = request.Start,
            End = request.End,
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            NombrePaciente = request.NombrePaciente,
            Email = request.Email,
            Numero = request.Numero,
            Motivo = request.Motivo,
            Modalidad = request.Modalidad!.Value,
            IsReserved = true,
            UserId = userId
        };

        await _repository.CreateAsync(appointment);

        await SendEmailsAsync(appointment);

        return appointment;
    }

    public async Task<bool> DeleteAsync(
        int id,
        string? userId)
    {
        var appointment =
            await _repository.GetByIdAsync(id);

        if (appointment == null)
            return false;

        if (appointment.UserId != userId)
            return false;

        await _repository.DeleteAsync(appointment);

        return true;
    }
    
    // Mover a una propiedad, link desde el program al email por injection
    private async Task SendEmailsAsync(Appointment appointment)
    {
        var adminEmail = "metzli.lopez@cetys.edu.mx";

        var bodyAdmin = $@"
            <h2>Nueva cita agendada</h2>
            <p><b>Fecha:</b> {appointment.Start}</p>
            <p><b>Nombre:</b> {appointment.Nombre} {appointment.Apellido}</p>
            <p><b>Paciente:</b> {(string.IsNullOrWhiteSpace(appointment.NombrePaciente) ? "—" : appointment.NombrePaciente)}</p>
            <p><b>Email:</b> {appointment.Email}</p>
            <p><b>WhatsApp:</b> {appointment.Numero}</p>
            <p><b>Motivo:</b> {appointment.Motivo}</p>
            <p><b>Modalidad:</b> {appointment.Modalidad}</p>
        ";

        await _email.SendEmailAsync(
            adminEmail,
            "Nueva cita agendada",
            bodyAdmin
        );

        var bodyPatient = $@"
            <p>¡Hola!</p>

            <p>
                Gracias por agendar tu consulta con
                <strong>Connie López</strong> 🤍
            </p>

            <p>
                El costo de la consulta es de
                <strong>$950 MXN</strong> e incluye:
            </p>

            <ul>
                <li>✨ Toma de medidas básicas</li>
                <li>✨ Valoración nutricional</li>
                <li>✨ Manuales y material de apoyo</li>
                <li>✨ Educación en nutrición</li>
                <li>✨ Plan de alimentación (según necesidades del paciente)</li>
            </ul>

            <p>
                <b>Fecha:</b>
                {appointment.Start:dddd dd 'de' MMMM yyyy, HH:mm} hrs
            </p>

            <p><b>Nombre:</b> {appointment.Nombre} {appointment.Apellido}</p>

            <p>
                <b>Paciente:</b>
                {(string.IsNullOrWhiteSpace(appointment.NombrePaciente)
                    ? "—"
                    : appointment.NombrePaciente)}
            </p>

            <p><b>Email:</b> {appointment.Email}</p>
            <p><b>WhatsApp:</b> {appointment.Numero}</p>
            <p><b>Motivo:</b> {appointment.Motivo}</p>
            <p><b>Modalidad:</b> {appointment.Modalidad}</p>

            <p>
                <strong>Políticas de cancelación y reagenda:</strong>
            </p>

            <ul>
                <li>
                    Cancelar una consulta con cita confirmada genera una multa
                    del <strong>costo total</strong> de la consulta.
                </li>

                <li>
                    Reagendar la misma consulta en
                    <strong>3 ocasiones</strong> generará una multa de
                    <strong>$200 MXN</strong>.
                </li>

                <li>
                    No se permite el cambio de modalidad
                    (en línea/presencial) de último momento.
                    En días lluviosos o por seguridad de ambas partes,
                    esto puede ser sugerido por tu nutrióloga.
                </li>
            </ul>

            <p>
                Cualquier duda, puedes responder directamente a este correo. 💌
            </p>
        ";

        await _email.SendEmailAsync(
            appointment.Email,
            "Gracias por agendar con Connie López",
            bodyPatient
        );
    }
}
using anotterwebpage_WebApi.Api.Requests;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Repositories;
using anotterwebpage_WebApi.Services;

namespace anotterwebpage_WebApi.Delegate;

public class CollabDelegate : ICollabDelegate
{
    private readonly ICollabRepository _repository;
    private readonly EmailService _email;

    public CollabDelegate(
        ICollabRepository repository,
        EmailService email)
    {
        _repository = repository;
        _email = email;
    }

    public async Task<Collab> CreateAsync(
        CreateCollabRequest request)
    {
        var collaboration = new Collab
        {
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            NombreOrg = request.NombreOrg,
            Email = request.Email,
            Numero = request.Numero,
            Motivo = request.Motivo
        };

        await _repository.CreateAsync(collaboration);

        //await SendEmailsAsync(collaboration);

        return collaboration;
    }

    private async Task SendEmailsAsync(Collab collaboration)
    {
        var adminEmail = "metzli.lopez@cetys.edu.mx";

        var bodyAdmin = $@"
            <h2>Nueva solicitud de colaboración</h2>
            <p><b>Nombre:</b> {collaboration.Nombre} {collaboration.Apellido}</p>
            <p><b>Institución:</b> {collaboration.NombreOrg}</p>
            <p><b>Email:</b> {collaboration.Email}</p>
            <p><b>WhatsApp:</b> {collaboration.Numero}</p>
            <p><b>Motivo:</b> {collaboration.Motivo}</p>
        ";

        await _email.SendEmailAsync(
            adminEmail,
            "Nueva colaboración",
            bodyAdmin);

        await _email.SendEmailAsync(
            collaboration.Email,
            "Gracias por colaborar con nosotros",
            "<h3>Gracias por tu mensaje, pronto nos pondremos en contacto contigo. 🧡</h3>");
    }
}
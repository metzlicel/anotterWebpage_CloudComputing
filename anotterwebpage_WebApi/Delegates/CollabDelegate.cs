using anotterwebpage_WebApi.Api.Dtos;
using anotterwebpage_WebApi.Domain;
using anotterwebpage_WebApi.Repositories;
using anotterwebpage_WebApi.Services;

namespace anotterwebpage_WebApi.Delegate;

public class CollabDelegate : ICollabDelegate
{
    private readonly ICollabRepository _repository;
    private readonly IEmailService _email;

    public CollabDelegate(
        ICollabRepository repository,
        IEmailService email)
    {
        _repository = repository;
        _email = email;
    }

    public async Task<Collab> CreateAsync(
        CreateCollabDto request)
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

        await SendEmailsAsync(collaboration);

        return collaboration;
    }
    
    public async Task<List<Collab>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }
    
    public async Task<Collab?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Collab?> UpdateAsync(
        int id,
        UpdateCollabDto request)
    {
        var collab = await _repository.GetByIdAsync(id);

        if (collab == null)
            return null;

        collab.Nombre = request.Nombre;
        collab.Apellido = request.Apellido;
        collab.NombreOrg = request.NombreOrg;
        collab.Email = request.Email;
        collab.Numero = request.Numero;
        collab.Motivo = request.Motivo;

        return await _repository.UpdateAsync(collab);
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        var collab = await _repository.GetByIdAsync(id);

        if (collab == null)
            return false;

        await _repository.DeleteAsync(collab);

        return true;
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
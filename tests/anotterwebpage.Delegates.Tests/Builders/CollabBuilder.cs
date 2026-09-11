using anotterwebpage_WebApi.Domain;

namespace anotterwebpage.Delegates.Tests.Builders;

public class CollabBuilder
{
    private readonly Collab _collab = new()
    {
        Id = 1,
        Nombre = "Metzli",
        Apellido = "Lopez",
        NombreOrg = "CETYS",
        Email = "metzli@example.com",
        Numero = "6641234567",
        Motivo = "Colaboración"
    };

    public CollabBuilder WithId(int id)
    {
        _collab.Id = id;
        return this;
    }

    public CollabBuilder WithNombre(string nombre)
    {
        _collab.Nombre = nombre;
        return this;
    }

    public CollabBuilder WithEmail(string email)
    {
        _collab.Email = email;
        return this;
    }

    public Collab Build()
    {
        return _collab;
    }
}
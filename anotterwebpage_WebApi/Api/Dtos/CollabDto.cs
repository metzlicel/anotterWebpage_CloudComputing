namespace anotterwebpage_WebApi.Api.Dtos;

public class CollabDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? NombreOrg { get; set; }

    public string Email { get; set; } = "";
    public string Numero { get; set; } = "";
    public string? Motivo { get; set; }
}

public class CreateCollabDto
{
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? NombreOrg { get; set; }
    public string Email { get; set; } = "";
    public string Numero { get; set; } = "";
    public string? Motivo { get; set; }
}

public class UpdateCollabDto
{
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? NombreOrg { get; set; }
    public string Email { get; set; } = "";
    public string Numero { get; set; } = "";
    public string? Motivo { get; set; }
}
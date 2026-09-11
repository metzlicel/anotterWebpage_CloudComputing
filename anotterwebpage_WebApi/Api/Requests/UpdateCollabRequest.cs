namespace anotterwebpage_WebApi.Api.Requests;

public class UpdateCollabRequest
{
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? NombreOrg { get; set; }
    public string Email { get; set; } = "";
    public string Numero { get; set; } = "";
    public string? Motivo { get; set; }
}
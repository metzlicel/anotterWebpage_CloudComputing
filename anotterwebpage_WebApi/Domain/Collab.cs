using System.ComponentModel.DataAnnotations;

namespace anotterwebpage_WebApi.Domain;

public class Collab
{
    public int Id { get; set; }


    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? NombreOrg { get; set; } = "";
    [EmailAddress]
    public string Email { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Motivo { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
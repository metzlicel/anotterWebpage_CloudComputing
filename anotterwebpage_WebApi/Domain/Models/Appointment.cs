using System.ComponentModel.DataAnnotations;

namespace anotterwebpage_WebApi.Domain;

public class Appointment
{
    public int Id { get; set; }

    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? NombrePaciente { get; set; } = "";
    [EmailAddress]
    public string Email { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Motivo { get; set; }
    public bool IsReserved { get; set; }
    public string Modalidad { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string? UserId { get; set; }
}
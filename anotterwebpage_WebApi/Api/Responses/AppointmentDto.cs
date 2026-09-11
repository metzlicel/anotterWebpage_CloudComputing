namespace anotterwebpage_WebApi.Api.Responses;

public class AppointmentDto
{
    public int Id { get; set; }

    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? NombrePaciente { get; set; }

    public string Email { get; set; } = "";
    public string Numero { get; set; } = "";
    public string? Motivo { get; set; }

    public string Modalidad { get; set; } = "";
}
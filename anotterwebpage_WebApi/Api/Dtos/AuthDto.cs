namespace anotterwebpage_WebApi.Api.Dtos;

public class RegisterPatientDto
{
    public string Email { get; set; } = "";

    public string Password { get; set; } = "";

    public string Nombre { get; set; } = "";

    public string Apellido { get; set; } = "";

    public string Numero { get; set; } = "";
}

public class CompletePatientProfileDto
{
    public string Nombre { get; set; } = "";

    public string Apellido { get; set; } = "";

    public string Numero { get; set; } = "";
}

public class PatientProfileDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Apellido { get; set; } = "";

    public string Numero { get; set; } = "";
}

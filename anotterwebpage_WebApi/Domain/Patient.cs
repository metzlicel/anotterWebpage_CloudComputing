namespace anotterwebpage_WebApi.Domain;

public class Patient
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    public string Nombre { get; set; } = "";

    public string Apellido { get; set; } = "";

    public string Numero { get; set; } = "";

    public ICollection<Appointment> Appointments { get; set; } =
        new List<Appointment>();
}

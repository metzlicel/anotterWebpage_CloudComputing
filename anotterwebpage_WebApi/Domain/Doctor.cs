using System.ComponentModel.DataAnnotations;

namespace anotterwebpage_WebApi.Domain;

public class Doctor
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    [EmailAddress]
    public string Email { get; set; } = "";

    public ICollection<Appointment> Appointments { get; set; } =
        new List<Appointment>();
}

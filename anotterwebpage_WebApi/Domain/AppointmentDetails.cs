namespace anotterwebpage_WebApi.Domain;

public record AppointmentDetails(
    DateTime Start,
    DateTime End,
    string Motivo,
    Modalidad Modalidad);

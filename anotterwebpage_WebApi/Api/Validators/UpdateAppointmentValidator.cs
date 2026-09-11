using FluentValidation;
using anotterwebpage_WebApi.Api.Dtos;

namespace anotterwebpage_WebApi.Api.Validators;

public class UpdateAppointmentValidator
    : AbstractValidator<UpdateAppointmentDto>
{
    public UpdateAppointmentValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty();

        RuleFor(x => x.Apellido)
            .NotEmpty();

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Numero)
            .NotEmpty();

        RuleFor(x => x.Modalidad)
            .NotEmpty();

        RuleFor(x => x.End)
            .GreaterThan(x => x.Start)
            .WithMessage(
                "La fecha final debe ser posterior a la inicial.");
    }
}
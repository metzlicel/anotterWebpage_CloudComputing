using FluentValidation;
using anotterwebpage_WebApi.Api.Dtos;

namespace anotterwebpage_WebApi.Api.Validators;

public class BookAppointmentValidator
    : AbstractValidator<CreateAppointmentDto>
{
    public BookAppointmentValidator()
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
            .NotNull()
            .WithMessage("La modalidad es obligatoria.");

        RuleFor(x => x.Modalidad)
            .IsInEnum()
            .When(x => x.Modalidad.HasValue);

        RuleFor(x => x.End)
            .NotEmpty()
            .GreaterThan(x => x.Start)
            .WithMessage(
                "La fecha final debe ser posterior a la inicial.");
    }
}
using FluentValidation;
using anotterwebpage_WebApi.Api.Dtos;

namespace anotterwebpage_WebApi.Api.Validators;

public class BookAppointmentValidator
    : AbstractValidator<CreateAppointmentDto>
{
    public BookAppointmentValidator()
    {
        RuleFor(x => x.Start)
            .NotEmpty()
            .Must(date => date.Kind == DateTimeKind.Utc)
            .WithMessage("La fecha inicial debe estar en UTC.");

        RuleFor(x => x.Motivo)
            .NotEmpty();

        RuleFor(x => x.Modalidad)
            .NotNull()
            .WithMessage("La modalidad es obligatoria.");

        RuleFor(x => x.Modalidad)
            .IsInEnum()
            .When(x => x.Modalidad.HasValue);

        RuleFor(x => x.End)
            .NotEmpty()
            .Must(date => date.Kind == DateTimeKind.Utc)
            .WithMessage("La fecha final debe estar en UTC.")
            .GreaterThan(x => x.Start)
            .WithMessage(
                "La fecha final debe ser posterior a la inicial.");
    }
}

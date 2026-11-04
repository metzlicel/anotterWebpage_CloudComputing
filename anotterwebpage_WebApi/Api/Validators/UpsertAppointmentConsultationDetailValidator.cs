using anotterwebpage_WebApi.Api.Dtos;
using FluentValidation;

namespace anotterwebpage_WebApi.Api.Validators;

public class UpsertAppointmentConsultationDetailValidator
    : AbstractValidator<UpsertAppointmentConsultationDetailDto>
{
    public UpsertAppointmentConsultationDetailValidator()
    {
        RuleFor(detail => detail.Diagnostico)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(detail => detail.Recomendaciones)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(detail => detail.Observaciones)
            .MaximumLength(4000)
            .When(detail => detail.Observaciones is not null);
    }
}

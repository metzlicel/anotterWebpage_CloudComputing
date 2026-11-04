using anotterwebpage_WebApi.Api.Dtos;
using FluentValidation;

namespace anotterwebpage_WebApi.Api.Validators;

public class RegisterPatientValidator : AbstractValidator<RegisterPatientDto>
{
    public RegisterPatientValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();

        AddProfileRules();
    }

    private void AddProfileRules()
    {
        RuleFor(x => x.Nombre).NotEmpty();
        RuleFor(x => x.Apellido).NotEmpty();
        RuleFor(x => x.Numero).NotEmpty();
    }
}

public class CompletePatientProfileValidator
    : AbstractValidator<CompletePatientProfileDto>
{
    public CompletePatientProfileValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty();
        RuleFor(x => x.Apellido).NotEmpty();
        RuleFor(x => x.Numero).NotEmpty();
    }
}

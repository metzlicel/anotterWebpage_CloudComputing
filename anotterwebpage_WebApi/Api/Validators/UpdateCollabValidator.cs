using FluentValidation;
using anotterwebpage_WebApi.Api.Requests;

namespace anotterwebpage_WebApi.Api.Validators;

public class UpdateCollabValidator
    : AbstractValidator<UpdateCollabRequest>
{
    public UpdateCollabValidator()
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

        RuleFor(x => x.Motivo)
            .NotEmpty();
    }
}
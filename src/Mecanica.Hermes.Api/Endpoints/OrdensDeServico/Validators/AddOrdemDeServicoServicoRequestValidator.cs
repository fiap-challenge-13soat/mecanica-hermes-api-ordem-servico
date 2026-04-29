using FluentValidation;
using Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Validators;

public sealed class AddOrdemDeServicoServicoRequestValidator : AbstractValidator<AddOrdemDeServicoServicoRequest>
{
    public AddOrdemDeServicoServicoRequestValidator()
    {
        RuleFor(x => x.ServicoId)
            .NotEmpty().WithMessage("O ServicoId é obrigatório.");

        RuleFor(x => x.Valor)
            .GreaterThanOrEqualTo(0).WithMessage("O Valor não pode ser negativo.");
    }
}

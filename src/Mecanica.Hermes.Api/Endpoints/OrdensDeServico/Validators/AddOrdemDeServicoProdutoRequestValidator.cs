using FluentValidation;
using Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Validators;

public sealed class AddOrdemDeServicoProdutoRequestValidator : AbstractValidator<AddOrdemDeServicoProdutoRequest>
{
    public AddOrdemDeServicoProdutoRequestValidator()
    {
        RuleFor(x => x.ProdutoId)
            .NotEmpty().WithMessage("O ProdutoId é obrigatório.");

        RuleFor(x => x.ValorUnitario)
            .GreaterThanOrEqualTo(0).WithMessage("O ValorUnitario não pode ser negativo.");

        RuleFor(x => x.Quantidade)
            .GreaterThan(0).WithMessage("A Quantidade deve ser maior que zero.");
    }
}

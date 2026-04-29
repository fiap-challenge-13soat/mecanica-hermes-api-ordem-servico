using FluentValidation;
using Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Contracts;

namespace Mecanica.Hermes.Api.Endpoints.OrdensDeServico.Validators;

public sealed class CreateOrdemDeServicoRequestValidator : AbstractValidator<CreateOrdemDeServicoRequest>
{
    public CreateOrdemDeServicoRequestValidator()
    {
        RuleFor(x => x.ClienteId)
            .NotEmpty().WithMessage("O ClienteId é obrigatório.");

        RuleFor(x => x.VeiculoId)
            .NotEmpty().WithMessage("O VeiculoId é obrigatório.");

        RuleFor(x => x.ProblemaRelatado)
            .MaximumLength(1000).WithMessage("ProblemaRelatado não pode exceder 1000 caracteres.")
            .When(x => x.ProblemaRelatado is not null);

        RuleFor(x => x.Observacoes)
            .MaximumLength(1000).WithMessage("Observacoes não pode exceder 1000 caracteres.")
            .When(x => x.Observacoes is not null);
    }
}

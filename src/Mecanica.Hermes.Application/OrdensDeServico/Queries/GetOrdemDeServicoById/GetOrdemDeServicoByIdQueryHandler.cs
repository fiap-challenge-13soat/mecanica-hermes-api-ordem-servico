using Mecanica.Hermes.Application.OrdensDeServico.Dtos;
using Mecanica.Hermes.Application.OrdensDeServico.Mappings;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Shared.Core.Results;
using MediatR;

namespace Mecanica.Hermes.Application.OrdensDeServico.Queries.GetOrdemDeServicoById;

internal sealed class GetOrdemDeServicoByIdQueryHandler(IOrdemDeServicoReader repository)
    : IRequestHandler<GetOrdemDeServicoByIdQuery, Result<OrdemDeServicoDto>>
{
    public async Task<Result<OrdemDeServicoDto>> Handle(GetOrdemDeServicoByIdQuery request,
        CancellationToken cancellationToken)
    {
        var ordemDeServico = await repository.GetByIdAsync(request.Id);

        if (ordemDeServico is null)
            return Result<OrdemDeServicoDto>.NotFound();

        return Result<OrdemDeServicoDto>.Ok(ordemDeServico.ToDto());
    }
}

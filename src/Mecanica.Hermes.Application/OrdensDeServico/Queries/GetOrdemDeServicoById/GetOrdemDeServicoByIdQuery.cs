using Mecanica.Hermes.Application.OrdensDeServico.Dtos;
using Mecanica.Hermes.Domain.Common.Results;
using MediatR;

namespace Mecanica.Hermes.Application.OrdensDeServico.Queries.GetOrdemDeServicoById;

public sealed record GetOrdemDeServicoByIdQuery(Guid Id) : IRequest<Result<OrdemDeServicoDto>>;

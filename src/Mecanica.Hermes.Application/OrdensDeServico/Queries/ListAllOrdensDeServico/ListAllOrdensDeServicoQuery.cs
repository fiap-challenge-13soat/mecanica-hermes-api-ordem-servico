using Mecanica.Hermes.Application.Common.Pagination;
using Mecanica.Hermes.Application.OrdensDeServico.Dtos;
using Mecanica.Hermes.Domain.Common.Results;
using MediatR;

namespace Mecanica.Hermes.Application.OrdensDeServico.Queries.ListAllOrdensDeServico;

public sealed record ListAllOrdensDeServicoQuery(
    int Page = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<OrdemDeServicoDto>>>;

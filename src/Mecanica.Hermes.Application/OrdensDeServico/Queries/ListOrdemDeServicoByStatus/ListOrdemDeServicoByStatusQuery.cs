using Mecanica.Hermes.Shared.Core.Pagination;
using Mecanica.Hermes.Application.OrdensDeServico.Dtos;
using Mecanica.Hermes.Shared.Core.Results;
using MediatR;

namespace Mecanica.Hermes.Application.OrdensDeServico.Queries.ListOrdemDeServicoByStatus;

public sealed record ListOrdemDeServicoByStatusQuery(
    string Status,
    int Page = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<OrdemDeServicoDto>>>;

using System.Net;
using Mecanica.Hermes.Application.Common.Pagination;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Application.OrdensDeServico.Queries.GetOrdemDeServicoById;
using Mecanica.Hermes.Application.OrdensDeServico.Queries.ListAllOrdensDeServico;
using Mecanica.Hermes.Application.OrdensDeServico.Queries.ListOrdemDeServicoByStatus;
using Mecanica.Hermes.Domain.OrdensDeServico;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.TestUtils.Builders;

namespace Mecanica.Hermes.Application.Tests.OrdensDeServico.Queries;

/// <summary>
///     Testes consolidados dos 3 query handlers. Dependem apenas de <see cref="IOrdemDeServicoReader" />
///     (ISP — ver ADR-010).
/// </summary>
[Trait("Category", "Unit")]
public class QueryHandlerTests
{
    private readonly Mock<IOrdemDeServicoReader> _reader = new();

    // ---- GetOrdemDeServicoById ----

    [Fact]
    public async Task GetById_QuandoOrdemExiste_DeveRetornarSucesso()
    {
        var ordem = new OrdemDeServicoBuilder().Build();
        _reader.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);
        var handler = new GetOrdemDeServicoByIdQueryHandler(_reader.Object);

        var result = await handler.Handle(new GetOrdemDeServicoByIdQuery(ordem.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(ordem.Id);
        _reader.Verify(r => r.GetByIdAsync(ordem.Id), Times.Once);
    }

    [Fact]
    public async Task GetById_QuandoOrdemNaoExiste_DeveRetornarNotFound()
    {
        _reader.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((OrdemDeServico?)null);
        var handler = new GetOrdemDeServicoByIdQueryHandler(_reader.Object);

        var result = await handler.Handle(new GetOrdemDeServicoByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- ListAllOrdensDeServico ----

    [Fact]
    public async Task ListAll_DeveRetornarPagedResult()
    {
        var ordens = new[] { new OrdemDeServicoBuilder().Build(), new OrdemDeServicoBuilder().Build() };
        var paged = PagedResult<OrdemDeServico>.Create(ordens, 1, 10, 2);
        _reader.Setup(r => r.ListAllAsync(It.IsAny<PaginationParams>())).ReturnsAsync(paged);
        var handler = new ListAllOrdensDeServicoQueryHandler(_reader.Object);

        var result = await handler.Handle(new ListAllOrdensDeServicoQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ListAll_QuandoVazio_DeveRetornarSucessoComListaVazia()
    {
        _reader.Setup(r => r.ListAllAsync(It.IsAny<PaginationParams>()))
            .ReturnsAsync(PagedResult<OrdemDeServico>.Create([], 1, 10, 0));
        var handler = new ListAllOrdensDeServicoQueryHandler(_reader.Object);

        var result = await handler.Handle(new ListAllOrdensDeServicoQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    // ---- ListOrdemDeServicoByStatus ----

    [Theory]
    [InlineData("Recebida", OrdemDeServicoStatus.Recebida)]
    [InlineData("EmDiagnostico", OrdemDeServicoStatus.EmDiagnostico)]
    public async Task ListByStatus_ComStatusValido_DeveChamarReaderComEnumCorreto(
        string statusString, OrdemDeServicoStatus statusEnum)
    {
        _reader.Setup(r => r.ListByStatusAsync(It.IsAny<OrdemDeServicoStatus>(), It.IsAny<PaginationParams>()))
            .ReturnsAsync(PagedResult<OrdemDeServico>.Create([], 1, 10, 0));
        var handler = new ListOrdemDeServicoByStatusQueryHandler(_reader.Object);

        var result = await handler.Handle(
            new ListOrdemDeServicoByStatusQuery(statusString),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _reader.Verify(r => r.ListByStatusAsync(statusEnum, It.IsAny<PaginationParams>()), Times.Once);
    }

    [Fact]
    public async Task ListByStatus_ComStatusInvalido_DeveRetornarBadRequest()
    {
        var handler = new ListOrdemDeServicoByStatusQueryHandler(_reader.Object);

        var result = await handler.Handle(
            new ListOrdemDeServicoByStatusQuery("StatusInexistente"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

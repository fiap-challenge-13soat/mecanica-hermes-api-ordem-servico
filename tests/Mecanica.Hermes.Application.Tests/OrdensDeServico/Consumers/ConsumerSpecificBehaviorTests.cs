using Mecanica.Hermes.Application.Common.Persistence;
using Mecanica.Hermes.Application.OrdensDeServico.Consumers;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Application.Tests.Common;
using Mecanica.Hermes.Domain.OrdensDeServico;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.TestUtils.Builders;

namespace Mecanica.Hermes.Application.Tests.OrdensDeServico.Consumers;

/// <summary>
///     Testes focados no comportamento <strong>específico</strong> de cada consumer concreto:
///     - override correto de <c>OperacaoMetricKey</c>;
///     - chamada do método de domínio esperado;
///     - aplicação de <c>ValidatePreconditions</c> quando relevante.
///     Os caminhos genéricos do pipeline são cobertos por <c>OrdemDeServicoConsumerBaseTests</c>.
///     Cada consumer é invocado diretamente via <see cref="FakeConsumeContext" />, evitando o
///     overhead do harness MassTransit em cada cenário.
/// </summary>
[Trait("Category", "Unit")]
public class ConsumerSpecificBehaviorTests
{
    private readonly Mock<IOrdemDeServicoMetrics> _metrics = new();
    private readonly Mock<IOrdemDeServicoRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    [Fact]
    public async Task AddProduto_DeveChamarAdicionarProdutoNoDominio()
    {
        var ordem = new OrdemDeServicoBuilder().Build();
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var consumer = new AddProdutoConsumer(_repository.Object, _uow.Object);
        var produtoId = Guid.NewGuid();

        await consumer.Consume(FakeConsumeContext.For(new ExecuteAddProduto(ordem.Id, produtoId, 100m, 2)));

        ordem.Produtos.Should().ContainSingle(p => p.ProdutoId == produtoId);
        _repository.Verify(r => r.UpdateAsync(ordem), Times.Once);
    }

    [Fact]
    public async Task AddServico_DeveChamarAdicionarServicoNoDominio()
    {
        var ordem = new OrdemDeServicoBuilder().Build();
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var consumer = new AddServicoConsumer(_repository.Object, _uow.Object);
        var servicoId = Guid.NewGuid();

        await consumer.Consume(FakeConsumeContext.For(new ExecuteAddServico(ordem.Id, servicoId, 150m)));

        ordem.Servicos.Should().ContainSingle(s => s.ServicoId == servicoId);
    }

    [Fact]
    public async Task RemoveProduto_DeveChamarRemoverProdutoNoDominio()
    {
        var produtoId = Guid.NewGuid();
        var ordem = new OrdemDeServicoBuilder().ComProduto(produtoId).Build();
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var consumer = new RemoveProdutoConsumer(_repository.Object, _uow.Object);

        await consumer.Consume(FakeConsumeContext.For(new ExecuteRemoveProduto(ordem.Id, produtoId)));

        ordem.Produtos.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveServico_DeveChamarRemoverServicoNoDominio()
    {
        var servicoId = Guid.NewGuid();
        var ordem = new OrdemDeServicoBuilder().ComServico(servicoId).Build();
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var consumer = new RemoveServicoConsumer(_repository.Object, _uow.Object);

        await consumer.Consume(FakeConsumeContext.For(new ExecuteRemoveServico(ordem.Id, servicoId)));

        ordem.Servicos.Should().BeEmpty();
    }

    [Fact]
    public async Task AvancarEtapa_DeveChamarAvancarEtapaNoDominio()
    {
        var ordem = new OrdemDeServicoBuilder().Build();
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var consumer = new AvancarEtapaConsumer(_repository.Object, _uow.Object, _metrics.Object);

        await consumer.Consume(FakeConsumeContext.For(new ExecuteAvancarEtapa(ordem.Id)));

        ordem.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.EmDiagnostico);
    }

    [Fact]
    public async Task ReverterEtapa_DeveChamarReverterEtapaNoDominio()
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(1).Build();
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var consumer = new ReverterEtapaConsumer(_repository.Object, _uow.Object, _metrics.Object);

        await consumer.Consume(FakeConsumeContext.For(new ExecuteReverterEtapa(ordem.Id)));

        ordem.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.Recebida);
    }

    [Fact]
    public async Task Cancelar_DeveChamarCancelarNoDominio()
    {
        var ordem = new OrdemDeServicoBuilder().Build();
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var consumer = new CancelarOrdemDeServicoConsumer(_repository.Object, _uow.Object);

        await consumer.Consume(FakeConsumeContext.For(new ExecuteCancelarOrdemDeServico(ordem.Id)));

        ordem.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.Cancelada);
    }

    [Fact]
    public async Task Aprovar_QuandoEmAguardandoAprovacao_DeveAvancarEtapa()
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(2).Build();
        ordem.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.AguardandoAprovacao);
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var consumer = new AprovarOrdemDeServicoConsumer(_repository.Object, _uow.Object, _metrics.Object);

        await consumer.Consume(FakeConsumeContext.For(new ExecuteAprovarOrdemDeServico(ordem.Id)));

        ordem.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.EmExecucao);
        _repository.Verify(r => r.UpdateAsync(ordem), Times.Once);
    }

    [Fact]
    public async Task Aprovar_QuandoStatusDiferente_DeveRegistrarMetricaStatusInvalido()
    {
        var ordem = new OrdemDeServicoBuilder().Build();
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var consumer = new AprovarOrdemDeServicoConsumer(_repository.Object, _uow.Object, _metrics.Object);

        await consumer.Consume(FakeConsumeContext.For(new ExecuteAprovarOrdemDeServico(ordem.Id)));

        _metrics.Verify(m => m.ErroRegistrado("aprovar", "status_invalido"), Times.Once);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task Rejeitar_QuandoEmAguardandoAprovacao_DeveMoverParaRejeitada()
    {
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(2).Build();
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var consumer = new RejeitarOrdemDeServicoConsumer(_repository.Object, _uow.Object, _metrics.Object);

        await consumer.Consume(FakeConsumeContext.For(new ExecuteRejeitarOrdemDeServico(ordem.Id)));

        ordem.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.Rejeitada);
    }

    [Fact]
    public async Task Rejeitar_QuandoStatusDiferente_DeveRegistrarMetricaStatusInvalido()
    {
        var ordem = new OrdemDeServicoBuilder().Build();
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var consumer = new RejeitarOrdemDeServicoConsumer(_repository.Object, _uow.Object, _metrics.Object);

        await consumer.Consume(FakeConsumeContext.For(new ExecuteRejeitarOrdemDeServico(ordem.Id)));

        _metrics.Verify(m => m.ErroRegistrado("rejeitar", "status_invalido"), Times.Once);
    }

    [Fact]
    public async Task PagamentoAprovado_QuandoEmAguardandoPagamento_DeveAvancarParaPagamentoConfirmado()
    {
        // Recebida(0) → EmDiagnostico(1) → AguardandoAprovacao(2) → EmExecucao(3)
        // → ManutencaoFinalizada(4) → AguardandoPagamento(5)
        var ordem = new OrdemDeServicoBuilder().ComStatusAvancadoEm(5).Build();
        ordem.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.AguardandoPagamento);
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var consumer = new PagamentoAprovadoConsumer(_repository.Object, _uow.Object, _metrics.Object);

        await consumer.Consume(FakeConsumeContext.For(new ExecutePagamentoAprovado(ordem.Id)));

        ordem.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.PagamentoConfirmado);
        _repository.Verify(r => r.UpdateAsync(ordem), Times.Once);
    }

    [Fact]
    public async Task PagamentoAprovado_QuandoStatusDiferente_DeveRegistrarMetricaStatusInvalido()
    {
        var ordem = new OrdemDeServicoBuilder().Build();
        _repository.Setup(r => r.GetByIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var consumer = new PagamentoAprovadoConsumer(_repository.Object, _uow.Object, _metrics.Object);

        await consumer.Consume(FakeConsumeContext.For(new ExecutePagamentoAprovado(ordem.Id)));

        _metrics.Verify(m => m.ErroRegistrado("pagamento_aprovado", "status_invalido"), Times.Once);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task Create_ComIdValido_DevePersistirOrdemDeServicoNova()
    {
        var consumer = new CreateOrdemDeServicoConsumer(_repository.Object, _uow.Object, _metrics.Object);
        var msg = new ExecuteCreateOrdemDeServico(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Motor", "Obs");

        await consumer.Consume(FakeConsumeContext.For(msg));

        _repository.Verify(r => r.AddAsync(It.IsAny<OrdemDeServico>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ComIdVazio_DeveRegistrarMetricaValidacao()
    {
        var consumer = new CreateOrdemDeServicoConsumer(_repository.Object, _uow.Object, _metrics.Object);
        var msg = new ExecuteCreateOrdemDeServico(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Motor", "Obs");

        await consumer.Consume(FakeConsumeContext.For(msg));

        _metrics.Verify(m => m.ErroRegistrado("criar", "validacao"), Times.Once);
        _repository.Verify(r => r.AddAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }
}

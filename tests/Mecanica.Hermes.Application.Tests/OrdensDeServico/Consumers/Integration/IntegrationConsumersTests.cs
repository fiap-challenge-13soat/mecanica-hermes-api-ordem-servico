using MassTransit;
using Mecanica.Hermes.Application.OrdensDeServico.Consumers.Integration;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Integration;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

namespace Mecanica.Hermes.Application.Tests.OrdensDeServico.Consumers.Integration;

[Trait("Category", "Unit")]
public class IntegrationConsumersTests
{
    private static Mock<ConsumeContext<T>> BuildContext<T>(T message) where T : class
    {
        var mock = new Mock<ConsumeContext<T>>();
        mock.SetupGet(c => c.Message).Returns(message);
        mock.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return mock;
    }

    [Fact]
    public async Task OrcamentoAprovadoPeloCliente_Publica_AprovarOrdemDeServicoRequest()
    {
        var id = Guid.NewGuid();
        AprovarOrdemDeServicoRequest? published = null;
        var mock = BuildContext(new OrcamentoAprovadoPeloClienteEvent(id, DateTime.UtcNow));
        mock.Setup(c => c.Publish(It.IsAny<AprovarOrdemDeServicoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AprovarOrdemDeServicoRequest, CancellationToken>((msg, _) => published = msg)
            .Returns(Task.CompletedTask);

        await new OrcamentoAprovadoPeloClienteConsumer().Consume(mock.Object);

        published.Should().NotBeNull();
        published!.OrdemDeServicoId.Should().Be(id);
    }

    [Fact]
    public async Task OrcamentoRejeitadoPeloCliente_Publica_RejeitarOrdemDeServicoRequest()
    {
        var id = Guid.NewGuid();
        RejeitarOrdemDeServicoRequest? published = null;
        var mock = BuildContext(new OrcamentoRejeitadoPeloClienteEvent(id, DateTime.UtcNow, "Motivo"));
        mock.Setup(c => c.Publish(It.IsAny<RejeitarOrdemDeServicoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RejeitarOrdemDeServicoRequest, CancellationToken>((msg, _) => published = msg)
            .Returns(Task.CompletedTask);

        await new OrcamentoRejeitadoPeloClienteConsumer().Consume(mock.Object);

        published.Should().NotBeNull();
        published!.OrdemDeServicoId.Should().Be(id);
    }

    [Fact]
    public async Task PagamentoConfirmado_Publica_PagamentoAprovadoRequest()
    {
        var id = Guid.NewGuid();
        PagamentoAprovadoRequest? published = null;
        var mock = BuildContext(new PagamentoConfirmadoEvent(id, Guid.NewGuid(), DateTime.UtcNow));
        mock.Setup(c => c.Publish(It.IsAny<PagamentoAprovadoRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PagamentoAprovadoRequest, CancellationToken>((msg, _) => published = msg)
            .Returns(Task.CompletedTask);

        await new PagamentoConfirmadoConsumer().Consume(mock.Object);

        published.Should().NotBeNull();
        published!.OrdemDeServicoId.Should().Be(id);
    }

    [Fact]
    public async Task PagamentoRecusado_Publica_ReverterEtapaRequest()
    {
        var id = Guid.NewGuid();
        ReverterEtapaRequest? published = null;
        var mock = BuildContext(new PagamentoRecusadoEvent(id, Guid.NewGuid(), "Sem saldo", DateTime.UtcNow));
        mock.Setup(c => c.Publish(It.IsAny<ReverterEtapaRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ReverterEtapaRequest, CancellationToken>((msg, _) => published = msg)
            .Returns(Task.CompletedTask);

        await new PagamentoRecusadoConsumer().Consume(mock.Object);

        published.Should().NotBeNull();
        published!.OrdemDeServicoId.Should().Be(id);
    }
}

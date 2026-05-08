using MassTransit;
using Mecanica.Hermes.Contracts.Integration;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers.Integration;

public sealed class OrcamentoRejeitadoPeloClienteConsumer
    : IConsumer<OrcamentoRejeitadoPeloClienteEvent>
{
    public Task Consume(ConsumeContext<OrcamentoRejeitadoPeloClienteEvent> context)
        => context.Publish(new RejeitarOrdemDeServicoRequest(context.Message.OrdemDeServicoId));
}

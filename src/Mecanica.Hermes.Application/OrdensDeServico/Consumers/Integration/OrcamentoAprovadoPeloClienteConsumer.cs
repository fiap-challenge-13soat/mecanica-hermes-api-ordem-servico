using MassTransit;
using Mecanica.Hermes.Contracts.Integration;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers.Integration;

public sealed class OrcamentoAprovadoPeloClienteConsumer
    : IConsumer<OrcamentoAprovadoPeloClienteEvent>
{
    public Task Consume(ConsumeContext<OrcamentoAprovadoPeloClienteEvent> context)
        => context.Publish(new AprovarOrdemDeServicoRequest(context.Message.OrdemDeServicoId));
}

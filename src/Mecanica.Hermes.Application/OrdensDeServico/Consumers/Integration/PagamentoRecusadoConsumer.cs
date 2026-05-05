using MassTransit;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Integration;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers.Integration;

public sealed class PagamentoRecusadoConsumer
    : IConsumer<PagamentoRecusadoEvent>
{
    public Task Consume(ConsumeContext<PagamentoRecusadoEvent> context)
        => context.Publish(new ReverterEtapaRequest(context.Message.OrdemDeServicoId));
}

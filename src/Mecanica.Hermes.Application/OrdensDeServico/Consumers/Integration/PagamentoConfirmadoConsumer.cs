using MassTransit;
using Mecanica.Hermes.Contracts.Integration;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers.Integration;

public sealed class PagamentoConfirmadoConsumer
    : IConsumer<PagamentoConfirmadoEvent>
{
    public Task Consume(ConsumeContext<PagamentoConfirmadoEvent> context)
        => context.Publish(new PagamentoAprovadoRequest(context.Message.OrdemDeServicoId));
}

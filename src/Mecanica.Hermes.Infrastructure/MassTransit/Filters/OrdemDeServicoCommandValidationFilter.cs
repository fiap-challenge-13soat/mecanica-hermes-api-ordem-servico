using MassTransit;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Microsoft.Extensions.Logging;

namespace Mecanica.Hermes.Infrastructure.MassTransit.Filters;

/// <summary>
///     Filtro de consumo (consume pipe filter) que valida invariantes básicas de qualquer
///     <see cref="IOrdemDeServicoExecuteCommand" /> antes de entregar ao consumer concreto.
///     Garante que <c>OrdemDeServicoId != Guid.Empty</c>. Caso contrário, a mensagem é
///     descartada com log de erro sem ser recolocada na fila — mensagens com ID inválido são
///     determinísticas e nunca serão válidas após retry.
/// </summary>
internal sealed class OrdemDeServicoCommandValidationFilter<T>(ILogger<OrdemDeServicoCommandValidationFilter<T>> logger)
    : IFilter<ConsumeContext<T>>
    where T : class, IOrdemDeServicoExecuteCommand
{
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        if (context.Message.OrdemDeServicoId == Guid.Empty)
        {
            logger.LogError(
                "Mensagem {MessageType} recebida com OrdemDeServicoId vazio. Mensagem descartada.",
                typeof(T).Name);
            return;
        }

        await next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("ordemDeServicoCommandValidation");
    }
}

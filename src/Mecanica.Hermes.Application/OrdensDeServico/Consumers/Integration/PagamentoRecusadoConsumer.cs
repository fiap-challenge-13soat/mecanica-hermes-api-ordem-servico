using MassTransit;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Contracts.Integration;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Microsoft.Extensions.Logging;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers.Integration;

/// <summary>
///     Consumidor cross-service do evento de pagamento recusado/expirado publicado pela API
///     de Pagamentos.
///     <para>
///         <b>Idempotência cross-service:</b> só reverte a OS de
///         <see cref="OrdemDeServicoStatus.AguardandoPagamento" /> para
///         <see cref="OrdemDeServicoStatus.ManutencaoFinalizada" /> se estiver realmente em
///         <see cref="OrdemDeServicoStatus.AguardandoPagamento" />. Em qualquer outro estado,
///         o evento é ignorado.
///     </para>
/// </summary>
public sealed class PagamentoRecusadoConsumer(
    IOrdemDeServicoReader reader,
    IOrdemDeServicoMetrics metrics,
    ILogger<PagamentoRecusadoConsumer> logger)
    : IConsumer<PagamentoRecusadoEvent>
{
    private const string EventoEntityName = "pagamento.recusado.v1";
    private const OrdemDeServicoStatus EstadoFonteEsperado = OrdemDeServicoStatus.AguardandoPagamento;

    public async Task Consume(ConsumeContext<PagamentoRecusadoEvent> context)
    {
        var ordemId = context.Message.OrdemDeServicoId;
        var ordem = await reader.GetByIdAsync(ordemId);

        if (ordem is null)
        {
            logger.LogWarning(
                "Evento {Evento} ignorado: ordem {OrdemId} não encontrada na base de OS.",
                EventoEntityName, ordemId);
            return;
        }

        var statusAtual = ordem.StatusAtual.StatusAtual;
        if (statusAtual != EstadoFonteEsperado)
        {
            metrics.EventoIntegracaoIgnorado(EventoEntityName, statusAtual.ToString());
            logger.LogInformation(
                "Evento {Evento} ignorado por idempotência: OS {OrdemId} está em {StatusAtual}, " +
                "esperado {EstadoFonte} para reverter pagamento. Provável redelivery do webhook MP.",
                EventoEntityName, ordemId, statusAtual, EstadoFonteEsperado);
            return;
        }

        await context.Publish(new ReverterEtapaRequest(ordemId));
    }
}

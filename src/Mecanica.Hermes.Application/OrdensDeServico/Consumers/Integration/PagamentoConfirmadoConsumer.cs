using MassTransit;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Contracts.Integration;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Microsoft.Extensions.Logging;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers.Integration;

/// <summary>
///     Consumidor cross-service do evento de pagamento confirmado publicado pela API de Pagamentos
///     (após webhook do Mercado Pago).
///     <para>
///         <b>Idempotência cross-service:</b> só promove a OS para
///         <see cref="OrdemDeServicoStatus.PagamentoConfirmado" /> se estiver em
///         <see cref="OrdemDeServicoStatus.AguardandoPagamento" />. Em qualquer outro estado,
///         o evento é ignorado. Cenário típico: Mercado Pago redelivera o webhook após nosso
///         primeiro processamento já ter avançado a OS.
///     </para>
/// </summary>
public sealed class PagamentoConfirmadoConsumer(
    IOrdemDeServicoReader reader,
    IOrdemDeServicoMetrics metrics,
    ILogger<PagamentoConfirmadoConsumer> logger)
    : IConsumer<PagamentoConfirmadoEvent>
{
    private const string EventoEntityName = "pagamento.confirmado.v1";
    private const OrdemDeServicoStatus EstadoFonteEsperado = OrdemDeServicoStatus.AguardandoPagamento;

    public async Task Consume(ConsumeContext<PagamentoConfirmadoEvent> context)
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
                "esperado {EstadoFonte} para confirmar pagamento. Provável redelivery do webhook MP.",
                EventoEntityName, ordemId, statusAtual, EstadoFonteEsperado);
            return;
        }

        await context.Publish(new PagamentoAprovadoRequest(ordemId));
    }
}

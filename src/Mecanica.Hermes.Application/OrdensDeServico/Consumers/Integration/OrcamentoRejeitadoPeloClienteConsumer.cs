using MassTransit;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Contracts.Integration;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Microsoft.Extensions.Logging;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers.Integration;

/// <summary>
///     Consumidor cross-service do evento de rejeição de orçamento publicado pela API de Cadastros.
///     <para>
///         <b>Idempotência cross-service:</b> só promove a OS para <see cref="OrdemDeServicoStatus.Rejeitada" />
///         se ela estiver em <see cref="OrdemDeServicoStatus.AguardandoAprovacao" />. Em qualquer
///         outro estado (já rejeitada, já aprovada, cancelada), o evento é ignorado e contabilizado
///         em <c>ordemdeservico.evento_integracao_ignorado</c>.
///     </para>
/// </summary>
public sealed partial class OrcamentoRejeitadoPeloClienteConsumer(
    IOrdemDeServicoReader reader,
    IOrdemDeServicoMetrics metrics,
    ILogger<OrcamentoRejeitadoPeloClienteConsumer> logger)
    : IConsumer<OrcamentoRejeitadoPeloClienteEvent>
{
    private const string EventoEntityName = "orcamento.rejeitado-pelo-cliente.v1";
    private const OrdemDeServicoStatus EstadoFonteEsperado = OrdemDeServicoStatus.AguardandoAprovacao;

    public async Task Consume(ConsumeContext<OrcamentoRejeitadoPeloClienteEvent> context)
    {
        var ordemId = context.Message.OrdemDeServicoId;
        var ordem = await reader.GetByIdAsync(ordemId);

        if (ordem is null)
        {
            LogOrdemNaoEncontrada(logger, EventoEntityName, ordemId);
            return;
        }

        var statusAtual = ordem.StatusAtual.StatusAtual;
        if (statusAtual != EstadoFonteEsperado)
        {
            metrics.EventoIntegracaoIgnorado(EventoEntityName, statusAtual.ToString());
            LogIgnoradoPorIdempotencia(logger, EventoEntityName, ordemId, statusAtual, EstadoFonteEsperado);
            return;
        }

        await context.Publish(new RejeitarOrdemDeServicoRequest(ordemId), context.CancellationToken);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Evento {Evento} ignorado: ordem {OrdemId} não encontrada na base de OS.")]
    private static partial void LogOrdemNaoEncontrada(ILogger logger, string evento, Guid ordemId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Evento {Evento} ignorado por idempotência: OS {OrdemId} está em {StatusAtual}, "
                  + "esperado {EstadoFonte} para rejeitar. Provável entrega tardia/duplicada.")]
    private static partial void LogIgnoradoPorIdempotencia(
        ILogger logger,
        string evento,
        Guid ordemId,
        OrdemDeServicoStatus statusAtual,
        OrdemDeServicoStatus estadoFonte);
}

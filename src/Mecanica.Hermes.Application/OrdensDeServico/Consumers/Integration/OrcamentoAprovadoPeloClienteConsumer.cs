using MassTransit;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Contracts.Integration;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Microsoft.Extensions.Logging;

namespace Mecanica.Hermes.Application.OrdensDeServico.Consumers.Integration;

/// <summary>
///     Consumidor cross-service do evento de aprovação de orçamento publicado pela API de
///     Cadastros (via webhook HMAC + Outbox transacional).
///     <para>
///         <b>Idempotência cross-service:</b> antes de publicar o <see cref="AprovarOrdemDeServicoRequest" />
///         à SAGA, verifica se a OS está realmente em <see cref="OrdemDeServicoStatus.AguardandoAprovacao" />.
///         Se já está em estado-destino (ou em estado terminal — Cancelada, Entregue, Rejeitada), o evento
///         é ignorado e contabilizado em <c>ordemdeservico.evento_integracao_ignorado</c>. Mitiga o cenário
///         de entrega tardia/duplicada após o cliente reaprovar via novo link de e-mail ou RabbitMQ
///         redeliver após restart.
///     </para>
/// </summary>
public sealed class OrcamentoAprovadoPeloClienteConsumer(
    IOrdemDeServicoReader reader,
    IOrdemDeServicoMetrics metrics,
    ILogger<OrcamentoAprovadoPeloClienteConsumer> logger)
    : IConsumer<OrcamentoAprovadoPeloClienteEvent>
{
    private const string EventoEntityName = "orcamento.aprovado-pelo-cliente.v1";
    private const OrdemDeServicoStatus EstadoFonteEsperado = OrdemDeServicoStatus.AguardandoAprovacao;

    public async Task Consume(ConsumeContext<OrcamentoAprovadoPeloClienteEvent> context)
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
                "esperado {EstadoFonte} para promover. Provável entrega tardia/duplicada.",
                EventoEntityName, ordemId, statusAtual, EstadoFonteEsperado);
            return;
        }

        await context.Publish(new AprovarOrdemDeServicoRequest(ordemId));
    }
}

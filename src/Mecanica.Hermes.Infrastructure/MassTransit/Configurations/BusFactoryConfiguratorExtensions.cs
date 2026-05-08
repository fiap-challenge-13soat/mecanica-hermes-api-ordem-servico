using System.Diagnostics.CodeAnalysis;
using MassTransit;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Infrastructure.MassTransit.Filters;
using Mecanica.Hermes.Infrastructure.MassTransit.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mecanica.Hermes.Infrastructure.MassTransit.Configurations;

[ExcludeFromCodeCoverage(Justification =
    "Configuração de retry/redelivery do MassTransit — exercitada pelo broker em produção/integração.")]
internal static class BusFactoryConfiguratorExtensions
{
    /// <summary>
    ///     Política de retry imediato para erros transientes (timeouts em I/O, conexão com broker etc).
    ///     Mantém as tentativas curtas: 3x com 2s de intervalo. Erros de regra de domínio NÃO são
    ///     jogados como exception nos consumers — eles são publicados como
    ///     <c>OrdemDeServicoOperationFailed</c>, então não devem ser captados aqui.
    /// </summary>
    private const int ImmediateRetryCount = 3;

    /// <summary>
    ///     Redelivery (volta a mensagem para o broker com delay) é mantido apenas para falhas
    ///     de infraestrutura mais persistentes. Limite curto para evitar loop de mensagens
    ///     "envenenadas".
    /// </summary>
    private const int DelayedRetryLimit = 3;

    private static readonly TimeSpan ImmediateRetryInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan DelayedMinInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan DelayedMaxInterval = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan DelayedIntervalDelta = TimeSpan.FromSeconds(15);

    public static void ConfigureCommonFactory(this IBusFactoryConfigurator configurator,
        IBusRegistrationContext registration)
    {
        var options = registration.GetRequiredService<IOptions<MassTransitOptions>>().Value;

        configurator.PrefetchCount = options.MassTransitPrefetchCount;

        configurator.UseMessageRetry(r =>
        {
            // Erros de validação/argumento NUNCA devem retentar — são determinísticos.
            r.Ignore<ArgumentException>();
            r.Ignore<InvalidOperationException>();

            // Apenas erros transientes de infra fazem retry imediato.
            r.Handle<TimeoutException>();
            r.Handle<HttpRequestException>();
            r.Interval(ImmediateRetryCount, ImmediateRetryInterval);
        });

        configurator.UseDelayedRedelivery(r =>
        {
            r.Ignore<ArgumentException>();
            r.Ignore<InvalidOperationException>();

            r.Handle<TimeoutException>();
            r.Handle<HttpRequestException>();
            r.Exponential(
                DelayedRetryLimit,
                DelayedMinInterval,
                DelayedMaxInterval,
                DelayedIntervalDelta);
        });

        // Outbox em memória — garante "tudo ou nada" entre publish + commit no consumer.
        configurator.UseInMemoryOutbox(registration);

        // Filtro de validação: garante OrdemDeServicoId != Guid.Empty antes de qualquer consumer
        // processar um IOrdemDeServicoExecuteCommand. Mensagens inválidas são descartadas sem retry.
        configurator.UseConsumeFilter(typeof(OrdemDeServicoCommandValidationFilter<>), registration,
            x => x.Include(type => typeof(IOrdemDeServicoExecuteCommand).IsAssignableFrom(type)));

        // DLQ observability: hooka o ConsumeFault depois que retry+redelivery esgotam.
        // Emite métrica `messaging.consume_faults` e log estruturado, permitindo alarmar quando
        // mensagens vão em massa para o _error queue.
        configurator.ConnectConsumeObserver(registration.GetRequiredService<MessagingFaultObserver>());
    }
}

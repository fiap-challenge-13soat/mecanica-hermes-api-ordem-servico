using System.Diagnostics.CodeAnalysis;
using MassTransit;
using Mecanica.Hermes.Application.Common.Messaging;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Mecanica.Hermes.Infrastructure.MassTransit.Adapters;

/// <summary>
///     Adapter que implementa o port <see cref="IAsyncMessagePublisher" /> sobre o
///     <see cref="IPublishEndpoint" /> do MassTransit com retry Polly para falhas
///     transitórias do broker. Retorna 503 (via <c>ServiceUnavailableException</c>)
///     após esgotar todas as tentativas, permitindo ao caller retornar HTTP 503.
///     Retry policy: 3 tentativas, backoff exponencial (200ms → 400ms → 800ms).
///     Captura apenas <see cref="Exception" /> genérico para cobertura de falhas de
///     conexão RabbitMQ, timeouts e afins.
/// </summary>
internal sealed class MassTransitMessagePublisher(
    IPublishEndpoint publishEndpoint,
    ILogger<MassTransitMessagePublisher> logger)
    : IAsyncMessagePublisher
{
    private static readonly ResiliencePipeline RetryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                ex is not OperationCanceledException)
        })
        .Build();

    public async Task Publish(object message, CancellationToken cancellationToken)
    {
        try
        {
            await RetryPipeline.ExecuteAsync(
                async ct => await publishEndpoint.Publish(message, ct),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex,
                "Falha ao publicar mensagem {MessageType} após todas as tentativas. O broker pode estar indisponível.",
                message.GetType().Name);
            throw new BrokerUnavailableException(
                $"Não foi possível enfileirar a mensagem {message.GetType().Name}. Tente novamente em instantes.", ex);
        }
    }
}

/// <summary>
///     Lançada quando o broker de mensagens está indisponível após todas as tentativas de retry.
///     Mapeada para HTTP 503 pelo GlobalExceptionHandlerMiddleware.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Exception sem lógica — apenas transporte de mensagem.")]
public sealed class BrokerUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);

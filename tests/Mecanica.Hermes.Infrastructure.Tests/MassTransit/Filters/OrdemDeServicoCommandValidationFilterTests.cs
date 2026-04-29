using MassTransit;
using MassTransit.Testing;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Infrastructure.MassTransit.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mecanica.Hermes.Infrastructure.Tests.MassTransit.Filters;

/// <summary>
/// Testes do <see cref="OrdemDeServicoCommandValidationFilter{T}"/> usando consumer stub.
/// Verifica que mensagens com <c>OrdemDeServicoId == Guid.Empty</c> são descartadas antes
/// de chegar ao consumer concreto, e que mensagens válidas atravessam normalmente.
/// </summary>
[Trait("Category", "Unit")]
public class OrdemDeServicoCommandValidationFilterTests
{
    private static async Task<(ITestHarness harness, ServiceProvider provider)> StartHarnessAsync()
    {
        var provider = new ServiceCollection()
            .AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>))
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<StubExecuteCommandConsumer>();
                cfg.UsingInMemory((ctx, bus) =>
                {
                    bus.UseConsumeFilter(typeof(OrdemDeServicoCommandValidationFilter<>), ctx,
                        x => x.Include(type => typeof(IOrdemDeServicoExecuteCommand).IsAssignableFrom(type)));
                    bus.ConfigureEndpoints(ctx);
                });
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        return (harness, provider);
    }

    [Fact]
    public async Task MensagemComIdValido_DeveSerProcessadaPeloConsumer()
    {
        var (harness, provider) = await StartHarnessAsync();
        await using var _ = provider;

        var consumerHarness = harness.GetConsumerHarness<StubExecuteCommandConsumer>();

        await harness.Bus.Publish(new StubExecuteCommand(Guid.NewGuid()));

        (await consumerHarness.Consumed.Any<StubExecuteCommand>()).Should().BeTrue(
            "mensagem com OrdemDeServicoId válido deve ser entregue ao consumer");
    }

    [Fact]
    public async Task MensagemComIdVazio_DeveSerDescartadaSemChamarConsumer()
    {
        var (harness, provider) = await StartHarnessAsync();
        await using var _ = provider;

        var consumerHarness = harness.GetConsumerHarness<StubExecuteCommandConsumer>();

        await harness.Bus.Publish(new StubExecuteCommand(Guid.Empty));

        // Aguarda o bus processar
        await Task.Delay(200);

        consumerHarness.Consumed.Select<StubExecuteCommand>().Count().Should().Be(0,
            "mensagem com OrdemDeServicoId == Guid.Empty deve ser descartada pelo filtro");
    }

    // ── Stubs internos ao teste ──────────────────────────────────────────────

    private record StubExecuteCommand(Guid OrdemDeServicoId) : IOrdemDeServicoExecuteCommand;

    private class StubExecuteCommandConsumer : IConsumer<StubExecuteCommand>
    {
        public Task Consume(ConsumeContext<StubExecuteCommand> context) => Task.CompletedTask;
    }
}

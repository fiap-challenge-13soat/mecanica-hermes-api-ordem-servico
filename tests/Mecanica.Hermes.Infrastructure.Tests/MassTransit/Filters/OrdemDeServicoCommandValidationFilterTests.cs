using MassTransit;
using MassTransit.Testing;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Infrastructure.MassTransit.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mecanica.Hermes.Infrastructure.Tests.MassTransit.Filters;

/// <summary>
///     Testes do <see cref="OrdemDeServicoCommandValidationFilter{T}" /> usando consumer stub.
///     Verifica que mensagens com <c>OrdemDeServicoId == Guid.Empty</c> são descartadas antes
///     de chegar ao consumer concreto, e que mensagens válidas atravessam normalmente.
/// </summary>
[Trait("Category", "Unit")]
public class OrdemDeServicoCommandValidationFilterTests
{
    private static async Task<(ITestHarness harness, ServiceProvider provider)> StartHarnessAsync()
    {
        var provider = new ServiceCollection()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
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

        await Task.WhenAny(harness.InactivityTask, Task.Delay(TimeSpan.FromSeconds(3)));

        consumerHarness.Consumed.Select<StubExecuteCommand>().Count().Should().Be(0,
            "mensagem com OrdemDeServicoId == Guid.Empty deve ser descartada pelo filtro");
    }

    [Fact]
    public void Probe_DeveCriarFilterScopeNoContexto()
    {
        var filter =
            new OrdemDeServicoCommandValidationFilter<StubExecuteCommand>(
                NullLogger<OrdemDeServicoCommandValidationFilter<StubExecuteCommand>>.Instance);

        // CreateFilterScope é um método de extensão sobre ProbeContext, então usamos
        // um stub recording em vez de Moq (extension methods não são mockáveis).
        var probe = new RecordingProbeContext();

        filter.Probe(probe);

        // CreateFilterScope cria um scope "filter" e registra "ordemDeServicoCommandValidation"
        // como o tipo do filtro — verificamos ambos para garantir cobertura do método Probe.
        probe.CreatedScopes.Should().NotBeEmpty();
        probe.Values.Values.Should().Contain("ordemDeServicoCommandValidation");
    }

    private sealed class RecordingProbeContext : ProbeContext
    {
        public List<string> CreatedScopes { get; } = [];
        public Dictionary<string, object> Values { get; } = [];
        public CancellationToken CancellationToken => CancellationToken.None;

        public ProbeContext CreateScope(string id)
        {
            CreatedScopes.Add(id);
            return this;
        }

        public void Add(string key, object value)
        {
            Values[key] = value;
        }

        public void Add(string key, string value)
        {
            Values[key] = value;
        }

        public void Set(object values)
        {
        }

        public void Set(IEnumerable<KeyValuePair<string, object>> values)
        {
        }
    }

    // -- Stubs internos ao teste --

    private sealed record StubExecuteCommand(Guid OrdemDeServicoId) : IOrdemDeServicoExecuteCommand;

    private sealed class StubExecuteCommandConsumer : IConsumer<StubExecuteCommand>
    {
        public Task Consume(ConsumeContext<StubExecuteCommand> context)
        {
            return Task.CompletedTask;
        }
    }
}

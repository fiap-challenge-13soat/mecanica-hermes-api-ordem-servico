using MassTransit;
using MassTransit.Testing;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Commands;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Events;
using Mecanica.Hermes.Application.OrdensDeServico.Messages.Requests;
using Mecanica.Hermes.Infrastructure.MassTransit.Saga;
using Microsoft.Extensions.DependencyInjection;

namespace Mecanica.Hermes.Infrastructure.Tests.MassTransit.Saga;

/// <summary>
/// Testes da <see cref="OrdemDeServicoStateMachine"/> usando <see cref="ITestHarness"/>
/// + <see cref="ISagaStateMachineTestHarness{TStateMachine,TInstance}"/>. Cobrem as
/// transições críticas: Initial → Processing → Initial e a propriedade de "no máximo
/// uma operação por agregado" (segunda Request em Processing é ignorada).
/// </summary>
[Trait("Category", "Unit")]
public class OrdemDeServicoStateMachineTests
{
    private static async Task<(ITestHarness harness, ISagaStateMachineTestHarness<OrdemDeServicoStateMachine, OrdemDeServicoState> saga, ServiceProvider provider)>
        StartHarnessAsync()
    {
        var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<OrdemDeServicoStateMachine, OrdemDeServicoState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var saga = harness.GetSagaStateMachineHarness<OrdemDeServicoStateMachine, OrdemDeServicoState>();
        return (harness, saga, provider);
    }

    [Fact]
    public async Task Request_NoEstadoInitial_DeveTransicionarParaProcessingEPublicarExecuteCommand()
    {
        var (harness, saga, provider) = await StartHarnessAsync();
        await using var _ = provider;

        var ordemId = Guid.NewGuid();
        await harness.Bus.Publish(new AvancarEtapaRequest(ordemId));

        // Aguarda saga consumir a request
        (await saga.Consumed.Any<AvancarEtapaRequest>()).Should().BeTrue();
        // Saga publica o ExecuteAvancarEtapa
        (await harness.Published.Any<ExecuteAvancarEtapa>()).Should().BeTrue();

        // Estado da instância deve ser "Processing"
        var instance = saga.Created.ContainsInState(ordemId, saga.StateMachine, saga.StateMachine.Processing);
        instance.Should().NotBeNull();
        instance!.OperacaoEmAndamento.Should().Be("AvancarEtapaRequested");
    }

    [Fact]
    public async Task SegundaRequest_DuranteProcessing_DeveSerIgnorada()
    {
        var (harness, saga, provider) = await StartHarnessAsync();
        await using var _ = provider;

        var ordemId = Guid.NewGuid();

        await harness.Bus.Publish(new AvancarEtapaRequest(ordemId));
        (await saga.Consumed.Any<AvancarEtapaRequest>()).Should().BeTrue();

        // Segunda request enquanto SAGA está em Processing
        await harness.Bus.Publish(new AvancarEtapaRequest(ordemId));

        // SAGA processa duas requests, mas só uma ExecuteAvancarEtapa deve ter sido publicada
        var executeCount = harness.Published.Select<ExecuteAvancarEtapa>().Count();
        executeCount.Should().Be(1, "segunda request em Processing é ignorada (não dispara novo Execute)");
    }

    [Fact]
    public async Task OperationCompleted_EmProcessing_DeveRetornarParaInitialELimparEstado()
    {
        var (harness, saga, provider) = await StartHarnessAsync();
        await using var _ = provider;

        var ordemId = Guid.NewGuid();

        await harness.Bus.Publish(new AvancarEtapaRequest(ordemId));
        (await saga.Consumed.Any<AvancarEtapaRequest>()).Should().BeTrue();

        await harness.Bus.Publish(new OrdemDeServicoOperationCompleted(ordemId, "ExecuteAvancarEtapa"));
        (await saga.Consumed.Any<OrdemDeServicoOperationCompleted>()).Should().BeTrue();

        var instance = saga.Created.ContainsInState(ordemId, saga.StateMachine, saga.StateMachine.Initial);
        instance.Should().NotBeNull();
        instance!.OperacaoEmAndamento.Should().BeNull();
        instance.UltimoMotivoFalha.Should().BeNull();
    }

    [Fact]
    public async Task OperationFailed_EmProcessing_DeveRetornarParaInitialEArmazenarMotivo()
    {
        var (harness, saga, provider) = await StartHarnessAsync();
        await using var _ = provider;

        var ordemId = Guid.NewGuid();

        await harness.Bus.Publish(new AvancarEtapaRequest(ordemId));
        (await saga.Consumed.Any<AvancarEtapaRequest>()).Should().BeTrue();

        await harness.Bus.Publish(new OrdemDeServicoOperationFailed(ordemId, "ExecuteAvancarEtapa", "status invalido"));
        (await saga.Consumed.Any<OrdemDeServicoOperationFailed>()).Should().BeTrue();

        var instance = saga.Created.ContainsInState(ordemId, saga.StateMachine, saga.StateMachine.Initial);
        instance.Should().NotBeNull();
        instance!.UltimoMotivoFalha.Should().Be("status invalido");
    }

    [Fact]
    public async Task OperationCompleted_EmInitial_DeveSerSilenciosamenteIgnorado()
    {
        var (harness, _, provider) = await StartHarnessAsync();
        await using var _provider = provider;

        var ordemId = Guid.NewGuid();

        // Sem nenhuma request prévia (saga ainda não existe), publicar Completed.
        // OnMissingInstance(Discard) impede a criação de uma nova instância.
        await harness.Bus.Publish(new OrdemDeServicoOperationCompleted(ordemId, "ExecuteAvancarEtapa"));

        // Espera o bus drenar
        await Task.Delay(100);

        // Nenhum Execute deve ter sido publicado
        harness.Published.Select<ExecuteAvancarEtapa>().Count().Should().Be(0);
    }

    [Fact]
    public async Task Request_DevePreencherOperationTimeoutTokenId()
    {
        var (harness, saga, provider) = await StartHarnessAsync();
        await using var _ = provider;

        var ordemId = Guid.NewGuid();
        await harness.Bus.Publish(new AvancarEtapaRequest(ordemId));
        (await saga.Consumed.Any<AvancarEtapaRequest>()).Should().BeTrue();

        var instance = saga.Created.ContainsInState(ordemId, saga.StateMachine, saga.StateMachine.Processing);
        instance.Should().NotBeNull();
        instance!.OperationTimeoutTokenId.Should().NotBeNull(
            "o token do timeout sentinel deve ser preenchido ao entrar em Processing");
    }

    [Fact]
    public async Task OperationCompleted_DeveZerarOperationTimeoutTokenId()
    {
        var (harness, saga, provider) = await StartHarnessAsync();
        await using var _ = provider;

        var ordemId = Guid.NewGuid();

        await harness.Bus.Publish(new AvancarEtapaRequest(ordemId));
        (await saga.Consumed.Any<AvancarEtapaRequest>()).Should().BeTrue();

        await harness.Bus.Publish(new OrdemDeServicoOperationCompleted(ordemId, "ExecuteAvancarEtapa"));
        (await saga.Consumed.Any<OrdemDeServicoOperationCompleted>()).Should().BeTrue();

        var instance = saga.Created.ContainsInState(ordemId, saga.StateMachine, saga.StateMachine.Initial);
        instance.Should().NotBeNull();
        instance!.OperationTimeoutTokenId.Should().BeNull(
            "o Unschedule limpa o token ao finalizar a operação");
    }

    [Fact]
    public async Task OperationTimedOut_EmProcessing_DeveRetornarParaInitialComMotivoTimeout()
    {
        var (harness, saga, provider) = await StartHarnessAsync();
        await using var _ = provider;

        var ordemId = Guid.NewGuid();

        await harness.Bus.Publish(new AvancarEtapaRequest(ordemId));
        (await saga.Consumed.Any<AvancarEtapaRequest>()).Should().BeTrue();

        // Simula o disparo do timeout publicando diretamente a mensagem sentinel
        await harness.Bus.Publish(new OrdemDeServicoOperationTimedOut(ordemId, "AvancarEtapaRequested"));
        (await saga.Consumed.Any<OrdemDeServicoOperationTimedOut>()).Should().BeTrue();

        var instance = saga.Created.ContainsInState(ordemId, saga.StateMachine, saga.StateMachine.Initial);
        instance.Should().NotBeNull("a SAGA deve retornar para Initial após timeout");
        instance!.UltimoMotivoFalha.Should().StartWith("Timeout após",
            "o motivo de falha deve registrar o timeout da operação");
        instance.OperacaoEmAndamento.Should().BeNull();
    }
}

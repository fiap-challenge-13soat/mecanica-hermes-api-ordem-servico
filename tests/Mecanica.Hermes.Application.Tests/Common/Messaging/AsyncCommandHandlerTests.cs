using System.Net;
using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Application.OrdensDeServico.Commands;
using Mecanica.Hermes.Domain.Common.Results;

namespace Mecanica.Hermes.Application.Tests.Common.Messaging;

/// <summary>
/// Testa o <see cref="AsyncCommandHandler{TRequest}"/> genérico: publicação via
/// <see cref="IAsyncMessagePublisher"/> e retorno <see cref="Result.Accepted"/>.
///
/// Validação de OrdemDeServicoId vazio foi removida do handler — está coberta por
/// <c>OrdemDeServicoCommandValidationFilter</c> (consumer side) e futuramente por validators
/// de edge (FluentValidation). O handler é intencionalmente um thin publisher.
/// </summary>
[Trait("Category", "Unit")]
public class AsyncCommandHandlerTests
{
    private readonly Mock<IAsyncMessagePublisher> _publisher = new();

    private sealed record FakeMessage(Guid OrdemDeServicoId, string Payload);

    private sealed record FakeCommand(Guid OrdemDeServicoId, string Payload)
        : IAsyncCommand, IOrdemDeServicoCommand
    {
        public object ToMessage() => new FakeMessage(OrdemDeServicoId, Payload);
    }

    private sealed record CommandSemOrdemId(string Data) : IAsyncCommand
    {
        public object ToMessage() => new { Data };
    }

    [Fact]
    public async Task Handle_QuandoCommandValido_DevePublicarERetornarAccepted()
    {
        var handler = new AsyncCommandHandler<FakeCommand>(_publisher.Object);
        var command = new FakeCommand(Guid.NewGuid(), "payload");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Accepted);
        _publisher.Verify(p => p.Publish(
            It.Is<FakeMessage>(m => m.OrdemDeServicoId == command.OrdemDeServicoId && m.Payload == "payload"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_QuandoCommandNaoTemOrdemId_DevePublicarERetornarAccepted()
    {
        var handler = new AsyncCommandHandler<CommandSemOrdemId>(_publisher.Object);

        var result = await handler.Handle(new CommandSemOrdemId("x"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Accepted);
        _publisher.Verify(p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

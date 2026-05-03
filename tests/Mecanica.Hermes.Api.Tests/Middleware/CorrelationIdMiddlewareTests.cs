using Mecanica.Hermes.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace Mecanica.Hermes.Api.Tests.Middleware;

[Trait("Category", "Unit")]
public class CorrelationIdMiddlewareTests
{
    private const string HeaderName = "X-Correlation-ID";

    [Fact]
    public async Task InvokeAsync_QuandoHeaderPresente_DeveEcoarNoResponse()
    {
        var existing = "abc-123";
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderName] = existing;

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers[HeaderName].ToString().Should().Be(existing);
    }

    [Fact]
    public async Task InvokeAsync_QuandoHeaderAusente_DeveGerarNovoCorrelationId()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var generated = context.Response.Headers[HeaderName].ToString();
        generated.Should().NotBeNullOrEmpty();
        Guid.TryParse(generated, out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_DeveChamarProximoMiddleware()
    {
        var called = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(new DefaultHttpContext());

        called.Should().BeTrue();
    }
}

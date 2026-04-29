using System.Text.Json;
using Mecanica.Hermes.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mecanica.Hermes.Api.Tests.Middleware;

/// <summary>
/// Valida o mapeamento exceção → <c>ApiProblemDetails</c>/HTTP status no middleware global.
/// </summary>
[Trait("Category", "Unit")]
public class GlobalExceptionHandlerMiddlewareTests
{
    private static HttpContext BuildContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static async Task<(int status, JsonElement body)> InvokeAsync(
        Exception exception,
        string environmentName = "Production")
    {
        var env = new HostingEnvironment { EnvironmentName = environmentName };
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw exception,
            NullLogger<GlobalExceptionHandlerMiddleware>.Instance,
            env);

        var ctx = BuildContext();
        await middleware.InvokeAsync(ctx);

        ctx.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(ctx.Response.Body);
        return (ctx.Response.StatusCode, doc.RootElement.Clone());
    }

    [Fact]
    public async Task InvokeAsync_SemExcecao_DeveNaoMexerNoResponse()
    {
        var env = new HostingEnvironment { EnvironmentName = "Production" };
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => Task.CompletedTask,
            NullLogger<GlobalExceptionHandlerMiddleware>.Instance,
            env);

        var ctx = BuildContext();

        await middleware.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_DeveRetornar400()
    {
        var (status, body) = await InvokeAsync(new ArgumentException("bad"));

        status.Should().Be(400);
        body.GetProperty("status").GetInt32().Should().Be(400);
        body.GetProperty("title").GetString().Should().Contain("Argumento Inválido");
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_DeveRetornar401()
    {
        var (status, body) = await InvokeAsync(new UnauthorizedAccessException());

        status.Should().Be(401);
        body.GetProperty("title").GetString().Should().Contain("Não Autorizado");
    }

    [Fact]
    public async Task InvokeAsync_DbUpdateException_DeveRetornar500()
    {
        var (status, body) = await InvokeAsync(new DbUpdateException("erro persistência"));

        status.Should().Be(500);
        body.GetProperty("title").GetString().Should().Contain("Persistência");
    }

    [Fact]
    public async Task InvokeAsync_ExcecaoGenerica_DeveRetornar500()
    {
        var (status, body) = await InvokeAsync(new InvalidOperationException("boom"));

        status.Should().Be(500);
        body.GetProperty("status").GetInt32().Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_EmDevelopment_DeveIncluirStackTrace()
    {
        var (status, body) = await InvokeAsync(new InvalidOperationException("boom"), "Development");

        status.Should().Be(500);
        body.GetProperty("title").GetString().Should().Contain("InvalidOperationException");
    }

    [Fact]
    public async Task InvokeAsync_EmTesting_DeveIncluirStackTrace()
    {
        var (status, body) = await InvokeAsync(new InvalidOperationException("boom"), "Testing");

        status.Should().Be(500);
        body.GetProperty("title").GetString().Should().Contain("InvalidOperationException");
    }
}

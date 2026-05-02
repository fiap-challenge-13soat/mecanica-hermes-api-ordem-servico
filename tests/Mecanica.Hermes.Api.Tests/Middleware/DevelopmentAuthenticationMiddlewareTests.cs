using System.Security.Claims;
using Mecanica.Hermes.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Mecanica.Hermes.Api.Tests.Middleware;

/// <summary>
///     Cobre o middleware que injeta um <c>ClaimsPrincipal</c> em DEV/Testing,
///     incluindo o guard hard-fail em Production.
/// </summary>
[Trait("Category", "Unit")]
public class DevelopmentAuthenticationMiddlewareTests
{
    private static IHostEnvironment Env(string name)
    {
        return new HostingEnvironment { EnvironmentName = name };
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public async Task InvokeAsync_EmDevOuTesting_DeveInjetarClaimsAdminECliente(string envName)
    {
        var middleware = new DevelopmentAuthenticationMiddleware(_ => Task.CompletedTask, Env(envName));
        var ctx = new DefaultHttpContext();

        await middleware.InvokeAsync(ctx);

        ctx.User.Identity!.IsAuthenticated.Should().BeTrue();
        ctx.User.FindFirst("scope")!.Value.Should().NotBeNullOrEmpty();
        ctx.User.FindFirst(ClaimTypes.Email)!.Value.Should().Be("test@example.com");
    }

    [Fact]
    public async Task InvokeAsync_QuandoUsuarioJaAutenticado_NaoSobrescreve()
    {
        var middleware =
            new DevelopmentAuthenticationMiddleware(_ => Task.CompletedTask, Env(Environments.Development));
        var ctx = new DefaultHttpContext();
        var existing = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "Real")], "RealAuth"));
        ctx.User = existing;

        await middleware.InvokeAsync(ctx);

        ctx.User.Should().BeSameAs(existing);
    }

    [Fact]
    public void Construtor_EmProduction_DeveLancarInvalidOperationException()
    {
        var act = () => new DevelopmentAuthenticationMiddleware(_ => Task.CompletedTask, Env(Environments.Production));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Production*");
    }

    [Fact]
    public async Task InvokeAsync_DeveChamarProximoMiddleware()
    {
        var called = false;
        var middleware = new DevelopmentAuthenticationMiddleware(
            _ =>
            {
                called = true;
                return Task.CompletedTask;
            },
            Env(Environments.Development));

        await middleware.InvokeAsync(new DefaultHttpContext());

        called.Should().BeTrue();
    }
}

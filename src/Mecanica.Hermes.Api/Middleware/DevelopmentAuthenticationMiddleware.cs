using System.Security.Claims;
using Mecanica.Hermes.Infrastructure.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Mecanica.Hermes.Api.Middleware;

public class DevelopmentAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;

    public DevelopmentAuthenticationMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        // Guard de segurança defensivo: impede execução em ambientes != Development/Testing
        // mesmo se algum operador encadear o middleware por engano em Production.
        if (!environment.IsDevelopment() && environment.EnvironmentName != Mecanica.Hermes.Domain.Common.EnvironmentNames.Testing)
        {
            throw new InvalidOperationException(
                $"DevelopmentAuthenticationMiddleware foi instanciado em ambiente '{environment.EnvironmentName}'. " +
                "Este middleware concede acesso irrestrito e NUNCA deve ser usado em produção.");
        }

        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if ((_environment.IsDevelopment() || _environment.EnvironmentName == Mecanica.Hermes.Domain.Common.EnvironmentNames.Testing) &&
            context.User.Identity?.IsAuthenticated != true)
        {
            const string FallbackAdminScope = "mechermes-admin/admin";
            const string FallbackClienteScope = "mechermes-admin/client";

            var adminScope = string.IsNullOrWhiteSpace(AuthPolicies.OnlyAdminScope)
                ? FallbackAdminScope
                : AuthPolicies.OnlyAdminScope;
            var clienteScope = string.IsNullOrWhiteSpace(AuthPolicies.AllowClienteScope)
                ? FallbackClienteScope
                : AuthPolicies.AllowClienteScope;

            var scopeValue = string.Join(' ', adminScope, clienteScope);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "test-user-id"),
                new(ClaimTypes.Name, "Test User"),
                new(ClaimTypes.Email, "test@example.com"),
                new("scope", scopeValue)
            };

            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}

public static class DevelopmentAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseDevelopmentAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DevelopmentAuthenticationMiddleware>();
    }
}

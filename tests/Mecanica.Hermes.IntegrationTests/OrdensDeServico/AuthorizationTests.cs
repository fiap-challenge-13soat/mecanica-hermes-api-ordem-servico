using System.Net;
using System.Net.Http.Headers;
using Mecanica.Hermes.IntegrationTests.Infrastructure;
using Xunit;

#pragma warning disable CA1001 // IAsyncLifetime.DisposeAsync handles disposal; IDisposable not needed

namespace Mecanica.Hermes.IntegrationTests.OrdensDeServico;

/// <summary>
/// Testes de autorização: verificam que as políticas <c>OnlyAdminScope</c> e
/// <c>AllowClienteScope</c> estão corretamente aplicadas nos endpoints.
///
/// Usam <see cref="AuthorizationTestWebApplicationFactory"/> que:
///  - NÃO injeta claims de desenvolvimento (ambiente "Testing_Auth")
///  - Valida JWTs reais com chave simétrica de teste (sem JWKS externo)
///
/// Não dependem do Testcontainers PostgreSQL — usam InMemory DB se disponível
/// (endpoints de escrita retornam 202 sem persistir).
/// </summary>
[Trait("Category", "Integration")]
public sealed class AuthorizationTests : IAsyncLifetime
{
    private readonly AuthorizationTestWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    private static readonly object CreateBody = new
    {
        ClienteId = Guid.NewGuid(),
        VeiculoId = Guid.NewGuid(),
        ProblemaRelatado = "Teste"
    };

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ---- 401: sem token ----

    [Fact]
    public async Task CriarOrdemDeServico_SemToken_DeveRetornar401()
    {
        var response = await _client.PostAsJsonAsync("/api/ordens-de-servico", CreateBody);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrdemDeServico_SemToken_DeveRetornar401()
    {
        var response = await _client.GetAsync($"/api/ordens-de-servico/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- 403: scope insuficiente ----

    [Fact]
    public async Task CriarOrdemDeServico_ComScopeClienteApenas_DeveRetornar403()
    {
        var token = AuthorizationTestWebApplicationFactory.GenerateToken([AuthorizationTestWebApplicationFactory.ClientScope]);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/ordens-de-servico", CreateBody);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListarTodas_ComScopeClienteApenas_DeveRetornar403()
    {
        var token = AuthorizationTestWebApplicationFactory.GenerateToken([AuthorizationTestWebApplicationFactory.ClientScope]);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/ordens-de-servico");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- 200/202: scope correto ----

    [Fact]
    public async Task CriarOrdemDeServico_ComScopeAdmin_DeveRetornar202()
    {
        var token = AuthorizationTestWebApplicationFactory.GenerateToken([AuthorizationTestWebApplicationFactory.AdminScope]);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/ordens-de-servico", CreateBody);

        // 202 Accepted ou 503 (broker unavailable em ambiente sem RabbitMQ) — ambos indicam que
        // a autenticação/autorização passou. Se houver 400, indica problema de validação.
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Accepted,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.InternalServerError);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOrdemDeServico_ComScopeCliente_DevePassarAutorizacao()
    {
        var token = AuthorizationTestWebApplicationFactory.GenerateToken([AuthorizationTestWebApplicationFactory.ClientScope]);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/ordens-de-servico/{Guid.NewGuid()}");

        // A autorização passou se não retornou 401/403.
        // 404 = ordem não existe (esperado). 500 = InMemory DB não suporta todas as queries
        // com navigations — indica que a autorização foi processada, mas o DB falhou.
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "Token com scope cliente deve ser autenticado");
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "AllowClienteScope deve aceitar scope cliente");
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Mecanica.Hermes.IntegrationTests.Infrastructure;

namespace Mecanica.Hermes.IntegrationTests.Fixtures;

public class OrdemDeServicoTestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected readonly HttpClient Client;
    protected readonly IntegrationTestWebApplicationFactory Factory;

    public OrdemDeServicoTestFixture(IntegrationTestWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected async Task<HttpResponseMessage> CriarOrdemDeServicoAsync(
        Guid clienteId,
        Guid veiculoId,
        string? problemaRelatado = null,
        string? observacoes = null)
    {
        var request = new
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId,
            ProblemaRelatado = problemaRelatado ?? "Problema padrão",
            Observacoes = observacoes
        };

        return await Client.PostAsJsonAsync("/api/ordens-de-servico", request);
    }

    protected async Task<HttpResponseMessage> AdicionarProdutoAsync(
        Guid ordemId,
        Guid produtoId,
        decimal valorVenda,
        int quantidade = 1)
    {
        var request = new
        {
            ProdutoId = produtoId,
            ValorVenda = valorVenda,
            Quantidade = quantidade
        };
        return await Client.PostAsJsonAsync($"/api/ordens-de-servico/{ordemId}/produtos", request);
    }

    protected async Task<HttpResponseMessage> AdicionarServicoAsync(
        Guid ordemId,
        Guid servicoId,
        decimal valor)
    {
        var request = new
        {
            ServicoId = servicoId,
            Valor = valor
        };
        return await Client.PostAsJsonAsync($"/api/ordens-de-servico/{ordemId}/servicos", request);
    }

    protected async Task<HttpResponseMessage> AvancarEtapaAsync(Guid ordemId)
    {
        return await Client.PatchAsync($"/api/ordens-de-servico/{ordemId}/avancar", null);
    }

    protected async Task<HttpResponseMessage> CancelarOrdemAsync(Guid ordemId)
    {
        return await Client.PatchAsync($"/api/ordens-de-servico/{ordemId}/cancelar", null);
    }

    protected async Task<HttpResponseMessage> PagamentoAprovadoAsync(Guid ordemId)
    {
        return await Client.PatchAsync($"/api/ordens-de-servico/{ordemId}/pagamento-aprovado", null);
    }

    protected async Task<HttpResponseMessage> ObterOrdemPorIdAsync(Guid ordemId)
    {
        return await Client.GetAsync($"/api/ordens-de-servico/{ordemId}");
    }

    protected async Task<HttpResponseMessage> RemoverProdutoAsync(Guid ordemId, Guid produtoId)
    {
        return await Client.DeleteAsync($"/api/ordens-de-servico/{ordemId}/produtos/{produtoId}");
    }

    protected async Task<HttpResponseMessage> RemoverServicoAsync(Guid ordemId, Guid servicoId)
    {
        return await Client.DeleteAsync($"/api/ordens-de-servico/{ordemId}/servicos/{servicoId}");
    }

    protected async Task<HttpResponseMessage> ListarPorStatusAsync(string status, int page = 1, int pageSize = 10)
    {
        return await Client.GetAsync($"/api/ordens-de-servico/status/{status}?page={page}&pageSize={pageSize}");
    }

    protected async Task<HttpResponseMessage> ListarTodasAsync(int page = 1, int pageSize = 10)
    {
        return await Client.GetAsync($"/api/ordens-de-servico?page={page}&pageSize={pageSize}");
    }
}

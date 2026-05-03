using System.Net;
using Mecanica.Hermes.IntegrationTests.Fixtures;
using Mecanica.Hermes.IntegrationTests.Infrastructure;

namespace Mecanica.Hermes.IntegrationTests.OrdensDeServico;

[Collection(IntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public class OrdemDeServicoIntegrationTests : OrdemDeServicoTestFixture
{
    public OrdemDeServicoIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CriarOrdemDeServico_DeveRetornarAccepted()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        // Act
        var response = await CriarOrdemDeServicoAsync(
            clienteId,
            veiculoId,
            "Motor fazendo barulho",
            "Cliente relatou barulho ao acelerar");

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task ListarPorStatus_DeveRetornarOk()
    {
        // Act
        var response = await ListarPorStatusAsync("Recebida");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task AdicionarProduto_DeveRetornarAccepted()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        // Act
        var response = await AdicionarProdutoAsync(ordemId, produtoId, 100m, 2);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task AdicionarServico_DeveRetornarAccepted()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();

        // Act
        var response = await AdicionarServicoAsync(ordemId, servicoId, 150m);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task AvancarEtapa_DeveRetornarAccepted()
    {
        // Arrange
        var ordemId = Guid.NewGuid();

        // Act
        var response = await AvancarEtapaAsync(ordemId);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task CancelarOrdem_DeveRetornarAccepted()
    {
        // Arrange
        var ordemId = Guid.NewGuid();

        // Act
        var response = await CancelarOrdemAsync(ordemId);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task RemoverProduto_DeveRetornarAccepted()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        // Act
        var response = await RemoverProdutoAsync(ordemId, produtoId);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task RemoverServico_DeveRetornarAccepted()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();

        // Act
        var response = await RemoverServicoAsync(ordemId, servicoId);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task PagamentoAprovado_DeveRetornarAccepted()
    {
        // Arrange
        var ordemId = Guid.NewGuid();

        // Act
        var response = await PagamentoAprovadoAsync(ordemId);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
}

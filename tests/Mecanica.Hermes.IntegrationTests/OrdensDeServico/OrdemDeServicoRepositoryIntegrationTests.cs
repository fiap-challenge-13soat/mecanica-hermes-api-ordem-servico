using Mecanica.Hermes.Shared.Core.Pagination;
using Mecanica.Hermes.Shared.Core.Persistence;
using Mecanica.Hermes.Application.OrdensDeServico.Ports;
using Mecanica.Hermes.Domain.OrdensDeServico.Enums;
using Mecanica.Hermes.IntegrationTests.Infrastructure;
using Mecanica.Hermes.TestUtils.Builders;

namespace Mecanica.Hermes.IntegrationTests.OrdensDeServico;

/// <summary>
///     Integração real do <c>OrdemDeServicoRepository</c> + <c>AppDbContext</c> + mapper contra
///     o Postgres real (Testcontainers). Valida que o ciclo <c>AddAsync → CommitAsync → GetByIdAsync</c>
///     preserva o agregado e que a listagem paginada funciona com filtro por status.
///     Diferencia-se dos smoke tests (apenas HTTP 202) por exercitar a camada de persistência
///     end-to-end, garantindo cobertura do Repository + Mapper + Migrations.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public class OrdemDeServicoRepositoryIntegrationTests(IntegrationTestWebApplicationFactory factory)
{
    [Fact]
    public async Task AddAsync_GetByIdAsync_DevePreservarAgregadoCompleto()
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrdemDeServicoRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var ordem = new OrdemDeServicoBuilder()
            .ComProblemaRelatado("Motor")
            .ComObservacoes("Obs")
            .ComProduto(valor: 100m, quantidade: 2)
            .ComServico(valor: 50m)
            .Build();

        await repository.AddAsync(ordem);
        await uow.CommitAsync(CancellationToken.None);

        var persisted = await repository.GetByIdAsync(ordem.Id);

        Assert.NotNull(persisted);
        Assert.Equal(ordem.Id, persisted.Id);
        Assert.Equal(ordem.ClienteId, persisted.ClienteId);
        Assert.Equal(ordem.VeiculoId, persisted.VeiculoId);
        Assert.Equal("Motor", persisted.ProblemaRelatado);
        Assert.Single(persisted.Produtos);
        Assert.Single(persisted.Servicos);
        Assert.Equal(OrdemDeServicoStatus.Recebida, persisted.StatusAtual.StatusAtual);
    }

    [Fact]
    public async Task UpdateAsync_DevePersistirMudancaDeStatus()
    {
        // Add em um scope (simulando consumer 1)
        Guid id;
        using (var scope = factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IOrdemDeServicoRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var ordem = new OrdemDeServicoBuilder().Build();
            id = ordem.Id;
            await repo.AddAsync(ordem);
            await uow.CommitAsync(CancellationToken.None);
        }

        // Update em outro scope (simulando consumer 2)
        using (var scope = factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IOrdemDeServicoRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var loaded = await repo.GetByIdAsync(id);
            Assert.NotNull(loaded);
            loaded.AvancarEtapa();
            await repo.UpdateAsync(loaded);
            await uow.CommitAsync(CancellationToken.None);
        }

        // Verificar em um terceiro scope
        using (var scope = factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IOrdemDeServicoRepository>();
            var updated = await repo.GetByIdAsync(id);

            Assert.NotNull(updated);
            Assert.Equal(OrdemDeServicoStatus.EmDiagnostico, updated.StatusAtual.StatusAtual);
            Assert.NotEmpty(updated.HistoricosStatus);
        }
    }

    [Fact]
    public async Task ListByStatusAsync_DeveFiltrarCorretamente()
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrdemDeServicoRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var recebida = new OrdemDeServicoBuilder().Build();
        var emDiagnostico = new OrdemDeServicoBuilder().ComStatusAvancadoEm(1).Build();
        await repository.AddAsync(recebida);
        await repository.AddAsync(emDiagnostico);
        await uow.CommitAsync(CancellationToken.None);

        var list = await repository.ListByStatusAsync(
            OrdemDeServicoStatus.EmDiagnostico,
            new PaginationParams { Page = 1, PageSize = 50 });

        Assert.NotNull(list);
        Assert.Contains(list.Items, o => o.Id == emDiagnostico.Id);
        Assert.DoesNotContain(list.Items, o => o.Id == recebida.Id);
    }
}

# Testes

## Estrutura

| Projeto | Tipo | Escopo |
| --- | --- | --- |
| `Mecanica.Hermes.Domain.Tests` | Unitário | Agregado, value objects, status lifecycle, `Result<T>`, paginação |
| `Mecanica.Hermes.Application.Tests` | Unitário | Consumers, query handlers, `AsyncCommandHandler<T>`, pipeline base |
| `Mecanica.Hermes.Infrastructure.Tests` | Unitário | `DomainEventDispatcher`, mappers, `MassTransitMessagePublisher`, `OrdemDeServicoMetrics`, OTel env |
| `Mecanica.Hermes.Api.Tests` | Unitário | Presenter, middleware (correlation, exception handler) |
| `Mecanica.Hermes.IntegrationTests` | Integração | API HTTP + Postgres real (Testcontainers) — smoke 202 + repository end-to-end |
| `Mecanica.Hermes.ArchitectureTests` | Estática | NetArchTest — valida regras de dependência entre projetos |
| `Mecanica.Hermes.TestUtils` | Suporte | `OrdemDeServicoBuilder` e fakes compartilhados |

> `TestUtils` **não é runnable** — é referenciado pelos demais projetos de teste.

## Stack

| Categoria | Pacote |
| --- | --- |
| Test runner | xUnit |
| Assertions | FluentAssertions |
| Mocking | Moq |
| Geração de dados | Bogus (`new Faker("pt_BR")` no `OrdemDeServicoBuilder`) |
| MassTransit | `MassTransit.TestFramework` (`InMemoryTestHarness`) |
| Web factory | `Microsoft.AspNetCore.Mvc.Testing` |
| Banco real | `Testcontainers.PostgreSql` |
| Cobertura | `coverlet.collector` + `dotnet-reportgenerator-globaltool` |
| Arquitetura | `NetArchTest.Rules` |

## Como rodar

```bash
# tudo (precisa Docker para os testes de integração)
just test
# ou
dotnet test Mecanica.Hermes.Ordem.Servico.sln --settings coverlet.runsettings \
    --results-directory .temp/coverage

# apenas unitários (sem Docker)
just test-unit

# relatório HTML de cobertura (depois do test)
just coverage-report
# abre .temp/coverage/report/index.html
```

## Convenções

### Nomenclatura

- `Cenario_Resultado` ou `Verbo_CondicaoEsperada`.
- Exemplo: `AdicionarProduto_DeveRetornarErro_QuandoProdutoJaAdicionado`.
- Use `[Theory]` + `[InlineData]` para casos paramétricos.

### Arrange-Act-Assert

Sempre — comentários `// Arrange` etc. são opcionais (use quando ajudar a leitura).

### Test Data Builder

Use o `OrdemDeServicoBuilder` em vez de espalhar `Guid.NewGuid()` e
`OrdemDeServico.Criar(...)` pelos testes:

```csharp
var ordem = new OrdemDeServicoBuilder()
    .ComProblemaRelatado("Motor")
    .ComProduto(valor: 100m, quantidade: 2)
    .ComStatusAvancadoEm(2) // → AguardandoAprovacao
    .Build();
```

O builder limpa Domain Events ao final de `Build()` para que testes só observem eventos
gerados pela ação sob teste.

### FluentAssertions

```csharp
result.IsSuccess.Should().BeTrue();
result.Data!.StatusAtual.StatusAtual.Should().Be(OrdemDeServicoStatus.Recebida);
ordem.Produtos.Should().ContainSingle(p => p.ProdutoId == produtoId);
```

### Mock de `ConsumeContext<T>`

Use o helper `FakeConsumeContext.For(mensagem)` quando quiser invocar `Consume()`
diretamente:

```csharp
var consumer = new AddProdutoConsumer(repo.Object, uow.Object);
await consumer.Consume(FakeConsumeContext.For(new ExecuteAddProduto(...)));
```

Use o `InMemoryTestHarness` apenas quando o que está sob teste é a SAGA ou a interação
com o broker.

### Integration tests — escopos por operação

Sempre crie um `IServiceScope` por operação (simula consumers reais) para evitar erros
do EF Core por entidades já trackeadas:

```csharp
using (var scope = factory.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<IOrdemDeServicoRepository>();
    // ...
}
```

## Cobertura

- **Threshold no CI:** ≥ 80% de linhas (configurado em `build-and-tests-ci.yml`).
- **Configuração:** `coverlet.runsettings` na raiz.
- **Excluído por arquivo:** `**/Migrations/*.cs`, `obj/`, `bin/`.
- **Excluído por atributo:** `Obsolete`, `GeneratedCode`, `CompilerGenerated`,
  `ExcludeFromCodeCoverage`.
- **Assemblies excluídos:** `*.Tests`, `Mecanica.Hermes.TestUtils`.

### Marcadas `[ExcludeFromCodeCoverage]`

Composition roots, normalmente exercitados em integração:

- `WebApplicationBuilderExtensions` (Serilog config).
- `LogLevelEnricher`.
- `ObservabilityConfiguration` (OTel).
- `ActivitySources` (constantes de tracing).
- `BusFactoryConfiguratorExtensions`, `BusRegistrationConfiguratorExtensions`,
  `RabbitMqBusFactoryConfiguratorExtensions` (MassTransit config).

## Anti-duplicação no pipeline de consumers

Os 9 consumers de mutação **não** repetem testes do pipeline genérico:

- `OrdemDeServicoConsumerBaseTests` (em `Application.Tests/Common/Messaging/`) valida
  uma vez o pipeline.
- `ConsumerSpecificBehaviorTests` valida apenas o que cada consumer override
  (`Execute`, `OperacaoMetricKey`, `ValidatePreconditions`).

## Checklist para novos testes

- [ ] Nome segue padrão `Cenario_Resultado`.
- [ ] Usa FluentAssertions (`Should().*`).
- [ ] Usa `OrdemDeServicoBuilder` quando precisa de agregado.
- [ ] AAA visível.
- [ ] Mock só do que é externo (Repository, UoW, Metrics, Publisher).
- [ ] Sem `Thread.Sleep` (use polling com timeout curto em integration).
- [ ] Um único comportamento por teste (SRP nos testes).
- [ ] Não duplica cenário já testado em outro lugar.

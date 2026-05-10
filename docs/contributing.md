# Guia de contribuição

## Antes de começar

1. Leia [architecture.md](./architecture.md) para entender as camadas e a regra de
   dependência (validada pelo `Mecanica.Hermes.ArchitectureTests`).
2. Leia [service-order-flow.md](./service-order-flow.md) — o agregado é o coração do
   sistema.
3. Familiarize-se com os ADRs em [adrs.md](./adrs.md) — eles cobrem os trade-offs já
   discutidos.

## Branching

| Branch | Para quê |
| --- | --- |
| `main` | Linha estável; deploys de HML disparam daqui. |
| `feature/<descrição-curta>` | Trabalho em andamento. Cobertura de testes é executada no push. |
| `versao/<x.y.z>` | Branch de estabilização para release (opcional). |

Tags `v*.*.*` em `main` disparam o pipeline de produção.

## Setup local

```bash
# pré-requisitos: .NET 10, Docker, just (opcional)

dotnet restore
dotnet build --configuration Release

# subir Postgres + RabbitMQ + Mongo (ver README)
# rodar a API
just run
```

## Estilo de código

- `Directory.Build.props` define `Nullable=enable`, `ImplicitUsings=enable`,
  `EnableNETAnalyzers=true` (`AnalysisMode=Recommended`).
- `.editorconfig` na raiz é fonte da verdade para indent/quebras/espaços.
- Convenções:
  - Arquivos: `PascalCase.cs`.
  - Namespaces seguem o caminho da pasta.
  - Records `sealed` por default (especialmente para mensagens MassTransit).
  - Use `Result`/`Result<T>` para sinalizar erro de regra de negócio; reserve exceções
    para falhas inesperadas.
  - Não introduza dependências em Domain (validado por testes de arquitetura).

## Workflow de uma mudança típica

### Adicionar uma nova operação no agregado

1. Definir o método no agregado (`Mecanica.Hermes.Domain.OrdensDeServico.OrdemDeServico`).
2. Adicionar testes em `tests/Mecanica.Hermes.Domain.Tests`.
3. Criar `Request` e `Execute*Command` em
   `Mecanica.Hermes.Application/OrdensDeServico/Messages`.
4. Criar consumer em `Mecanica.Hermes.Application/OrdensDeServico/Consumers` herdando
   de `OrdemDeServicoMutateConsumerBase<T>` (ou `Create...` quando for criação).
5. Definir o `*Command` (record `IAsyncCommand`) em
   `Mecanica.Hermes.Application/OrdensDeServico/Commands`. Ele converte para o `*Request`
   no `ToMessage()`.
6. Atualizar `OrdemDeServicoStateMachine` para correlacionar/transicionar pelo novo
   request.
7. Criar a rota em `Mecanica.Hermes.Api/Endpoints/OrdensDeServico/Routes` e registrá-la em
   `OrdensDeServicoEndpoints.cs`.
8. Criar o `Request`/`Response` HTTP e o `Validator` (FluentValidation) se houver body.
9. Cobrir com:
   - testes unitários do consumer em `Application.Tests`,
   - integration test em `IntegrationTests` se for end-to-end (HTTP → DB).
10. Atualizar [`api-reference.md`](./api-reference.md) e [`messaging.md`](./messaging.md).

### Adicionar uma migration

```bash
just migration-add Add{Algo}Para{Entidade}

# revisar o diff gerado em src/Mecanica.Hermes.Infrastructure/Persistence/Migrations
# aplicar localmente:
just migration-apply
```

Não use nomes genéricos como `Migration1`/`Fix`/`Test`. O nome precisa descrever a
mudança (ex.: `AddOutboxMessages`, `AlterOrdemDeServicoAddObservacoes`). Ver
[database.md](./database.md#convencao-de-nome).

## Pull Requests

- O nome da PR deve descrever a mudança no infinitivo
  (ex.: `feat: avançar etapa via novo endpoint`).
- Inclua: contexto, decisão, alternativas avaliadas (se houver), impacto operacional.
- O CI roda em todo push em `feature/**`. PRs só mergeiam com:
  - build verde,
  - testes verdes,
  - cobertura ≥ 80%.
- Toda mudança em domínio deve vir com testes em `Domain.Tests`.
- Toda mudança em consumer/handler deve vir com testes em `Application.Tests`.
- Mudanças no contrato HTTP devem atualizar `api-reference.md`.

## Convenções de teste

Detalhes em [testing.md](./testing.md). Resumo:

- xUnit + FluentAssertions + Moq + Bogus.
- `OrdemDeServicoBuilder` para criar agregados em testes.
- Naming: `Cenario_Resultado` ou `Verbo_CondicaoEsperada`.
- AAA visível.
- Sem `Thread.Sleep` em testes — use polling com timeout curto se for inevitável.

## Runbook & observabilidade

- Antes de fazer um change com efeito operacional, revise [runbook.md](./runbook.md) para
  entender quais sinais existem.
- Adicionou métrica nova? Documente em [observability.md](./observability.md) e adicione
  teste em `tests/Mecanica.Hermes.Infrastructure.Tests/Observability/MetricsHandlersTests.cs`.

## Segurança

- Endpoints de mutação devem ter `RequireAuthorization(AuthPolicies.OnlyAdminScope)`.
- Endpoints de leitura usam `AllowClienteScope` (ou `OnlyAdminScope`).
- Nunca comite secrets (`AUTH__*`, license keys). O `launchSettings.json` **não** é
  versionado — é mantido apenas localmente por cada contribuidor.

## Onde achar ajuda

- Documentação: [`docs/`](./).
- Decisões registradas: [`adrs.md`](./adrs.md).
- Testes de arquitetura: `tests/Mecanica.Hermes.ArchitectureTests` — eles falham se a
  regra de dependência é quebrada.

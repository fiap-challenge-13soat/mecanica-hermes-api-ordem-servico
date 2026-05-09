# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Ecosystem Context

This repository is **one of five** in the Mecânica Hermes ecosystem. They live as siblings under
`C:\git\mecanica-hermes\` (or equivalent). When working here, keep in mind:

| Repo | Role | Key tech |
|---|---|---|
| `mecanica-hermes-api-ordem-servico` | **This repo** — orquestra ordens de serviço (state machine, SAGA principal) | .NET 10, PostgreSQL, RabbitMQ, MongoDB (saga state) |
| `mecanica-hermes-cadastros` | Cadastros de Clientes e Produtos; recebe webhook de aprovação/rejeição de orçamento | .NET 10, PostgreSQL |
| `mecanica-hermes-api-pagamentos` | Integração com Mercado Pago, gera link, recebe webhook MP, polling de status | .NET 10, MongoDB |
| `mecanica-hermes-tests-e2e` | Suíte E2E Robot Framework BDD cobrindo as 3 APIs em Docker Compose | Python 3.11, Robot Framework, Allure |
| `mecanica-hermes-shared-sdk` | SDK compartilhado em GitHub Packages (6 pacotes NuGet) | .NET 10, MassTransit.Abstractions |

### Cross-service integration

- **Mensageria** (RabbitMQ via MassTransit): 7 eventos versionados via `[EntityName(".v1")]`. Esta API
  publica e consome 6 deles (ver "Cross-service Integration Events" abaixo). O sufixo `.v1` é
  contrato — qualquer mudança de schema cria `.v2` em paralelo (deprecation window).
- **HTTP M2M**: esta API consome `GET /api/clientes/{id}` do Cadastros (autenticação via JWT M2M;
  no Development o `DevelopmentAuthenticationMiddleware` bypassa).

### Shared SDK status (em adoção)

PR #1 do `mecanica-hermes-shared-sdk` ainda **não foi mergeado** quando este CLAUDE.md foi escrito.
Quando for: 6 pacotes NuGet (`Mecanica.Hermes.Shared.Core`, `.Application`, `.AspNetCore`,
`.Observability`, `.MassTransit`, `Mecanica.Hermes.Contracts`) substituirão ~700 linhas duplicadas
deste repo. Até lá, **não mover código para o SDK** sem alinhar com o usuário primeiro.

## Commands

```bash
# Build
dotnet build --configuration Release

# Run API locally
dotnet run --project src/Mecanica.Hermes.Api

# Run all tests
dotnet test Mecanica.Hermes.Ordem.Servico.sln

# Run only unit tests (no Docker/Postgres required)
dotnet test tests/Mecanica.Hermes.Domain.Tests
dotnet test tests/Mecanica.Hermes.Application.Tests

# Run tests with coverage
dotnet test Mecanica.Hermes.Ordem.Servico.sln --settings coverlet.runsettings --results-directory .temp/coverage

# Generate coverage report
reportgenerator -reports:.temp/coverage/**/coverage.opencover.xml -targetdir:.temp/coverage/report -reporttypes:TextSummary;Html

# EF Core migrations
dotnet ef migrations add <Name> --project src/Mecanica.Hermes.Infrastructure --startup-project src/Mecanica.Hermes.Api
dotnet ef database update --project src/Mecanica.Hermes.Infrastructure --startup-project src/Mecanica.Hermes.Api
```

Integration tests (`Mecanica.Hermes.IntegrationTests`) require Docker to spin up a PostgreSQL container via Testcontainers. The database is auto-migrated on startup.

Coverage threshold enforced in CI: ≥80% line coverage.

## Architecture

**Clean Architecture + DDD + CQRS**, strict dependency rule: `Domain ← Application ← Infrastructure`, `Api → all`.

```text
Api (Minimal API)
 └─► Application (CQRS / MediatR)
      └─► Domain (pure business logic)
Infrastructure (EF Core, MassTransit, Outbox)
 └─► Application (implements ports)
```

### Projects

| Project                          | Role                                                                      |
| :------------------------------- | :------------------------------------------------------------------------ |
| `Mecanica.Hermes.Domain`         | Aggregates, value objects, domain events, state classes, `Result<T>`      |
| `Mecanica.Hermes.Application`    | CQRS commands/queries, MassTransit consumers, ports (interfaces)          |
| `Mecanica.Hermes.Infrastructure` | EF Core, repositories, Outbox processor, MassTransit setup, observability |
| `Mecanica.Hermes.Api`            | Minimal API endpoints, middleware, `ResultPresenter`, OpenAPI/Scalar      |
| `Mecanica.Hermes.TestUtils`      | Shared test builders and fakes (not a runnable test project)              |

### Domain: OrdemDeServico

The aggregate root `OrdemDeServico` (service order) manages a vehicle repair lifecycle. Key sub-entities:

- `OrdemDeServicoProduto` — product/part snapshot (price captured at creation time, not a live reference)
- `OrdemDeServicoServico` — service snapshot
- `OrdemDeServicoHistoricoStatus` — audit log of each state transition

**State Pattern** — each status is a concrete class inheriting `OrdemDeServicoStatusBase`:

```text
Recebida → EmDiagnostico → AguardandoAprovacao → EmExecucao → ManutencaoFinalizada → AguardandoPagamento → PagamentoConfirmado → Entregue
                                         └─► Rejeitada
Any state → Cancelada
```

State classes define `AvancarEtapa()`, `ReverterEtapa()`, and `Cancelar()` returning `Result<OrdemDeServicoStatusBase>`. The property `PermiteEditarProdutos` controls whether products/services can be added or removed in a given state.

### Result Pattern

All domain and application operations return `Result` or `Result<T>`. Results carry `HttpStatusCode`, `IsSuccess`/`IsFailure`, and `Errors`. The `ResultPresenter` in the API layer converts `Result<T>` to the appropriate `IResult` HTTP response (RFC 7807 problem details on failure).

### CQRS Flow

**Write (async commands):**

1. Route handler creates a command and sends via `ISender` (MediatR).
2. The generic `AsyncCommandHandler<T>` publishes the command as a message via `IAsyncMessagePublisher` (MassTransit adapter).
3. Returns `Result.Accepted()` immediately to the caller.
4. A MassTransit consumer (`OrdemDeServicoConsumerBase<TCommand>`) processes the message, loads the aggregate, calls the domain method, then saves via `IUnitOfWork`.

**Read (sync queries):**
Query handlers call the repository directly and return `Result<T>` synchronously.

### Outbox Pattern

Domain events raised inside an aggregate are:

1. Serialized into `OutboxMessageEntity` rows in the **same EF Core transaction** as the aggregate save (atomicity guaranteed).
2. Picked up by the `OutboxProcessor` background service, which dispatches them via `DomainEventDispatcher` → MediatR notification → MassTransit publish.

This ensures at-least-once event delivery even if the message broker is temporarily unavailable.

### Idempotência cross-service nos consumers de integração

Os 4 consumers em `Application/OrdensDeServico/Consumers/Integration/`
(`OrcamentoAprovado/Rejeitado`, `PagamentoConfirmado/Recusado`) **carregam a OS antes de
despachar à SAGA** e ignoram o evento se a OS já está em estado-destino. Mitiga entrega
tardia/redelivery (RabbitMQ retry, Mercado Pago redelivery após nosso ack):

| Evento | Estado-fonte esperado | Ação se OS já não está nele |
|---|---|---|
| `orcamento.aprovado-pelo-cliente.v1` | `AguardandoAprovacao` | skip + métrica |
| `orcamento.rejeitado-pelo-cliente.v1` | `AguardandoAprovacao` | skip + métrica |
| `pagamento.confirmado.v1` | `AguardandoPagamento` | skip + métrica |
| `pagamento.recusado.v1` | `AguardandoPagamento` | skip + métrica |

Métrica: `IOrdemDeServicoMetrics.EventoIntegracaoIgnorado(evento, statusAtual)` — counter
`ordemdeservico.evento_integracao_ignorado` no meter `MecanicaHermes.Business`.

### DLQ observability

`MessagingFaultObserver` (`Infrastructure/MassTransit/Observability/`) é um
`IConsumeObserver` global encadeado em `BusFactoryConfiguratorExtensions.ConfigureCommonFactory`.
Quando `ConsumeFault<T>` dispara (após retry imediato 3x + delayed redelivery 3x exponencial),
emite counter `messaging.consume_faults` com tags `message_type`/`exception_type` no meter
`MecanicaHermes.Messaging`. Use pra alarmar quando consumers `Execute*` da SAGA ou
`Integration/*` falham em definitivo (OS travada num estado pra um `OrdemId` específico).

### Cross-service Integration Events

7 contratos cross-service circulam entre OS, Cadastros e Pagamentos. Todos versionados com sufixo
`.v1` no `[EntityName]` (kebab-case). Esta API:

| Direção | Evento (`EntityName`) | Fluxo |
|---|---|---|
| Publica → | `ordem-de-servico.aguardando-aprovacao.v1` | Quando OS entra em `AguardandoAprovacao` (Cadastros consome para gerar webhook link) |
| Consome ← | `orcamento.aprovado-pelo-cliente.v1` | Cadastros notifica que cliente aprovou pelo webhook |
| Consome ← | `orcamento.rejeitado-pelo-cliente.v1` | Cadastros notifica rejeição |
| Publica → | `ordem-de-servico.aguardando-pagamento.v1` | OS pediu pagamento (Pagamentos consome para gerar link MP) |
| Consome ← | `link-pagamento-gerado.v1` | Pagamentos confirma que o link foi gerado |
| Consome ← | `pagamento.confirmado.v1` | Pagamentos confirma aprovação (avança SAGA para `PagamentoConfirmado`) |
| Consome ← | `pagamento.recusado.v1` | Pagamentos sinaliza recusa (SAGA reverte para `ManutencaoFinalizada` para nova tentativa de pagamento) |

**Regra de evolução**: nunca alterar a forma de um evento publicado — sempre criar `.v2` em paralelo
e drenar `.v1` antes de removê-lo.

### Infrastructure: Entity/Domain Split

EF Core-mapped classes live in `Infrastructure/Persistence/Entities/` (e.g., `OrdemDeServicoEntity`). These are separate from domain classes and mapped back and forth via AutoMapper profiles inside Infrastructure. Domain classes have **no persistence attributes**.

### Endpoint Registration Pattern

Routes are static extension methods, grouped under `OrdensDeServicoEndpoints.cs`:

```csharp
// Program.cs
app.MapOrdensDeServicoEndpoints();

// OrdensDeServicoEndpoints.cs
var group = app.MapGroup("/api/ordens-de-servico");
group.MapCreateOrdemDeServico();
group.MapAddProduto();
// ...

// Routes/AddProdutoRoute.cs
endpoints.MapPost("/{id:guid}/produtos", AddProduto)
    .RequireAuthorization(AuthPolicies.OnlyAdminScope);
```

Each handler: parse request → build Command/Query → `sender.Send(...)` → `response.Present()`.

### Authentication

JWT Bearer authentication with scope-based authorization (`AuthPolicies`). In `Development` and `Testing` environments, `DevelopmentAuthenticationMiddleware` bypasses JWT verification so endpoints can be called without a real token.

### Repository Interfaces (ISP)

- `IOrdemDeServicoReader` — query-only methods (used by query handlers)
- `IOrdemDeServicoWriter` — mutation methods (used by command consumers)
- `IOrdemDeServicoRepository` — combines both (implemented by `OrdemDeServicoRepository`)

Query handlers only depend on `IOrdemDeServicoReader` to prevent accidental writes.

### Key Dependencies

| Concern         | Package                                             |
| :-------------- | :-------------------------------------------------- |
| CQRS mediator   | MediatR 14                                          |
| Async messaging | MassTransit 8 (RabbitMQ / SQS / MongoDB)            |
| Object mapping  | Manual extension methods (AutoMapper removed)       |
| DI container    | Autofac 9                                           |
| ORM             | EF Core 10 + Npgsql                                 |
| API docs        | Scalar + Microsoft.AspNetCore.OpenApi               |
| Observability   | OpenTelemetry (OTLP → New Relic) + Serilog          |
| Testing         | xUnit, Moq, FluentAssertions, Bogus, Testcontainers |

## Testing Strategy

- **`Domain.Tests`** — pure unit tests, no mocks needed (domain is side-effect free).
- **`Application.Tests`** — unit tests with Moq; use `MassTransit.TestFramework` `InMemoryTestHarness` for consumer tests.
- **`Infrastructure.Tests`** — test repositories against a real in-memory or Testcontainers DB.
- **`Api.Tests`** — middleware and presenter unit tests.
- **`IntegrationTests`** — full HTTP stack using `WebApplicationFactory` + Testcontainers PostgreSQL; actual migrations applied.
- **`TestUtils`** — `OrdemDeServicoBuilder` and other Bogus-powered builders shared across test projects.

Consumer tests follow the Template Method pattern: only override `Execute()` and `ValidatePreconditions()`.

## Docs

Detailed documentation lives in `docs/`:

- `architecture.md` — Layer dependency diagram and patterns
- `service-order-flow.md` — Full state machine and transition rules
- `database.md` — ER diagram, indexes, migration conventions
- `testing.md` — Test strategy and conventions
- `ci-cd.md` — GitHub Actions and Docker multi-platform build
- `adrs.md` — Architecture Decision Records
- `messaging.md` — SAGA, Outbox, message contracts, consumer pipeline
- `api-reference.md` — Endpoints, auth scopes, async semantics
- `configuration.md` — Environment variables
- `observability.md` — Metrics, traces, logs
- `runbook.md` — Operational incidents
- `contributing.md` — Contributor guide

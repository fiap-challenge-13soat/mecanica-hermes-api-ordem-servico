# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

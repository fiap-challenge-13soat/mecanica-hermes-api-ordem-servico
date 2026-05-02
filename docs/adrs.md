# Architecture Decision Records

Os ADRs registram decisões com impacto arquitetural duradouro. Cada entrada
descreve **decisão**, **motivação** e (quando relevante) **consequências**.

---

## ADR-001 — Entidades de persistência separadas do domínio

**Status:** Aceito.

**Decisão:** O EF Core opera sobre classes `*Entity` (em
`Infrastructure/Persistence/Entities`); o domínio permanece isolado de anotações de
persistência.

**Justificativa:** preserva a pureza do domínio, evita acoplamento ao ORM e libera o
modelo para evoluir sem migrations forçadas.

---

## ADR-002 — Result Pattern para fluxos de negócio

**Status:** Aceito.

**Decisão:** Comandos/queries retornam `Result`/`Result<T>` (com `HttpStatusCode`) em vez
de lançar exceções para regras de negócio.

**Justificativa:** padroniza tratamento de erro e simplifica o mapeamento HTTP via
`ResultPresenter`.

---

## ADR-003 — State Pattern para ciclo de vida da Ordem de Serviço

**Status:** Aceito.

**Decisão:** Modelar transições de estado por classes concretas
(`OrdemDeServicoStatusBase`).

**Justificativa:** encapsula regras de transição em cada estado e evita condicionais
distribuídas pelo agregado.

---

## ADR-004 — Domain Events pós-commit da Unit of Work

**Status:** Aceito.

**Decisão:** Eventos coletados pelo agregado são despachados após persistência
bem-sucedida (originalmente direto, hoje via Outbox — ver ADR-011).

**Justificativa:** evita efeitos colaterais antes da confirmação transacional.

---

## ADR-005 — MassTransit para processamento assíncrono

**Status:** Aceito.

**Decisão:** Processar comandos via MassTransit (RabbitMQ/SQS/Mongo).

**Justificativa:** desacopla execução de comandos da requisição HTTP, melhora
escalabilidade e resiliência.

---

## ADR-006 — Separação entre `Request` e `Execute*` na mensageria

**Status:** Aceito.

**Decisão:** Manter dois conjuntos de mensagens — `*Request` (entrada externa, consumido
pela SAGA) e `Execute*` (comando interno do orquestrador, consumido por consumers
especializados) — mesmo carregando campos idênticos.

**Justificativa:**

- Roteamento físico distinto: `Request` vai para a fila da SAGA; `Execute*` para a fila
  do consumer dedicado.
- Reentrância segura: SAGA pode `Ignore` `*Request` em estado `Processing` sem afetar
  consumers; redelivery tardio de `OperationCompleted` não cria nova SAGA.
- Intent semântico claro entre "intenção do cliente" e "ordem operacional do
  orquestrador".

**Consequências:** SAGA mapeia `Request → Execute*`; ferramentas de API expõem apenas
`*Request` como contrato externo.

---

## ADR-007 — Port `IAsyncMessagePublisher`

**Status:** Aceito.

**Decisão:** A camada Application publica mensagens via porta hexagonal
`IAsyncMessagePublisher`. O `AsyncCommandHandler<T>` genérico depende dessa porta.
`MassTransitMessagePublisher` (Infrastructure) é o adapter que delega para
`IPublishEndpoint`.

**Justificativa:** Application deixa de importar MassTransit no caso de uso genérico
(DIP); permite testes sem harness e troca futura do broker.

**Acoplamento intencional nos consumers:** Consumers herdam diretamente de
`IConsumer<T>` do MassTransit — são *driver adapters* de entrada e o
`MassTransit.TestFramework` já permite testá-los sem broker. Nenhuma porta extra é
introduzida só para isolar `IConsumer`.

---

## ADR-008 — Pipeline padrão de Consumers (Template Method)

**Status:** Aceito (refinado por ADR-012).

**Decisão:** Introduzir base abstrata centralizando o pipeline `load → validate →
execute → persist → publish`.

**Justificativa:** elimina ~75% da duplicação dos 9+ consumers, reforça SRP, e centraliza
publicação de `OperationCompleted/Failed` em um único ponto de manutenção.

---

## ADR-009 — ISP nos repositórios (Reader / Writer)

**Status:** Aceito.

**Decisão:** Segregar `IOrdemDeServicoRepository` em `IOrdemDeServicoReader` (consultas)
e `IOrdemDeServicoWriter` (mutações). A interface composite continua existindo para
consumers que precisam de ambas as capacidades.

**Justificativa:** Query Handlers ficam com superfície mínima, abrindo caminho para
read replica sem alterar consumers de escrita.

---

## ADR-010 — Outbox transacional para Domain Events

**Status:** Aceito.

**Decisão:** Cada Domain Event é serializado como `OutboxMessageEntity` na **mesma
transação** do `SaveChangesAsync` do agregado. Um `OutboxProcessor` (BackgroundService)
lê em lotes e despacha.

**Justificativa:**

- Atomicidade entre estado e despacho: se a transação falha, nem o estado nem os eventos
  persistem.
- O modelo anterior chamava o dispatcher após o commit — eventos eram perdidos quando o
  dispatcher falhava (apenas log).
- Entrega *at-least-once* — handlers downstream devem ser idempotentes.

**Consequências:**

- Latência de despacho aumenta no pior caso (próximo poll, default 2s).
- Tabela `OutboxMessages` acumula registros processados; `OutboxCleanupService` faz
  retenção (default 7 dias).

---

## ADR-011 — Pipeline com hierarquia (Mutate vs Create)

**Status:** Aceito.

**Decisão:** Evolução do ADR-009. `OrdemDeServicoConsumerBase<TCommand>` é abstrata
enxuta com helpers de publicação. Duas especializações concretas:

- `OrdemDeServicoMutateConsumerBase<T>`: `Load → ValidatePreconditions → Execute →
  Update → Publish`.
- `OrdemDeServicoCreateConsumerBase<T>`: `CreateAggregate → Add → Publish`.

**Justificativa:** elimina a única exceção (`CreateOrdemDeServicoConsumer`) que ainda
repetia a lógica de publicação; mantém SRP em cada base.

---

## ADR-012 — Estratégia de testes automatizados

**Status:** Aceito.

**Decisão:** Stack `xUnit` + `FluentAssertions` + `Moq` + `Bogus` para unidade;
`MassTransit.TestFramework` para SAGA/consumers; `Microsoft.AspNetCore.Mvc.Testing` +
`Testcontainers.PostgreSQL` para integração com banco real; `NetArchTest.Rules` para
testes de arquitetura.

**Justificativa:** stack dominante na comunidade .NET; Testcontainers evita o erro
"passa em SQLite, falha em Postgres".

**Consequências:** convenções centralizadas em [`testing.md`](./testing.md); threshold de
cobertura no CI configurado para ≥ 80% de linhas.

---

## ADR-013 — Outbox: DLQ, concorrência e discriminador estável

**Status:** Aceito.

**Decisão:**

1. `OutboxMessageEntity` ganha `Status` (`Pending`/`Processed`/`DeadLettered`) e
   `TraceContext` (W3C `traceparent`).
2. Após `OutboxOptions.MaxRetries` falhas, a mensagem é movida para `DeadLettered` e
   nunca mais reprocessada automaticamente.
3. `OutboxProcessor` usa `SELECT … FOR UPDATE SKIP LOCKED` (PostgreSQL) para evitar que
   réplicas concorrentes processem o mesmo lote.
4. Tipo do evento é discriminado por `DomainEventNameAttribute`
   (ex.: `"ordem-de-servico.criado.v1"`) em vez de `AssemblyQualifiedName`.

**Justificativa:** evita loop infinito de retry; garante at-most-once-per-pod em
multi-réplica; elimina fragilidade do `AssemblyQualifiedName` em deploys com refactor.

**Consequências:** migration adiciona `Status` e `TraceContext`; `OutboxCleanupService`
cuida da retenção (default 7 dias).

---

## ADR-014 — SAGA timeout sentinel

**Status:** Aceito.

**Decisão:** `Schedule<OrdemDeServicoOperationTimedOut>` do MassTransit
`DelayedMessageScheduler` agenda um timer de 5 minutos ao entrar em `Processing`. Se a
operação não concluir, a SAGA volta para `Initial` registrando o timeout em
`UltimoMotivoFalha`. Em finalização normal, o timer é cancelado com `Unschedule`.

**Justificativa:** sem sentinel, uma operação travada (consumer morto, mensagem perdida)
bloqueia a SAGA indefinidamente, impedindo novas requests para aquela `OrdemDeServicoId`.

**Consequências:** requer `DelayedMessageScheduler` ativo. Campo
`OperationTimeoutTokenId` em `OrdemDeServicoState` armazena o token para cancelamento.

---

## ADR-015 — Pacotes MassTransit: todos os transports no mesmo assembly

**Status:** Aceito.

**Decisão:** Manter `MassTransit.RabbitMQ`, `MassTransit.AmazonSQS` e `MassTransit.MongoDb`
referenciados no mesmo projeto `Mecanica.Hermes.Infrastructure`, mesmo quando apenas um
está ativo em runtime.

**Justificativa:** o composition root escolhe o transport via env
`MASSTRANSIT__TRANSPORT_SERVICE`; manter todos no mesmo assembly simplifica o deploy e
evita projetos condicionais. Overhead de tamanho do container ~5 MB.

**Consequências:** revisitar quando o projeto tiver ≥ 3 transports distintos em uso
simultâneo ou quando a imagem precisar ser minimizada.

---

## ADR-016 — Contracts library (planejado)

**Status:** Planejado.

**Decisão:** Os contracts de mensageria (`*Request`, `Execute*`, `Operation*`) residem
em `Mecanica.Hermes.Application`. A extração para um pacote dedicado
`Mecanica.Hermes.Contracts` está planejada mas ainda não executada.

**Justificativa para esperar:** o único consumidor atual dos contracts é o próprio
serviço. Extrair antes de haver um segundo consumidor seria over-engineering. Critério de
extração: **primeiro consumidor externo confirmado**.

**Consequências:** até a extração, `Application` continua referenciando tipos MassTransit
apenas para os contracts (não para execução de pipeline). Ao extrair, criar
`Mecanica.Hermes.Contracts` como pacote NuGet versionado semanticamente.

---

## ADR-017 — `EnvironmentNames` centralizado em Domain

**Status:** Aceito.

**Decisão:** A classe `EnvironmentNames` vive em `Mecanica.Hermes.Domain.Common`,
disponível a qualquer camada sem dependência circular.

**Justificativa:** antes da migração, assemblies de Infrastructure não podiam referenciar
`EnvironmentNames` (que estava em Api). Domain não tem dependências externas, sendo o
local correto para constantes compartilhadas.

**Consequências:** o alias `Mecanica.Hermes.Api.EnvironmentNames` pode permanecer
temporariamente como wrapper `[Obsolete]` para retrocompatibilidade; remover quando todas
as referências migrarem.

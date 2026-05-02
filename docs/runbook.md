# Runbook

Guia operacional para os incidentes mais frequentes. Cada cenário lista **sintomas**,
**diagnóstico** e **mitigação**.

---

## 1. SAGA travada em `Processing`

### Sintomas

- Cliente reporta que requests para uma `OrdemDeServicoId` específica são "ignoradas".
- Métrica `ordemdeservico.etapa_avancada` para sem nenhum erro associado.
- Endpoints retornam `202 Accepted` mas o estado na OS não muda.

### Diagnóstico

1. Verificar a collection MongoDB da SAGA (`ordem-de-servico-saga-state` no database
   apontado por `MONGODB__SAGA_DATABASE`):

    ```js
    db.getCollection("ordem-de-servico-saga-state")
      .find({ CurrentState: "Processing" })
      .sort({ OperacaoIniciadaEm: 1 })
      .limit(20);
    ```

2. SAGAs em `Processing` por mais de **5 minutos** (`OperationTimeoutDelay`) deveriam ter
   sido auto-finalizadas pelo timeout sentinel — se ainda estão presas, suspeite que o
   `DelayedMessageScheduler` não está ativo.
3. Inspecionar logs do consumer responsável (`OperacaoEmAndamento` indica qual
   `*Request`/`Execute*` está em voo).

### Mitigação

- **Curto prazo:** publicar manualmente
  `OrdemDeServicoOperationFailed{OrdemDeServicoId, OperacaoEmAndamento, "manual unstick"}`
  para forçar a SAGA de volta a `Initial`.
- **Médio prazo:** garantir que o `DelayedMessageScheduler` está registrado e ativo
  (`AddDelayedMessageScheduler` + `UseDelayedMessageScheduler`). Veja
  [ADR-014](./adrs.md#adr-014).

---

## 2. Outbox acumulando mensagens

### Sintomas

- Tabela `OutboxMessages` cresce continuamente; `Status = 'Pending'` aumenta.
- Domain events não chegam aos handlers (métricas não atualizam).

### Diagnóstico

```sql
SELECT
  COUNT(*) FILTER (WHERE "Status" = 'Pending')      AS pendentes,
  COUNT(*) FILTER (WHERE "Status" = 'DeadLettered') AS dead_lettered,
  MAX("RetryCount")                                  AS max_retries,
  MIN("OccurredOn")                                  AS mais_antiga
FROM "OutboxMessages";
```

- `RetryCount > 0` em todas → handler está lançando exceção. Veja `LastError`.
- `RetryCount = 0` em todas → `OutboxProcessor` não está rodando.
- Muitas linhas em `DeadLettered` → erro determinístico sem fix → próximo passo é o
  cenário 4 (Domain Event dispatch falhando).

### Mitigação

- Conferir logs do BackgroundService (busca por `OutboxProcessor iniciado`).
- Em emergência: subir mais réplicas da API (cada instância roda um processor).
  `FOR UPDATE SKIP LOCKED` evita double-delivery entre elas.
- Mensagens em `DeadLettered` ficam paradas até intervenção manual:

    ```sql
    UPDATE "OutboxMessages"
       SET "Status" = 'Pending', "RetryCount" = 0, "LastError" = NULL
     WHERE "Status" = 'DeadLettered'
       AND "Id" IN (...);
    ```

---

## 3. Consumer envenenando (poison message → retry infinito)

### Sintomas

- Logs de erro repetidos para a mesma `OrdemDeServicoId`.
- DLQ no RabbitMQ enchendo (filas `*_error`).

### Diagnóstico

- Inspecionar `*_error` na UI do RabbitMQ; payload da mensagem revela o command ofensivo.
- Verificar se `OrdemDeServicoOperationFailed` foi publicado (a SAGA já trata regras de
  negócio falhando — não deveria ir para DLQ).

### Mitigação

- Mensagens em DLQ podem ser **reroutadas** após a correção (RabbitMQ shovel).
- Se a mensagem é estruturalmente inválida: descartar manualmente e abrir bug — o schema
  da mensagem não deve permitir o caso.

---

## 4. Domain Event dispatch falhando

### Sintomas

- `OutboxMessages.LastError` populado em massa após deploy.
- Métricas observability não atualizam.

### Diagnóstico

1. Verificar se algum **handler** novo foi introduzido com erro.
2. `LastError` típico: `Type 'X' não pôde ser resolvido` → o `DomainEventNameAttribute`
   foi removido ou trocado entre versões. (Lembrar: o discriminador é o nome do attribute,
   não o `AssemblyQualifiedName` — ver [ADR-013](./adrs.md#adr-013).)

### Mitigação

- Reintroduzir o nome canônico do evento (ou criar adapter) até processar o backlog.
- Após processar, marcar mensagens antigas como descartadas via `UPDATE` direto.

---

## 5. Banco de dados não migrado em deploy

### Sintomas

- API não sobe; logs mostram `relation "OrdensDeServico" does not exist`.

### Diagnóstico

```bash
dotnet ef migrations list \
  --project src/Mecanica.Hermes.Infrastructure \
  --startup-project src/Mecanica.Hermes.Api
```

### Mitigação

- A API roda `Database.MigrateAsync()` em startup (`MigrateDatabaseAsync`). Verifique logs
  para erros (FK constraint, valor inválido, etc.).
- Se `RUN_MIGRATIONS_ON_STARTUP=false`, rode as migrations manualmente em um pipeline
  separado.

---

## 6. Health checks falhando em Kubernetes

### Sintomas

- Pod entra em `CrashLoopBackOff` ou `Unhealthy`.

### Diagnóstico

- `kubectl exec` no pod e curl em:
  - `/healthz/live` — sempre OK; se falhar, problema do processo.
  - `/healthz/ready` — depende do PostgreSQL (`Ready` + `Database` tags).

### Mitigação

- Liveness falhando → restart automático do K8s cuida.
- Readiness falhando por mais de 5 min → checar conectividade com Postgres
  (`ConnectionStrings__DefaultConnection`).

---

## 7. Rate limit disparando inesperadamente (`429`)

### Sintomas

- Cliente recebe `429 Too Many Requests` sob carga normal.

### Diagnóstico

- Default: 60 req/min/IP. Configurável via `RATE_LIMIT_PERMIT_PER_MINUTE`.
- Verificar se há proxy reverso (NGINX, ingress, ALB) sem `X-Forwarded-For` — todos os
  requests podem aparecer com o mesmo IP.

### Mitigação

- Curto prazo: aumentar `RATE_LIMIT_PERMIT_PER_MINUTE`.
- Médio prazo: configurar `FORWARDED_HEADERS_KNOWN_PROXIES` (CSV de IPs/CIDRs do proxy)
  para que o ASP.NET use o IP real do cliente.

---

## 8. CORS bloqueando frontend em Production

### Sintomas

- Console do browser: `CORS policy: No 'Access-Control-Allow-Origin' header`.

### Mitigação

- Em `Production`, `CORS_ALLOWED_ORIGINS` (CSV) é obrigatório — sem isso nenhuma origem
  é permitida (fail-secure).
- Defina a env e reinicie o pod.

---

## 9. Conflitos de concorrência inesperados

### Sintomas

- Métrica `ordemdeservico.erros{tipo_erro="concurrency_conflict"}` cresce.
- `OperationFailed` com `"Conflito de concorrência. A operação será reprocessada."`.

### Diagnóstico

- Provavelmente a mesma OS está sendo modificada por *múltiplos* fluxos. A SAGA já
  serializa por `OrdemDeServicoId` — se isso ocorre, alguém pode estar publicando
  `Execute*` direto, fora do fluxo da SAGA.

### Mitigação

- Verificar se o redelivery do MassTransit funciona (default exponential backoff). O
  pipeline base já trata `ConcurrencyConflictException` publicando `OperationFailed`
  e deixando a SAGA voltar para `Initial`; o cliente pode re-emitir o request.

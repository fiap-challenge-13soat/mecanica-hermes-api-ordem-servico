# Referência da API

Este documento complementa a documentação interativa. Para experimentar os endpoints,
abra o Scalar UI em <http://localhost:5016/scalar> com a API rodando.

## Convenções

- **Base path:** `/api/ordens-de-servico`
- **Content type:** `application/json`
- **Status codes**:
  - `200 OK` — leitura bem-sucedida.
  - `202 Accepted` — comando enfileirado para processamento assíncrono. O id da OS é
    retornado mesmo que o agregado ainda não esteja persistido.
  - `400 Bad Request` — falha de validação ou de regra de domínio (Problem Details RFC 7807).
  - `404 Not Found` — recurso inexistente.
  - `429 Too Many Requests` — rate limit (default: 60 req/min/IP).
- **Erro:** corpo segue **RFC 7807** (`application/problem+json`). Veja
  `Mecanica.Hermes.Api.Presenter.ApiProblemDetails`.
- **Correlation ID:** o middleware `CorrelationIdMiddleware` injeta/propaga o header
  `X-Correlation-Id` em toda resposta.

## Autenticação

JWT Bearer com OIDC. O endpoint exige um dos *scopes* abaixo no claim `scope`:

| Policy | Descrição | Var de ambiente |
| --- | --- | --- |
| `OnlyAdminScope` | Apenas admin pode mutar | `AUTH__ADMIN_SCOPE` |
| `AllowClienteScope` | Cliente OU admin pode ler | `AUTH__CLIENTE_SCOPE` |

> Em `Development`/`Testing`, o `DevelopmentAuthenticationMiddleware` autentica todas as
> requisições com claims sintéticas — não é necessário JWT real.

## Endpoints

### Criar ordem de serviço

```http
POST /api/ordens-de-servico
Authorization: Bearer <admin-scope>
Content-Type: application/json

{
  "clienteId": "11111111-1111-1111-1111-111111111111",
  "veiculoId": "22222222-2222-2222-2222-222222222222",
  "problemaRelatado": "Barulho no motor em marcha lenta",
  "observacoes": "Carro deixado às 9h"
}
```

Resposta `202 Accepted`:

```http
HTTP/1.1 202 Accepted
Location: /api/ordens-de-servico/<id>
Content-Type: application/json

{
  "id": "33333333-3333-3333-3333-333333333333"
}
```

> O `id` é gerado **na camada de API** antes de publicar a request, para que o cliente
> possa fazer polling em `GET /api/ordens-de-servico/{id}` enquanto a SAGA conclui.

### Buscar ordem de serviço por id

```http
GET /api/ordens-de-servico/{id}
```

Retorna `OrdemDeServicoResponse` com `Id`, `ClienteId`, `VeiculoId`, `ProblemaRelatado`,
`Observacoes`, `StatusAtual` (string), `ValorTotal`, listas de `Produtos`, `Servicos` e
`HistoricosStatus`, mais `CreatedAt` / `UpdatedAt`.

### Listar ordens de serviço

```http
GET /api/ordens-de-servico?page=1&pageSize=20
GET /api/ordens-de-servico/status/{status}?page=1&pageSize=20
```

`status` é o nome do enum `OrdemDeServicoStatus` (`Recebida`, `EmDiagnostico`,
`AguardandoAprovacao`, …). Resposta paginada via `PaginatedResponse<T>`.

### Adicionar produto

```http
POST /api/ordens-de-servico/{id}/produtos
Authorization: Bearer <admin-scope>
Content-Type: application/json

{
  "produtoId": "...",
  "valorUnitario": 199.90,
  "quantidade": 2
}
```

Resposta: `202 Accepted`.

### Remover produto

```http
DELETE /api/ordens-de-servico/{id}/produtos/{produtoId}
```

Resposta: `202 Accepted`.

### Adicionar / remover serviço

```http
POST   /api/ordens-de-servico/{id}/servicos
DELETE /api/ordens-de-servico/{id}/servicos/{servicoId}
```

`POST` body:

```json
{
  "servicoId": "...",
  "valorUnitario": 350.00
}
```

### Transições de estado

| Operação | Rota | Quando usar |
| --- | --- | --- |
| Avançar etapa | `PATCH /api/ordens-de-servico/{id}/avancar` | Move para `ProximoStatus` |
| Reverter etapa | `PATCH /api/ordens-de-servico/{id}/reverter` | Volta para `StatusRevertido` |
| Aprovar | `PATCH /api/ordens-de-servico/{id}/aprovar` | Apenas em `AguardandoAprovacao` (avança) |
| Rejeitar | `PATCH /api/ordens-de-servico/{id}/rejeitar` | Apenas em `AguardandoAprovacao` (vai para `Rejeitada`) |
| Pagamento aprovado | `PATCH /api/ordens-de-servico/{id}/pagamento-aprovado` | Apenas em `AguardandoPagamento` (avança para `PagamentoConfirmado`) |
| Cancelar | `PATCH /api/ordens-de-servico/{id}/cancelar` | Qualquer estado não-terminal |

Todas respondem `202 Accepted` sem body.

## Semântica assíncrona

Resumindo o ciclo após um `202 Accepted`:

1. O endpoint envia um command para o MediatR.
2. `AsyncCommandHandler<T>` publica um `*Request` no bus e devolve `Result.Accepted`.
3. A SAGA consume o `*Request`, transita para `Processing`, agenda o timeout (5 min) e
   publica o `Execute*Command`.
4. Um consumer dedicado processa, persiste e publica `OrdemDeServicoOperation{Completed,Failed}`.
5. A SAGA volta para `Initial` e cancela o timeout.
6. O cliente faz polling em `GET /api/ordens-de-servico/{id}` (status atualizado lá).

Se uma operação está em voo para um determinado `OrdemDeServicoId`, novas `*Request` para
o mesmo id são **silenciosamente descartadas pela SAGA** até a operação corrente terminar.

Para *consumers externos* que queiram reagir a eventos, escutem
`OrdemDeServicoOperationCompleted/Failed` no bus.

## Health checks

### Endpoints

| Endpoint | Propósito | Predicate |
| --- | --- | --- |
| `GET /healthz/live` | Liveness probe (Kubernetes) | Nenhum — retorna sempre `200 Healthy` |
| `GET /healthz/ready` | Readiness probe (Kubernetes) | Checks com tag `ready` |
| `GET /health` | Alias operacional de `/healthz/ready` | Idem |

### Checks registrados

| Nome | Tag(s) | `FailureStatus` | Ativo quando |
| --- | --- | --- | --- |
| `postgres` | `ready`, `database` | `Unhealthy` | Sempre (conexão PostgreSQL obrigatória) |
| `masstransit-bus` | `ready`, `messaging` | `Degraded` | `MASSTRANSIT__TRANSPORT_SERVICE` = `RabbitMq` ou `AmazonSqs` |
| `mongodb-saga` | `ready`, `saga` | `Degraded` | `MONGODB__CONNECTION_STRING` definida |

> **`Unhealthy` vs `Degraded`:**  
> `postgres` usa `Unhealthy` porque sem banco a aplicação não funciona.  
> `masstransit-bus` e `mongodb-saga` usam `Degraded` porque o MassTransit
> possui lógica de reconexão automática — um broker temporariamente indisponível
> não deve retirar o pod imediatamente do pool do load balancer.

### Implementação

- **`postgres`** — pacote `AspNetCore.HealthChecks.NpgSql`; executa `SELECT 1`.
- **`masstransit-bus`** — registrado internamente pelo `AddMassTransit` via
  `ConfigureHealthCheckOptions`. Reflete o estado real do bus (conectado / reconectando).
  Não é registrado quando transport = `None` (testes / desenvolvimento sem broker).
- **`mongodb-saga`** — `MongoDbSagaHealthCheck` customizado em `Infrastructure/HealthChecks/`;
  executa `db.adminCommand({ ping: 1 })`. Usa um `MongoClient` cacheado por connection string
  (thread-safe, evita criar conexões a cada probe). Não é registrado quando
  `MONGODB__CONNECTION_STRING` está ausente (SAGA in-memory).

### Exemplo de resposta (`/healthz/ready` com todos os checks)

```json
{
  "status": "Healthy",
  "checks": [
    { "name": "postgres",        "status": "Healthy",  "tags": ["ready", "database"]  },
    { "name": "masstransit-bus", "status": "Healthy",  "tags": ["ready", "messaging"] },
    { "name": "mongodb-saga",    "status": "Healthy",  "tags": ["ready", "saga"]      }
  ]
}
```

> O formato acima requer um `ResponseWriter` customizado; o comportamento padrão do
> ASP.NET Core retorna apenas `Healthy` / `Degraded` / `Unhealthy` como texto plano.

## Rate limiting

- **Default:** 60 req/min/IP (fixed window, sem fila).
- **Variável:** `RATE_LIMIT_PERMIT_PER_MINUTE`.
- **Em `Testing`** o limite é `int.MaxValue` para evitar flakiness.
- **Atrás de proxy/ingress:** configure `FORWARDED_HEADERS_KNOWN_PROXIES` (CSV de IPs/CIDRs)
  para que o IP real seja lido de `X-Forwarded-For`.

## CORS

- **`Development` / `Testing`:** libera qualquer origem.
- **`Production`:** exige `CORS_ALLOWED_ORIGINS` (CSV). Sem essa variável, *nenhuma* origem
  é permitida (fail-secure).

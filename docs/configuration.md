# Configuração

A API é configurada exclusivamente por **variáveis de ambiente** (sem `appsettings.json`
versionado). Para desenvolvimento local, mantenha um `src/Mecanica.Hermes.Api/Properties/launchSettings.json`
local — esse arquivo **não é versionado** e não deve ser commitado, pois carrega chaves
de licença e secrets de dev. Use o quadro abaixo como referência das variáveis suportadas.

## Quadro consolidado

### Banco de dados

| Variável | Default | Notas |
| --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | — | Connection string Npgsql obrigatória. |
| `RUN_MIGRATIONS_ON_STARTUP` | `true` | Quando `false`, o startup pula `Database.MigrateAsync`. |

### Autenticação JWT (OIDC)

| Variável | Obrigatória | Notas |
| --- | --- | --- |
| `AUTH__AUTHORITY` | em `Production` | Endpoint do provedor OIDC. |
| `AUTH__ISSUER` | em `Production` | `iss` esperado no JWT. |
| `AUTH__CLIENTE_SCOPE` | em `Production` | Nome do scope da policy `AllowClienteScope`. |
| `AUTH__ADMIN_SCOPE` | em `Production` | Nome do scope da policy `OnlyAdminScope`. |

> Em `Development` e `Testing`, o `DevelopmentAuthenticationMiddleware` autentica todas
> as requisições — nenhuma variável `AUTH__*` é necessária.

### MassTransit / mensageria

| Variável | Default | Notas |
| --- | --- | --- |
| `MASSTRANSIT__TRANSPORT_SERVICE` | `AmazonMq` (quando ausente) | `AmazonMq` (broker gerenciado AWS, default), `RabbitMq` (auto-hospedado), `None` (testes), `AmazonSqs`. |
| `MASSTRANSIT__CONNECTION_STRING` | — | Conexão do transport escolhido. AmazonMq requer `amqps://<user>:<password>@<broker-id>.mq.<region>.amazonaws.com:5671`. RabbitMq aceita `amqp://<user>:<password>@<host>:5672/`. Credenciais devem vir de Secret Manager / Kubernetes Secret. |
| `MASSTRANSIT__PREFETCH_COUNT` | `24` | Mensagens em flight por consumer. |
| `MASSTRANSIT__QUEUE_NAME_PREFIX` | `mecanica-hermes` | Prefixo de filas. |

### Estado da SAGA (MongoDB)

| Variável | Notas |
| --- | --- |
| `MONGODB__CONNECTION_STRING` | Connection string do MongoDB ou DocumentDB. |
| `MONGODB__SAGA_DATABASE` | Nome do database (ex.: `mechermes_saga`). |

### Outbox

| Variável | Default | Notas |
| --- | --- | --- |
| `OUTBOX__POLL_INTERVAL` | `00:00:02` | Intervalo entre polls. |
| `OUTBOX__BATCH_SIZE` | `50` | Tamanho do lote por ciclo. |
| `OUTBOX__MAX_RETRIES` | constante interna | Após exceder, mensagem vira `DeadLettered`. |
| `OUTBOX__PROCESSED_RETENTION` | `7.00:00:00` | Retenção antes do cleanup. |
| `OUTBOX__ALLOW_NON_LOCKING_FALLBACK` | `false` | Habilitar **apenas em testes** (provider InMemory). |

### Observabilidade (OpenTelemetry / OTLP)

A API segue a [especificação OTLP](https://opentelemetry.io/docs/specs/otel/protocol/exporter/).

| Variável | Notas |
| --- | --- |
| `OBSERVABILITY__ENABLED` | `true` para ativar exportador OTLP. |
| `OTEL_SERVICE_NAME` | Nome reportado em traces/logs/métricas. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Ex.: `https://otlp.nr-data.net:4318` (New Relic). |
| `OTEL_EXPORTER_OTLP_HEADERS` | Ex.: `api-key=<license-key>`. |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | Recomendado `http/protobuf`. |
| `OTEL_EXPORTER_OTLP_LOGS_ENDPOINT` | Override só para logs (opcional). |
| `OTEL_EXPORTER_OTLP_LOGS_HEADERS` | Override só para logs (opcional). |
| `OTEL_EXPORTER_OTLP_LOGS_PROTOCOL` | Override só para logs (opcional). |

### CORS, rate limit e proxy

| Variável | Default | Notas |
| --- | --- | --- |
| `CORS_ALLOWED_ORIGINS` | — | CSV de origens permitidas em `Production` (fail-secure). |
| `RATE_LIMIT_PERMIT_PER_MINUTE` | `60` | Limite por IP. Em `Testing` é forçado a `int.MaxValue`. |
| `FORWARDED_HEADERS_KNOWN_PROXIES` | — | CSV de IPs/CIDRs de proxies confiáveis. |

### Licenças

| Variável | Notas |
| --- | --- |
| `MediatR__LicenseKey` | License key do MediatR. |

### Ambiente

`ASPNETCORE_ENVIRONMENT` decide o pipeline:

- `Development` — Scalar UI ativa, dev auth bypass, CORS aberto, Forwarded Headers aceita
  qualquer proxy.
- `Testing` — usado pelos testes de integração; idem ao Development com algumas
  diferenças (ex.: rate limit `int.MaxValue`, sem Serilog).
- `Production` (default no Dockerfile) — JWT obrigatório, CORS exige `CORS_ALLOWED_ORIGINS`,
  Forwarded Headers só confia em proxies declarados.

## Exemplo mínimo (Development)

Modelo para o seu `launchSettings.json` local (substitua os placeholders):

```text
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=MecHermesOrdemDeServicoDB;Username=postgres;Password=<sua-senha>
# Dev local com RabbitMQ em container:
MASSTRANSIT__CONNECTION_STRING=amqp://guest:guest@localhost:5672/
MASSTRANSIT__TRANSPORT_SERVICE=RabbitMq
# Em produção (default): aponta para Amazon MQ via AMQPS sobre TLS, secret puxado do Secrets Manager.
# MASSTRANSIT__CONNECTION_STRING=amqps://mechermes:****@b-xxx.mq.us-east-1.amazonaws.com:5671
# MASSTRANSIT__TRANSPORT_SERVICE=AmazonMq
MASSTRANSIT__PREFETCH_COUNT=24
MASSTRANSIT__QUEUE_NAME_PREFIX=mecanica-hermes
MONGODB__CONNECTION_STRING=mongodb://localhost:27017
MONGODB__SAGA_DATABASE=mechermes_saga
OBSERVABILITY__ENABLED=true
OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp.nr-data.net:4318
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_EXPORTER_OTLP_HEADERS=api-key=<sua-license-key>
MediatR__LicenseKey=<sua-license-key>
```

> Nunca comite license keys reais — em produção use secrets do orquestrador
> (Kubernetes Secrets, AWS Secrets Manager, etc.). `launchSettings.json` **não** está
> versionado neste repositório justamente para evitar vazamento.

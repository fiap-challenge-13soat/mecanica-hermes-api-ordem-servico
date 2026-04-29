# Observabilidade

A API exporta **logs, traces e métricas** via OpenTelemetry, no protocolo OTLP/HTTP. A
configuração padrão exporta para o New Relic, mas qualquer collector OTLP serve
(Tempo, Honeycomb, Grafana Cloud, etc.).

## Componentes

| Sinal | Como é coletado | Para onde |
| --- | --- | --- |
| **Logs** | Serilog → `Serilog.Sinks.OpenTelemetry` | OTLP `/v1/logs` |
| **Traces** | OpenTelemetry SDK + instrumentações (ASP.NET Core, HttpClient, Npgsql) | OTLP `/v1/traces` |
| **Métricas** | OpenTelemetry SDK + `Meter`s do app | OTLP `/v1/metrics` |

A composição vive em `Mecanica.Hermes.Infrastructure/Observability/`:

- `ObservabilityConfiguration.cs` — registra OTel no DI.
- `OrdemDeServicoMetrics.cs` — `Meter` `MecanicaHermes.OrdemDeServico` + counters/histograms.
- `Handlers/` — handlers MediatR que escutam Domain Events e atualizam métricas.
- `ActivitySources.cs` — constantes para `ActivitySource`.
- `OtlpEnvironment.cs` / `EnvironmentDetector.cs` — leitura/validação das env vars OTLP.

Para a configuração das variáveis OTLP, ver
[`configuration.md`](./configuration.md#observabilidade-opentelemetry--otlp).

## Métricas emitidas

Sob o **Meter** `MecanicaHermes.OrdemDeServico`:

| Métrica | Tipo | Tags | Significado |
| --- | --- | --- | --- |
| `ordemservico.criadas` | Counter (long) | — | Número de OS criadas com sucesso. Emitida pelo handler de `OrdemDeServicoCriadoEvent`. |
| `ordemservico.etapa_avancada` | Counter (long) | `status_anterior`, `status_novo`, `tipo_movimentacao` | Transições bem-sucedidas. `tipo_movimentacao` ∈ `{etapa_avancada, etapa_revertida}`. |
| `ordemservico.canceladas` | Counter (long) | `status_anterior` | Cancelamentos. |
| `ordemservico.erros` | Counter (long) | `operacao`, `tipo_erro` | Erros por operação. `operacao` é o `OperacaoMetricKey` do consumer (ex.: `criar`, `avancar_etapa`). |
| `ordemservico.duracao_etapa_minutes` | Histogram (double, minutos) | `status` | Duração que a OS permaneceu em um determinado status (medida ao avançar). |

### Convenções

- Tags **baixa-cardinalidade**. Não use `OrdemDeServicoId`, `ClienteId` ou `VeiculoId`
  como tag.
- Status são serializados a partir do enum (`Recebida`, `EmDiagnostico`, …).
- `tipo_erro` ∈ `{nao_encontrada, status_invalido, validation_error, concurrency_conflict}`.

> Operações canônicas usadas na tag `operacao`: `criar`, `avancar_etapa`, `reverter_etapa`,
> `aprovar`, `rejeitar`, `pagamento_aprovado`, `dispatch_event`. Definidas em
> `OrdemDeServicoMetricKeys.Operations`.

### Consultas Prometheus / Grafana

```promql
# Taxa de OS criadas por minuto (5m janela)
rate(ordemservico_criadas_total[5m]) * 60

# Top 5 transições mais frequentes
topk(5, sum by (status_anterior, status_novo) (rate(ordemservico_etapa_avancada_total[1h])))

# Erros por operação (últimos 5 min)
sum by (operacao, tipo_erro) (rate(ordemservico_erros_total[5m]))

# P95 da duração por status (últimos 15 min)
histogram_quantile(0.95, sum by (le, status) (rate(ordemservico_duracao_etapa_minutes_bucket[15m])))
```

## Tracing

Activity sources principais (em `ActivitySources.cs`):

- `MecanicaHermes.OrdemDeServico` — spans de comandos/queries (criados pelo
  `TracingBehavior`).
- Instrumentação automática de ASP.NET Core, HttpClient, Npgsql via pacotes OTel.

O Outbox preserva o `traceparent` ativo no momento do commit em `OutboxMessages.TraceContext`
e o restaura ao despachar — assim, traces continuam contínuos mesmo cruzando a fronteira
do background processor (ver [ADR-014](./adrs.md#adr-014)).

## Logs

- **Serilog** com enrichers de span, correlation id, ambiente, machine name.
- Em `Testing` o Serilog é **desabilitado** para não poluir o output dos testes
  (`builder.AddSerilogConfiguration` é pulado).
- Logs estruturados são exportados para o backend OTLP configurado.
- O middleware `CorrelationIdMiddleware` injeta o header `X-Correlation-Id` em todas as
  respostas; o id também é colocado no `LogContext`.

## Health checks

- `GET /healthz/live` — sempre saudável (apenas o "self" check).
- `GET /healthz/ready` — agrega checks com tag `Ready` (incluindo PostgreSQL).
- `GET /health` — alias de `/healthz/ready`.

## Adicionar uma nova métrica

1. Defina a chave em `OrdemDeServicoMetricKeys` (snake_case).
2. Adicione o método na interface `IOrdemDeServicoMetrics`.
3. Implemente em `OrdemDeServicoMetrics` (use `Meter.CreateCounter`/`CreateHistogram`).
4. Crie o handler MediatR correspondente em
   `Infrastructure/Observability/Handlers/`.
5. Atualize a tabela acima e adicione teste em
   `tests/Mecanica.Hermes.Infrastructure.Tests/Observability/MetricsHandlersTests.cs`.

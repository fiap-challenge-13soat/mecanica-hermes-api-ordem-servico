# Mecânica Hermes — API de Ordem de Serviço

> Backend HTTP que orquestra o ciclo de vida das ordens de serviço de uma oficina mecânica:
> abertura, diagnóstico, aprovação, execução, pagamento e entrega.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-18-336791)](https://www.postgresql.org/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-4-FF6600)](https://www.rabbitmq.com/)
[![MongoDB](https://img.shields.io/badge/MongoDB-SAGA%20state-47A248)](https://www.mongodb.com/)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=fiap-challenge-13soat_mecanica-hermes-api-ordem-servico&metric=alert_status&token=6aa5812f4a2c7616442ace938524a8b0999b56c3)](https://sonarcloud.io/summary/new_code?id=fiap-challenge-13soat_mecanica-hermes-api-ordem-servico)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=fiap-challenge-13soat_mecanica-hermes-api-ordem-servico&metric=coverage&token=6aa5812f4a2c7616442ace938524a8b0999b56c3)](https://sonarcloud.io/summary/new_code?id=fiap-challenge-13soat_mecanica-hermes-api-ordem-servico)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=fiap-challenge-13soat_mecanica-hermes-api-ordem-servico&metric=sqale_rating&token=6aa5812f4a2c7616442ace938524a8b0999b56c3)](https://sonarcloud.io/summary/new_code?id=fiap-challenge-13soat_mecanica-hermes-api-ordem-servico)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=fiap-challenge-13soat_mecanica-hermes-api-ordem-servico&metric=reliability_rating&token=6aa5812f4a2c7616442ace938524a8b0999b56c3)](https://sonarcloud.io/summary/new_code?id=fiap-challenge-13soat_mecanica-hermes-api-ordem-servico)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=fiap-challenge-13soat_mecanica-hermes-api-ordem-servico&metric=security_rating&token=6aa5812f4a2c7616442ace938524a8b0999b56c3)](https://sonarcloud.io/summary/new_code?id=fiap-challenge-13soat_mecanica-hermes-api-ordem-servico)

---

## Sumário

- [O que este serviço faz](#o-que-este-serviço-faz)
- [Stack](#stack)
- [Pré-requisitos](#pré-requisitos)
- [Início rápido](#início-rápido)
- [Endpoints](#endpoints)
- [Estrutura do repositório](#estrutura-do-repositório)
- [Documentação](#documentação)
- [Equipe](#equipe)

---

## O que este serviço faz

A API expõe um único agregado de domínio — **`OrdemDeServico`** — e os comandos que fazem
ele transitar entre estados (`Recebida → EmDiagnostico → AguardandoAprovacao → EmExecucao →
ManutencaoFinalizada → AguardandoPagamento → PagamentoConfirmado → Entregue`).

Características importantes:

- **Endpoints de mutação são assíncronos:** o cliente recebe `202 Accepted` imediatamente; o
  trabalho é encaminhado por uma SAGA do MassTransit que serializa as operações por
  `OrdemDeServicoId` (no máximo uma operação em voo por OS).
- **Outbox transacional:** *domain events* são persistidos na mesma transação do agregado e
  despachados por um `BackgroundService`, garantindo entrega *at-least-once*.
- **State Pattern no domínio:** cada status é uma classe própria que conhece suas próprias
  transições e permissões de edição.
- **Autenticação JWT + scopes** com bypass automático em `Development`/`Testing`.
- **OpenTelemetry** para logs, traces e métricas (exporta via OTLP/HTTP — testado contra
  New Relic).

Para detalhes de fluxo, mensageria e arquitetura, ver [`docs/`](./docs/).

## Stack

- **.NET 10** + ASP.NET Core (Minimal API) + C# 13
- **PostgreSQL 16** com Entity Framework Core 10 (Npgsql)
- **MassTransit 8** (transports suportados: RabbitMQ, Amazon SQS, MongoDB)
- **MongoDB** para persistência de estado da SAGA
- **MediatR 14** + **Autofac 9** + **FluentValidation 11**
- **OpenTelemetry** (OTLP → New Relic) + **Serilog**
- **xUnit / FluentAssertions / Moq / Bogus / Testcontainers** para testes

## Pré-requisitos

| Ferramenta | Versão | Necessidade |
| --- | --- | --- |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0+ | obrigatória |
| [Docker](https://www.docker.com/get-started) | qualquer atual | testes de integração + serviços de dev |
| [just](https://github.com/casey/just) | qualquer | opcional — atalhos de build/test |

Para rodar a API localmente sem Docker, é preciso ter PostgreSQL 16, RabbitMQ e MongoDB
acessíveis (ou substituir o transport — ver [`docs/configuration.md`](./docs/configuration.md)).

## Início rápido

### 1. Subir as dependências de runtime

A API depende de **PostgreSQL**, **RabbitMQ** (ou outro transport MassTransit) e **MongoDB**
(persistência da SAGA). Suba o que preferir via Docker:

```bash
docker run -d --name hermes-postgres -p 5432:5432 \
  -e POSTGRES_PASSWORD=<sua-senha> -e POSTGRES_DB=MecHermesOrdemDeServicoDB postgres:16

docker run -d --name hermes-rabbitmq -p 5672:5672 -p 15672:15672 \
  rabbitmq:3-management

docker run -d --name hermes-mongo -p 27017:27017 mongo:7
```

> Os comandos acima são suficientes para rodar **apenas esta API** isoladamente. Para o
> **ecossistema completo** (3 APIs + WireMock como mock do Mercado Pago + observabilidade),
> consulte [Stack completa via Docker Compose](#stack-completa-via-docker-compose) abaixo.

### 2. Rodar a API

```bash
# usando o seu launchSettings.json local (perfil "http")
dotnet run --project src/Mecanica.Hermes.Api

# ou via just
just run
```

> O `launchSettings.json` **não** é versionado — cada contribuidor mantém o seu próprio
> com as variáveis listadas em [`docs/configuration.md`](./docs/configuration.md).

A primeira execução aplica as migrações automaticamente (`MigrateDatabaseAsync`).

### 3. URLs locais

| Recurso | URL |
| --- | --- |
| API | <http://localhost:5016> |
| Documentação interativa (Scalar) | <http://localhost:5016/scalar> |
| OpenAPI JSON | <http://localhost:5016/openapi/v1.json> |
| Liveness | <http://localhost:5016/healthz/live> |
| Readiness | <http://localhost:5016/healthz/ready> |
| RabbitMQ Management | <http://localhost:15672> (`guest` / `guest`) |

> Em `Development` e `Testing`, todas as requisições são autenticadas automaticamente pelo
> `DevelopmentAuthenticationMiddleware` — não é necessário token JWT.

### 4. Rodar os testes

```bash
# tudo (requer Docker — testes de integração usam Testcontainers)
dotnet test Mecanica.Hermes.Ordem.Servico.sln

# apenas unitários (sem Docker)
just test-unit
```

Cobertura mínima exigida pelo CI: **≥ 80%** de linhas. Veja
[`docs/testing.md`](./docs/testing.md) para detalhes.

## Stack completa via Docker Compose

O `docker-compose.yml` da stack inteira do ecossistema Mecânica Hermes — Postgres,
MongoDB (com replica set), RabbitMQ (com plugin de delayed exchange), WireMock (mock
do Mercado Pago) e as 3 APIs (`api-ordem-servico`, `api-cadastros`, `api-pagamentos`)
— **vive no repositório de testes E2E**,
[`mecanica-hermes-tests-e2e`](https://github.com/fiap-challenge-13soat/mecanica-hermes-tests-e2e).
É a forma canônica de rodar o ecossistema localmente sem precisar buildar cada API:

```bash
git clone https://github.com/fiap-challenge-13soat/mecanica-hermes-tests-e2e.git ../mecanica-hermes-tests-e2e
cd ../mecanica-hermes-tests-e2e
docker compose \
  -f docker-compose/docker-compose.yaml \
  -f docker-compose/docker-compose.e2e.yaml \
  up -d --wait
```

A imagem desta API consumida pelo compose vem do Docker Hub
(`mechermes/mecanica-hermes-api-ordem-servico:latest`), republicada automaticamente
pelo workflow [`Build and Publish Docker Image`](./.github/workflows/docker-ci.yml) a
cada merge na `main`. Para apontar para uma tag específica, exporte
`OS_IMAGE_TAG=<sha-curto-ou-versão>` antes do `compose up`.

A suíte E2E que valida os fluxos cross-service entre as 3 APIs também vive lá. Para
detalhes operacionais (variáveis, healthchecks, troubleshooting), consulte o README
e o `CLAUDE.md` do próprio repositório E2E.

## Endpoints

Base path: `/api/ordens-de-servico`.

| Método | Rota | Auth scope | Descrição |
| --- | --- | --- | --- |
| `POST` | `/` | admin | Criar OS (assíncrono → `202 Accepted`) |
| `GET` | `/{id}` | cliente, admin | Buscar OS por id |
| `GET` | `/` | cliente, admin | Listar OS com paginação |
| `GET` | `/status/{status}` | cliente, admin | Listar OS filtrando por status |
| `POST` | `/{id}/produtos` | admin | Adicionar produto (assíncrono) |
| `DELETE` | `/{id}/produtos/{produtoId}` | admin | Remover produto (assíncrono) |
| `POST` | `/{id}/servicos` | admin | Adicionar serviço (assíncrono) |
| `DELETE` | `/{id}/servicos/{servicoId}` | admin | Remover serviço (assíncrono) |
| `PATCH` | `/{id}/avancar` | admin | Avançar etapa (assíncrono) |
| `PATCH` | `/{id}/reverter` | admin | Reverter etapa (assíncrono) |
| `PATCH` | `/{id}/pagamento-aprovado` | admin | Confirmar pagamento (assíncrono — exige `AguardandoPagamento`) |
| `PATCH` | `/{id}/cancelar` | admin | Cancelar OS (assíncrono) |

**Endpoints de mutação retornam `202 Accepted`** — não esperam o consumer concluir. Veja
[`docs/api-reference.md`](./docs/api-reference.md) para o ciclo "publish → consume → query".

## Estrutura do repositório

```text
.
├── src/
│   ├── Mecanica.Hermes.Api/             Minimal API, middleware, presenter
│   ├── Mecanica.Hermes.Application/     CQRS, MediatR, MassTransit consumers, ports
│   ├── Mecanica.Hermes.Domain/          Agregados, Value Objects, Domain Events
│   └── Mecanica.Hermes.Infrastructure/  EF Core, repositórios, Outbox, SAGA, OpenTelemetry
├── tests/
│   ├── Mecanica.Hermes.Domain.Tests/
│   ├── Mecanica.Hermes.Application.Tests/
│   ├── Mecanica.Hermes.Infrastructure.Tests/
│   ├── Mecanica.Hermes.Api.Tests/
│   ├── Mecanica.Hermes.IntegrationTests/      WebApplicationFactory + Testcontainers
│   ├── Mecanica.Hermes.ArchitectureTests/     NetArchTest — regras de dependência
│   └── Mecanica.Hermes.TestUtils/             builders compartilhados (não roda)
├── docs/                                Documentação detalhada
├── .github/workflows/                   CI: build+tests, docker, k8s deploy trigger
├── Dockerfile                           multi-stage (.NET 10 SDK → aspnet:10)
├── Directory.Build.props                propriedades MSBuild compartilhadas
├── Directory.Packages.props             versões NuGet centralizadas (CPM)
├── coverlet.runsettings                 configuração de cobertura
└── justfile                             atalhos para build, test, run, migrations
```

## Documentação

Documentação técnica detalhada está em [`docs/`](./docs/):

- [Arquitetura](./docs/architecture.md) — camadas, padrões, dependências
- [Fluxo de Ordem de Serviço](./docs/service-order-flow.md) — máquina de estados
- [Mensageria](./docs/messaging.md) — SAGA, Outbox, contratos, pipeline de consumers
- [Banco de dados](./docs/database.md) — modelo, índices, migrations
- [Referência da API](./docs/api-reference.md) — endpoints, auth, semântica assíncrona
- [Configuração](./docs/configuration.md) — variáveis de ambiente
- [Observabilidade](./docs/observability.md) — métricas, traces, logs
- [Testes](./docs/testing.md) — estratégia e convenções
- [CI/CD](./docs/ci-cd.md) — pipelines GitHub Actions
- [Runbook](./docs/runbook.md) — incidentes operacionais comuns
- [ADRs](./docs/adrs.md) — registros de decisão arquitetural
- [Contribuição](./docs/contributing.md) — guia para novos contribuidores

## Equipe

Desenvolvido pelo grupo do **Tech Challenge 13SOAT — FIAP**.

| Nome | RM | Discord | Email |
| --- | --- | --- | --- |
| Allef Cleiton de F. Campos | 367214 | allefcampos2179 | <allef-cleiton88@hotmail.com> |
| Leandro Borgo Battochio | 368100 | sheenbr | <lbattochio@hotmail.com> |
| Matheus Alonso Scherma | 368955 | mscherma | <matheus.scherma@gmail.com> |
| Rogério dos Santos Gelonezi | 368238 | rogergelonezi | <rogeriogelonezi@gmail.com> |

## Licença

[MIT](./LICENSE).

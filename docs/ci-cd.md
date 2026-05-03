# CI / CD

Três workflows do GitHub Actions automatizam build, publicação e deploy.

```mermaid
flowchart LR
    A[push main / feature/**] --> B[build-and-tests-ci]
    B -- success on main --> C[docker-ci]
    D[push tag v*.*.*] --> C
    C -- success on main --> E[k8s-trigger-cd HML]
    C -- success on tag v* --> F[k8s-trigger-cd PRD]
```

## Workflow 1 — `build-and-tests-ci.yml`

**Triggers:** push em `main` e `feature/**`.

### Etapas

1. Checkout (`actions/checkout`).
2. Setup .NET 10 (`actions/setup-dotnet`).
3. Cache de NuGet (chave: `runner.os` + hash de `Directory.Packages.props`).
4. `dotnet restore`.
5. `dotnet build --configuration Release --no-restore`.
6. **Verificação de Docker** (`docker --version` + `docker ps`) — necessário para os
   testes de integração (Testcontainers).
7. Instala `dotnet-reportgenerator-globaltool`.
8. `dotnet test ... --settings coverlet.runsettings` — gera cobertura em formato OpenCover
   em `.temp/coverage`.
9. ReportGenerator gera HTML + `Summary.json` (filtra `*.g.cs`, `*Designer.cs`,
   `Migrations/*.cs`).
10. Publica artefato `cobertura-html` (retenção 7 dias).
11. **Threshold ≥ 80%** de cobertura de linhas. Pipeline falha se abaixo.

> Não há secrets adicionais — só os tokens padrão do GitHub Actions.

## Workflow 2 — `docker-ci.yml`

**Triggers:**

- `workflow_run` quando o workflow 1 completa **com sucesso na branch `main`** (publica o
  commit exato que passou no CI, não o `HEAD` corrente da `main`).
- `push` em tags `v*.*.*` (releases — o tag em si é o gate).

Branches `feature/**` **não** publicam imagem; só viram imagem quando mergeadas em `main`.

### Imagem publicada

```text
mechermes/mecanica-hermes-api-ordem-servico:<tag>
```

Tags geradas (via `docker/metadata-action`):

| Tag | Quando |
| --- | --- |
| `sha-<short>` | Toda imagem publicada |
| `{{version}}`, `{{major}}.{{minor}}`, `{{major}}` | Em push de tag SemVer (ex.: `v1.2.3`) |
| `latest` | Apenas em build vindo do gate de CI na main |

### Plataformas

`linux/amd64` e `linux/arm64` (via QEMU + Buildx). Cache de camadas via `gha`.

### Secrets necessários

| Secret | Descrição |
| --- | --- |
| `DOCKER_USERNAME` | Usuário Docker Hub |
| `DOCKER_PASSWORD` | Senha ou token Docker Hub |

## Workflow 3 — `k8s-trigger-cd.yml`

**Trigger:** `workflow_run` do workflow 2.

| Branch da imagem | Ambiente disparado | `image_tag` enviada |
| --- | --- | --- |
| `main` | `hml` | `main` |
| `v*.*.*` (tag) | `prd` | versão sem o prefixo `v` (ex.: `1.2.3`) |

Faz `createWorkflowDispatch` em `fiap-challenge-13soat/mecanica-hermes-k8s/api-deploy.yml`.

### Secrets necessários

| Secret | Descrição |
| --- | --- |
| `K8S_REPO_PAT` | PAT com `actions:write` no repositório `mecanica-hermes-k8s`. |

## Dockerfile

[`Dockerfile`](../Dockerfile) é multi-stage:

- **`build`** (`mcr.microsoft.com/dotnet/sdk:10.0`) — copia primeiro só os `.csproj` para
  cachear o `restore`, depois publica em `Release` para `/app/publish`.
- **`runtime`** (`mcr.microsoft.com/dotnet/aspnet:10.0`) — cria usuário não-root `appuser`,
  copia o publish, define `ASPNETCORE_HTTP_PORTS=8080`, expõe `8080`, entrypoint
  `dotnet Mecanica.Hermes.Api.dll`.

Práticas aplicadas:

- Usuário não-root (`appuser`, group `appgroup`).
- `chown -R appuser:appgroup /app`.
- Layer cache amigável (separa `restore` de `publish`).
- Sem ferramentas adicionais no estágio `runtime` (sem `curl`, sem `bash`).

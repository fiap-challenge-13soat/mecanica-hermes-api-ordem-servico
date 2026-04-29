# Receitas de desenvolvimento local. Requer 'just' (https://github.com/casey/just).
# Roda em PowerShell ou bash. Comando default: lista as receitas.
set windows-shell := ["pwsh.exe", "-NoLogo", "-NoProfile", "-Command"]

default:
    @just --list

# --- Build & test --------------------------------------------------------

# Build da solução em Debug.
build:
    dotnet build Mecanica.Hermes.Ordem.Servico.sln --nologo

# Roda todos os testes (unit + integration) com cobertura.
test:
    dotnet test Mecanica.Hermes.Ordem.Servico.sln --nologo --settings coverlet.runsettings --results-directory .temp/coverage

# Roda apenas testes de unidade (sem Testcontainers).
test-unit:
    dotnet test Mecanica.Hermes.Ordem.Servico.sln --nologo --filter "FullyQualifiedName!~IntegrationTests"

# Gera relatório HTML de cobertura em .temp/coverage/report (requer dotnet-reportgenerator-globaltool).
coverage-report: test
    reportgenerator -reports:".temp/coverage/**/coverage.cobertura.xml" -targetdir:".temp/coverage/report" -reporttypes:"Html;TextSummary"
    @echo "Relatório: .temp/coverage/report/index.html"

# --- Migrations ----------------------------------------------------------

# Cria nova migration (uso: just migration-add NomeDaMigracao).
migration-add name:
    dotnet ef migrations add {{name}} --project src/Mecanica.Hermes.Infrastructure --startup-project src/Mecanica.Hermes.Api

# Aplica migrations no banco apontado pela env var ConnectionStrings__DefaultConnection.
migration-apply:
    dotnet ef database update --project src/Mecanica.Hermes.Infrastructure --startup-project src/Mecanica.Hermes.Api

# Lista migrations conhecidas.
migration-list:
    dotnet ef migrations list --project src/Mecanica.Hermes.Infrastructure --startup-project src/Mecanica.Hermes.Api

# --- Run -----------------------------------------------------------------

# Sobe a API localmente (Development). Requer Postgres + RabbitMq + MongoDB rodando.
run:
    dotnet run --project src/Mecanica.Hermes.Api --launch-profile https

# --- Limpeza -------------------------------------------------------------

# Remove artefatos de build (bin, obj, TestResults, .temp/coverage).
clean:
    Get-ChildItem -Path src,tests -Recurse -Directory -Include bin,obj,TestResults | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force .temp/coverage -ErrorAction SilentlyContinue

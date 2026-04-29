# syntax=docker/dockerfile:1

# ─── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-level config first — these change rarely so the restore layer
# stays cached across code-only changes.
COPY Mecanica.Hermes.Ordem.Servico.sln       .
COPY Directory.Build.props                   .
COPY Directory.Packages.props                .

# Copy only the *.csproj files needed for the runtime image (no test projects).
# Docker caches the restore layer as long as these files don't change.
COPY src/Mecanica.Hermes.Domain/Mecanica.Hermes.Domain.csproj                   src/Mecanica.Hermes.Domain/
COPY src/Mecanica.Hermes.Application/Mecanica.Hermes.Application.csproj         src/Mecanica.Hermes.Application/
COPY src/Mecanica.Hermes.Infrastructure/Mecanica.Hermes.Infrastructure.csproj   src/Mecanica.Hermes.Infrastructure/
COPY src/Mecanica.Hermes.Api/Mecanica.Hermes.Api.csproj                         src/Mecanica.Hermes.Api/

RUN dotnet restore src/Mecanica.Hermes.Api/Mecanica.Hermes.Api.csproj

# Copy the full source and publish a self-contained release output.
COPY src/ src/
RUN dotnet publish src/Mecanica.Hermes.Api/Mecanica.Hermes.Api.csproj \
      --configuration Release \
      --no-restore \
      --output /app/publish

# ─── Runtime stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Non-root user — required for least-privilege execution and many Kubernetes
# security policies (runAsNonRoot: true).
RUN addgroup --system appgroup \
 && adduser  --system --ingroup appgroup --no-create-home appuser

COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app

USER appuser

# The aspnet:10.0 base image already sets ASPNETCORE_HTTP_PORTS=8080, but
# we make it explicit here so the intent is visible in the image config.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Mecanica.Hermes.Api.dll"]

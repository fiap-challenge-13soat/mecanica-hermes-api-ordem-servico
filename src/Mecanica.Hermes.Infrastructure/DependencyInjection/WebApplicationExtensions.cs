using Mecanica.Hermes.Infrastructure.Persistence;
using Mecanica.Hermes.Infrastructure.Settings;

#pragma warning disable CA1873 // Log argument is a const string reference — not expensive
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mecanica.Hermes.Infrastructure.DependencyInjection;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Aplica migrations pendentes na inicialização da aplicação, se a env var
    /// <c>RUN_MIGRATIONS_ON_STARTUP</c> for <c>true</c> (default) ou não estiver definida.
    ///
    /// <para>Para cenários de CD onde migrations são aplicadas por um init-container externo
    /// (ex: Kubernetes Job), defina <c>RUN_MIGRATIONS_ON_STARTUP=false</c> para que o serviço
    /// suba sem aguardar o EF Core.</para>
    /// </summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        var rawFlag = Environment.GetEnvironmentVariable(VariableNames.RunMigrationsOnStartup);

        // Comportamento padrão: migrar (true quando var ausente ou "true").
        if (rawFlag is not null
            && bool.TryParse(rawFlag, out var enabled)
            && !enabled)
        {
            var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
            logger.LogInformation(
                "Migrations na inicialização desabilitadas ({Var}=false). " +
                "Certifique-se de que as migrations foram aplicadas externamente.",
                VariableNames.RunMigrationsOnStartup);
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mecanica.Hermes.Infrastructure.Persistence;

// Usado exclusivamente pelas ferramentas de design-time do EF Core (dotnet ef migrations add/update).
// Não é registrado no container de DI em runtime.
[ExcludeFromCodeCoverage(Justification = "Design-time only — usado por dotnet ef, não roda em produção.")]
internal sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=mecanica_hermes_design_time;Username=postgres;Password=teste123A!",
                b => b.MigrationsAssembly(typeof(IInfrastructureAssemblyMarker).Assembly.GetName().Name)
                    .MigrationsHistoryTable("__EFMigrationsHistory", "public"))
            .Options;

        return new AppDbContext(options);
    }
}

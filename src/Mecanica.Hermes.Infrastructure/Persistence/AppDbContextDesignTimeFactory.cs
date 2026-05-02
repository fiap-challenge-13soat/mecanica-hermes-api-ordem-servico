using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mecanica.Hermes.Infrastructure.Persistence;

// Used exclusively by EF Core design-time tools (dotnet ef migrations add/update).
// Not registered in the DI container at runtime.
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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Mecanica.Hermes.Infrastructure.Observability;

[ExcludeFromCodeCoverage(Justification = "Constantes estáticas para tracing — valor testado em integração.")]
internal static class ActivitySources
{
    public const string ApplicationName = "MecanicaHermes";

    public static readonly ActivitySource Api =
        new(ApplicationName, typeof(ActivitySources).Assembly.GetName().Version?.ToString() ?? "1.0");
}

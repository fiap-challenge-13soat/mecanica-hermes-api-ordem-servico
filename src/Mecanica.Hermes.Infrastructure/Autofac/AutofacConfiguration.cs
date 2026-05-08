using System.Reflection;
using Autofac;
using Mecanica.Hermes.Application.Common.Messaging;
using Mecanica.Hermes.Shared.Application.Messaging;
using Mecanica.Hermes.Shared.Core.Results;
using MediatR;
using Module = Autofac.Module;

namespace Mecanica.Hermes.Infrastructure.Autofac;

internal class AutofacConfiguration : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(c => c.FullName!.Contains("Mecanica.Hermes"))
            .ToArray();

        builder.RegisterAssemblyTypes(assemblies)
            .Where(t => t.Name.EndsWith("Repository", StringComparison.Ordinal))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        RegisterAsyncCommandHandlers(builder, assemblies);
    }

    /// <summary>
    ///     Registra um <see cref="AsyncCommandHandler{TRequest}" /> fechado para cada
    ///     <see cref="IAsyncCommand" /> encontrado nos assemblies da aplicação.
    ///     <para>
    ///         Necessário porque Autofac não suporta o registro de open generics quando o número
    ///         de parâmetros do tipo difere do contrato (<c>IRequestHandler&lt;,&gt;</c> tem 2,
    ///         nosso handler tem 1). A construção é feita via reflection no startup.
    ///     </para>
    /// </summary>
    private static void RegisterAsyncCommandHandlers(ContainerBuilder builder, IEnumerable<Assembly> assemblies)
    {
        var asyncCommandTypes = assemblies
            .SelectMany(a => SafeGetTypes(a))
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IAsyncCommand).IsAssignableFrom(t));

        foreach (var commandType in asyncCommandTypes)
        {
            var handlerType = typeof(AsyncCommandHandler<>).MakeGenericType(commandType);
            var serviceType = typeof(IRequestHandler<,>).MakeGenericType(commandType, typeof(Result));

            builder.RegisterType(handlerType)
                .As(serviceType)
                .InstancePerDependency();
        }
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}

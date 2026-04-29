using Autofac;
using Autofac.Extensions.DependencyInjection;
using Mecanica.Hermes.Infrastructure.Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Mecanica.Hermes.Infrastructure.DependencyInjection;

public static class HostBuilderExtensions
{
    public static void ConfigureHostBuilder(this ConfigureHostBuilder hostBuilder)
    {
        hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        hostBuilder.ConfigureContainer<ContainerBuilder>(b => { b.RegisterModule<AutofacConfiguration>(); });
    }
}
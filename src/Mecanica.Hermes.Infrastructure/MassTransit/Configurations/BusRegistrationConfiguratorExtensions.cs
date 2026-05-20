using System.Diagnostics.CodeAnalysis;
using MassTransit;
using Mecanica.Hermes.Application;
using Mecanica.Hermes.Infrastructure.MassTransit.Saga;

namespace Mecanica.Hermes.Infrastructure.MassTransit.Configurations;

[ExcludeFromCodeCoverage(Justification = "Composition root de SAGA/MongoDB — exercitado em integração.")]
internal static class BusRegistrationConfiguratorExtensions
{
    internal static IBusRegistrationConfigurator ConfigureSaga(
        this IBusRegistrationConfigurator configurator,
        string? mongoConnectionString,
        string mongoDatabaseName)
    {
        var saga = configurator.AddSagaStateMachine<OrdemDeServicoStateMachine, OrdemDeServicoState>();

        if (!string.IsNullOrWhiteSpace(mongoConnectionString))
            saga.MongoDbRepository(r =>
            {
                r.Connection = mongoConnectionString;
                r.DatabaseName = mongoDatabaseName;
                r.CollectionName = "ordem-de-servico-saga-state";
            });
        else
            saga.InMemoryRepository();

        // Registra todos os consumers definidos na camada de Application (um por operação Execute*).
        configurator.AddConsumers(typeof(IApplicationAssemblyMarker).Assembly);

        // IEndpointNameFormatter é registrado como singleton em ServiceCollectionExtensions.AddMassTransitOptions,
        // lendo QueueNamePrefix da env var MASSTRANSIT__QUEUE_NAME_PREFIX (default: "mecanica-hermes").
        // O MassTransit resolve-o automaticamente do container — não é necessário chamar
        // SetEndpointNameFormatter aqui.

        configurator.AddQuartzConsumers();
        return configurator;
    }
}

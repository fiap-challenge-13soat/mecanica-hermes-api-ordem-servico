using Mecanica.Hermes.IntegrationTests.Infrastructure;

namespace Mecanica.Hermes.IntegrationTests;

[CollectionDefinition(IntegrationTestCollection.Name)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebApplicationFactory>
{
    public const string Name = "Integration Tests";
}

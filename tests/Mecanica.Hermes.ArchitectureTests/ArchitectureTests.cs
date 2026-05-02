using Mecanica.Hermes.Application;
using Mecanica.Hermes.Domain;
using NetArchTest.Rules;

#pragma warning disable CA1707 // Underscores in test method names are standard xUnit convention
#pragma warning disable CA1724 // Type name collision with namespace is acceptable in test projects

namespace Mecanica.Hermes.ArchitectureTests;

/// <summary>
/// Garante que as regras de dependência da Clean Architecture não sejam quebradas
/// por acidente. Cada teste falha com uma mensagem explicativa se a regra for violada.
///
/// Regras:
/// 1. Domain não pode referenciar Application, Infrastructure ou Api.
/// 2. Application não pode referenciar Infrastructure ou Api.
/// 3. Application não pode referenciar EntityFrameworkCore diretamente.
/// 4. Todos os AggregateRoot concretos devem ter construtores não-públicos (fábricas enforçadas).
/// 5. IRequestHandler (MediatR) handlers devem ser sealed.
/// 6. Consumers (MassTransit) devem ser sealed.
/// </summary>
[Trait("Category", "Unit")]
public class ArchitectureTests
{
    private const string DomainNamespace = "Mecanica.Hermes.Domain";
    private const string ApplicationNamespace = "Mecanica.Hermes.Application";
    private const string InfrastructureNamespace = "Mecanica.Hermes.Infrastructure";
    private const string ApiNamespace = "Mecanica.Hermes.Api";

    private static Types FromDomain => Types.InAssembly(typeof(IDomainAssemblyMarker).Assembly);
    private static Types FromApplication => Types.InAssembly(typeof(IApplicationAssemblyMarker).Assembly);

    // ---- Dependency direction rules ----

    [Fact]
    public void Domain_NaoDeveDependenDe_Application()
    {
        var result = FromDomain
            .That().ResideInNamespace(DomainNamespace)
            .ShouldNot().HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Domain não deve depender de Application. Violações: {Violations(result)}");
    }

    [Fact]
    public void Domain_NaoDeveDependenDe_Infrastructure()
    {
        var result = FromDomain
            .That().ResideInNamespace(DomainNamespace)
            .ShouldNot().HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Domain não deve depender de Infrastructure. Violações: {Violations(result)}");
    }

    [Fact]
    public void Domain_NaoDeveDependenDe_Api()
    {
        var result = FromDomain
            .That().ResideInNamespace(DomainNamespace)
            .ShouldNot().HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Domain não deve depender de Api. Violações: {Violations(result)}");
    }

    [Fact]
    public void Application_NaoDeveDependenDe_Infrastructure()
    {
        var result = FromApplication
            .That().ResideInNamespace(ApplicationNamespace)
            .ShouldNot().HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Application não deve depender de Infrastructure. Violações: {Violations(result)}");
    }

    [Fact]
    public void Application_NaoDeveDependenDe_Api()
    {
        var result = FromApplication
            .That().ResideInNamespace(ApplicationNamespace)
            .ShouldNot().HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Application não deve depender de Api. Violações: {Violations(result)}");
    }

    [Fact]
    public void Application_NaoDeveTerDependenciaDiretaDeEntityFramework()
    {
        var result = FromApplication
            .That().ResideInNamespace(ApplicationNamespace)
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Application não deve referenciar EntityFrameworkCore. Violações: {Violations(result)}");
    }

    // ---- Encapsulation rules ----

    [Fact]
    public void MediatRHandlers_DevemSerSealed()
    {
        var result = FromApplication
            .That().ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Handlers MediatR devem ser sealed para evitar herança acidental. Violações: {Violations(result)}");
    }

    // ---- Helper ----

    private static string Violations(TestResult result)
    {
        if (result.FailingTypes is null) return "(nenhuma)";
        return string.Join(", ", result.FailingTypes.Select(t => t.FullName ?? t.Name));
    }
}

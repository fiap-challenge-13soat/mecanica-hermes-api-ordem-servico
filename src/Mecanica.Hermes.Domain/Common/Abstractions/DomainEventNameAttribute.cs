namespace Mecanica.Hermes.Domain.Common.Abstractions;

/// <summary>
///     Associa um discriminador de nome estável a um tipo <see cref="IDomainEvent" />.
///     O <c>EfUnitOfWork</c> usa este atributo para gravar o campo <c>Type</c> na tabela
///     <c>OutboxMessages</c> em vez de <c>AssemblyQualifiedName</c>, tornando o discriminador
///     imune a refatorações de namespace ou renomeação de assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DomainEventNameAttribute(string name) : Attribute
{
    /// <summary>Nome lógico e imutável do evento — ex: <c>"ordem-de-servico.criado.v1"</c>.</summary>
    public string Name { get; } = name;
}

using System.Diagnostics.CodeAnalysis;

namespace Mecanica.Hermes.Application.Common.Exceptions;

/// <summary>
///     Lançada pela camada de persistência quando uma tentativa de atualização concorrente
///     falha (ex: <c>DbUpdateConcurrencyException</c> do EF Core / xmin do PostgreSQL).
///     Encapsula a exceção de infra para que Application não dependa de EF Core.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Exception sem lógica — apenas transporte de mensagem.")]
public sealed class ConcurrencyConflictException(string message, Exception? inner = null)
    : Exception(message, inner);

using Mecanica.Hermes.Domain.Common.Abstractions;
using MediatR;

namespace Mecanica.Hermes.Infrastructure.Persistence.Events;

internal sealed record DomainEventNotification<TEvent>(TEvent Event) : INotification
    where TEvent : IDomainEvent;

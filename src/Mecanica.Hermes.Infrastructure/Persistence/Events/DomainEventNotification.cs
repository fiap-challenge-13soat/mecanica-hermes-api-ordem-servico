using Mecanica.Hermes.Shared.Core.Domain;
using MediatR;

namespace Mecanica.Hermes.Infrastructure.Persistence.Events;

internal sealed record DomainEventNotification<TEvent>(TEvent Event) : INotification
    where TEvent : IDomainEvent;

using System.Linq.Expressions;
using BuildingBlocks.Abstractions.CQRS.Commands;
using BuildingBlocks.Abstractions.CQRS.Events.Internal;

namespace BuildingBlocks.Abstractions.Messaging.PersistMessage;

// Ref: http://www.kamilgrzybek.com/design/the-outbox-pattern/
// Ref: https://event-driven.io/en/outbox_inbox_patterns_and_delivery_guarantees_explained/
// Ref: https://debezium.io/blog/2019/02/19/reliable-microservices-data-exchange-with-the-outbox-pattern/
// Ref: https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/multi-container-microservice-net-applications/subscribe-events#designing-atomicity-and-resiliency-when-publishing-to-the-event-bus
// Ref: https://github.com/kgrzybek/modular-monolith-with-ddd#38-internal-processing
public interface IMessagePersistenceService
{
    Task<IReadOnlyList<StoreMessage>> GetByFilterAsync(
        Expression<Func<StoreMessage, bool>>? predicate = null,
        CancellationToken cancellationToken = default
    );

    Task AddPublishMessageAsync<TMessageEnvelope>(
        TMessageEnvelope messageEnvelope,
        CancellationToken cancellationToken = default
    )
    where TMessageEnvelope : MessageEnvelope;

    Task AddReceivedMessageAsync<TMessageEnvelope>(
        TMessageEnvelope messageEnvelope,
        CancellationToken cancellationToken = default
    )
    where TMessageEnvelope : MessageEnvelope;

    Task AddInternalMessageAsync<TCommand>(
        TCommand internalCommand,
        CancellationToken cancellationToken = default
    )
    where TCommand : class, IInternalCommand;

    Task AddNotificationAsync(
        IDomainNotificationEvent notification,
        CancellationToken cancellationToken = default
    );

    Task ProcessAsync(Guid messageId, CancellationToken cancellationToken = default);

    Task ProcessAllAsync(CancellationToken cancellationToken = default);
}

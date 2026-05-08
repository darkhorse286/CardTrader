using CardTrader.Domain.Common;

namespace CardTrader.Application.Abstractions;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default);
}

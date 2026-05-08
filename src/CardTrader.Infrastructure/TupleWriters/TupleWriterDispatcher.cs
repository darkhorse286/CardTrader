using CardTrader.Application.Abstractions;
using CardTrader.Domain.Common;

namespace CardTrader.Infrastructure.TupleWriters;

internal sealed class TupleWriterDispatcher(IEnumerable<ITupleWriterHandler> handlers)
    : IDomainEventDispatcher
{
    private readonly IReadOnlyList<ITupleWriterHandler> _handlers = handlers.ToList();

    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
        {
            var handler = _handlers.FirstOrDefault(h => h.CanHandle(@event));
            if (handler is not null)
                await handler.HandleAsync(@event, ct);
        }
    }
}

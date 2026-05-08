using CardTrader.Domain.Common;

namespace CardTrader.Infrastructure.TupleWriters;

internal interface ITupleWriterHandler
{
    bool CanHandle(IDomainEvent @event);
    Task HandleAsync(IDomainEvent @event, CancellationToken ct = default);
}

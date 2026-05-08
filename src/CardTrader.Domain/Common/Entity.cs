namespace CardTrader.Domain.Common;

public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected init; } = default!;

    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent @event) => _domainEvents.Add(@event);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

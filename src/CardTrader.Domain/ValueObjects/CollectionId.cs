namespace CardTrader.Domain.ValueObjects;

public readonly record struct CollectionId(Guid Value)
{
    public static CollectionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

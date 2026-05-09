namespace CardTrader.Domain.ValueObjects;

public readonly record struct RosterId(Guid Value)
{
    public static RosterId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

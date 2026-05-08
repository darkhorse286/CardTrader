namespace CardTrader.Domain.ValueObjects;

public readonly record struct CardInstanceId(Guid Value)
{
    public static CardInstanceId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

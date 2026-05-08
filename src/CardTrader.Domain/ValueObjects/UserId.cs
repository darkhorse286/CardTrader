namespace CardTrader.Domain.ValueObjects;

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public static UserId Parse(string s) => new(Guid.Parse(s));
    public override string ToString() => Value.ToString();
}

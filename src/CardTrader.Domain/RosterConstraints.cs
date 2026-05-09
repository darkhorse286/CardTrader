namespace CardTrader.Domain;

public static class RosterConstraints
{
    public const int MaxSize = 10;

    public static readonly IReadOnlyDictionary<string, int> PositionSlots =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Attack"]   = 3,
            ["Midfield"] = 3,
            ["Defense"]  = 3,
            ["Goalie"]   = 1,
        };
}

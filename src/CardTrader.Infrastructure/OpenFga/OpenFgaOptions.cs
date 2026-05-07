namespace CardTrader.Infrastructure.OpenFga;

public sealed class OpenFgaOptions
{
    public const string SectionName = "OpenFga";

    public string ApiUrl { get; init; } = string.Empty;
    public string StoreId { get; init; } = string.Empty;
    public string AuthorizationModelId { get; init; } = string.Empty;
}

namespace CardTrader.Integration.Tests.Infrastructure;

[CollectionDefinition(AdminIntegrationCollection.Name)]
public sealed class AdminIntegrationCollection : ICollectionFixture<AdminIntegrationFixture>
{
    public const string Name = "AdminIntegration";
}

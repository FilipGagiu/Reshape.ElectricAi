namespace Reshape.ElectricAi.LiveFeed.Tests.Integration.Fixtures;

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "LiveFeedPostgres";
}

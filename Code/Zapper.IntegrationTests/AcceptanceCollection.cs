using Xunit;

namespace Zapper.IntegrationTests;

[CollectionDefinition("Acceptance")]
public sealed class AcceptanceCollection : ICollectionFixture<AcceptanceFixture>;

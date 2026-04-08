using Xunit;

namespace RiftVeil.Api.Tests.Infrastructure;

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<TestWebApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

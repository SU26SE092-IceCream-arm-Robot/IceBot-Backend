using Application.Shared.Idempotency;

namespace IceBot.UnitTests.Orders;

public sealed class ScopedIdempotencyKeyTests
{
    [Fact]
    public void ScopeBuilders_IsolateTheSameClientKeyByOwner()
    {
        const string key = "retry-1";

        var first = ScopedIdempotencyKey.ForKiosk(Guid.NewGuid(), key);
        var second = ScopedIdempotencyKey.ForKiosk(Guid.NewGuid(), key);

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryNormalize_RejectsMissingKey(string? value)
    {
        var valid = ScopedIdempotencyKey.TryNormalize(value, out _);

        Assert.False(valid);
    }
}

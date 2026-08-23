using Application.Shared.Idempotency;

namespace IceBot.UnitTests.Orders;

public sealed class ScopedIdempotencyKeyTests
{
    [Fact]
    public void ClientDeviceScope_IsolatesTheSameClientKeyByDevice()
    {
        const string key = "retry-1";

        var first = ScopedIdempotencyKey.ForClientDevice(Guid.NewGuid(), key);
        var second = ScopedIdempotencyKey.ForClientDevice(Guid.NewGuid(), key);

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

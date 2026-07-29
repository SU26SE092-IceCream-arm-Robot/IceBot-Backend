using Infrastructure.Firebase;
using Microsoft.Extensions.Options;
using Polly.Timeout;

namespace IceBot.IntegrationTests.Identity;

public sealed class FirebasePushDeliveryTimeoutPolicyTests
{
    [Fact]
    public async Task TimeoutStopsSinglePushAttemptWithoutRetry()
    {
        var policy = new FirebasePushDeliveryTimeoutPolicy(Options.Create(new FirebasePushDeliveryOptions
        {
            OperationTimeoutSeconds = 1
        }));
        var attempts = 0;

        await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
            await policy.ExecuteAsync<string>(async cancellationToken =>
            {
                attempts++;
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return "unreachable";
            }));

        Assert.Equal(1, attempts);
    }
}

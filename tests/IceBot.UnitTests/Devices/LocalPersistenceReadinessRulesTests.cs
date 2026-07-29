using Application.Devices.Connectivity.Contracts;
using Application.Devices.Connectivity.Rules;
using Domain.Devices.ExecutionEndpoints;

namespace IceBot.UnitTests.Devices;

public sealed class LocalPersistenceReadinessRulesTests
{
    [Fact]
    public void Apply_HealthyPersistence_PreservesRequestedReadiness()
    {
        var decision = LocalPersistenceReadinessRules.Apply(
            Healthy(),
            ExecutionReadinessState.Ready,
            null);

        Assert.Equal(ExecutionReadinessState.Ready, decision.Readiness);
        Assert.Null(decision.FaultCode);
    }

    [Theory]
    [MemberData(nameof(UnhealthyStates))]
    public void Apply_UnhealthyPersistence_ForcesNotReady(
        LocalPersistenceHealthInput health,
        string expectedFaultCode)
    {
        var decision = LocalPersistenceReadinessRules.Apply(
            health,
            ExecutionReadinessState.Ready,
            null);

        Assert.Equal(ExecutionReadinessState.NotReady, decision.Readiness);
        Assert.Equal(expectedFaultCode, decision.FaultCode);
    }

    public static TheoryData<LocalPersistenceHealthInput, string> UnhealthyStates => new()
    {
        { Healthy() with { StorageWritable = false }, LocalPersistenceReadinessRules.StorageNotWritable },
        { Healthy() with { FreeSpaceBytes = 99 }, LocalPersistenceReadinessRules.InsufficientStorage },
        { Healthy() with { LocalDatabaseHealth = LocalDatabaseHealth.Corrupt }, LocalPersistenceReadinessRules.DatabaseUnhealthy },
        { Healthy() with { PendingEventCount = 101 }, LocalPersistenceReadinessRules.EventBacklogLimitExceeded }
    };

    private static LocalPersistenceHealthInput Healthy() =>
        new(true, 1_000, 100, LocalDatabaseHealth.Healthy, 0, 100);
}


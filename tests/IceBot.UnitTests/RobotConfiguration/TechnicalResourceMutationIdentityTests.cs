using Application.Shared.Concurrency;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class TechnicalResourceMutationIdentityTests
{
    [Fact]
    public void OrderForLocking_PlacesNaturalIdentitiesBeforeResourceIds()
    {
        var programId = TechnicalResourceMutationIdentity.Program(Guid.NewGuid());
        var contractId = TechnicalResourceMutationIdentity.Contract(Guid.NewGuid());
        var programDefinition = TechnicalResourceMutationIdentity.ProgramDefinition(
            Guid.NewGuid(), null, null, null, "MAKE_ICE_CREAM");
        var artifactDefinition = TechnicalResourceMutationIdentity.ArtifactDefinition(
            Guid.NewGuid(), "DISPENSE");

        var ordered = TechnicalResourceMutationIdentity.OrderForLocking(
            [programId, contractId, programDefinition, artifactDefinition]);

        Assert.All(ordered.Take(2), identity => Assert.Equal(0, identity.LockTier));
        Assert.All(ordered.Skip(2), identity => Assert.Equal(1, identity.LockTier));
    }
}

using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.CommandDelivery.Rules;

namespace IceBot.UnitTests.EdgeIntegration;

public sealed class EdgeCommandAcknowledgementRulesTests
{
    [Fact]
    public void Validate_AcceptedWithoutDurableLocalState_IsRejected()
    {
        var error = EdgeCommandAcknowledgementRules.Validate(Command("Accepted", null));

        Assert.Equal("Accepted acknowledgement requires durable local command state.", error);
    }

    [Fact]
    public void Validate_AcceptedAfterDurableLocalState_IsAllowed()
    {
        Assert.Null(EdgeCommandAcknowledgementRules.Validate(Command("Accepted", true)));
    }

    [Fact]
    public void Validate_RejectedDoesNotRequireDurableLocalState()
    {
        Assert.Null(EdgeCommandAcknowledgementRules.Validate(Command("Rejected", null)));
    }

    private static AcknowledgeEdgeCommandCommand Command(string status, bool? localStatePersisted) => new()
    {
        KioskId = Guid.NewGuid(),
        EndpointId = Guid.NewGuid(),
        CommandId = Guid.NewGuid(),
        AckStatus = status,
        LocalStatePersisted = localStatePersisted
    };
}


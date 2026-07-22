using Application.Operations.Alerts.Automation;

namespace IceBot.UnitTests.Operations;

public sealed class InventoryAlertScanWindowTests
{
    [Fact]
    public void RotatesAcrossAllCandidatesWhenCapacityIsSmallerThanPopulation()
    {
        var offsets = Enumerable.Range(0, 5)
            .Select(slot => InventoryAlertScanWindow.CalculateOffset(10, 2, slot))
            .ToArray();

        Assert.Equal([0, 2, 4, 6, 8], offsets);
    }

    [Fact]
    public void ReturnsZeroForEmptyOrInvalidWindow()
    {
        Assert.Equal(0, InventoryAlertScanWindow.CalculateOffset(0, 10, 1));
        Assert.Equal(0, InventoryAlertScanWindow.CalculateOffset(10, 0, 1));
    }
}

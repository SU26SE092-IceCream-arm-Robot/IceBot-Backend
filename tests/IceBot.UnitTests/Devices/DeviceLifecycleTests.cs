using Domain.Common;
using Domain.Devices.Catalog;

namespace IceBot.UnitTests.Devices;

public sealed class DeviceLifecycleTests
{
    [Fact]
    public void SetStatus_RejectsReturnToProvisioningUnlessDeviceWasDisabled()
    {
        var device = CreateDevice();
        device.SetStatus(DeviceStatus.Online);

        var exception = Assert.Throws<DomainRuleException>(() => device.SetStatus(DeviceStatus.Provisioning));

        Assert.Equal("Cannot transition a device from Online to Provisioning.", exception.Message);
    }

    [Fact]
    public void SetStatus_AllowsDisabledDeviceToReturnToProvisioning()
    {
        var device = CreateDevice();
        device.SetStatus(DeviceStatus.Disabled);

        device.SetStatus(DeviceStatus.Provisioning);

        Assert.Equal(DeviceStatus.Provisioning, device.Status);
    }

    private static Device CreateDevice() => Device.CreateProvisioning(
        1,
        null,
        Guid.NewGuid(),
        "DEVICE-1",
        "Test device",
        null,
        null,
        null,
        null);
}

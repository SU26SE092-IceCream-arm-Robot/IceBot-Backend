using Application.Devices.Support;
using Domain.Devices.Entities;

namespace Application.Inventory.Support;

public static class DispenserDeviceCapabilityRules
{
    public static string? Validate(DeviceModel? model, bool requiresLevelSensor)
    {
        if (model is null)
        {
            return "A dispenser state requires a DeviceModel with declared capabilities.";
        }

        if (!DeviceCapabilityContract.Supports(model.CapabilitiesJson, DeviceCapabilityContract.IngredientDispenser))
        {
            return $"Device model must support '{DeviceCapabilityContract.IngredientDispenser}'.";
        }

        if (requiresLevelSensor &&
            !DeviceCapabilityContract.Supports(model.CapabilitiesJson, DeviceCapabilityContract.LevelSensor))
        {
            return $"A level-to-quantity profile requires device capability '{DeviceCapabilityContract.LevelSensor}'.";
        }

        return null;
    }
}

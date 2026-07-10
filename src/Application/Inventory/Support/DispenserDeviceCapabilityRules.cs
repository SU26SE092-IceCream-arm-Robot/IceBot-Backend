using Application.Devices.Catalog.Support;
using Domain.Devices.Catalog;

namespace Application.Inventory.Support;

public static class DispenserDeviceCapabilityRules
{
    public static string? Validate(DeviceModel? model)
    {
        if (model is null)
        {
            return "A dispenser state requires a DeviceModel with declared capabilities.";
        }

        if (!DeviceCapabilityContract.Supports(model.CapabilitiesJson, DeviceCapabilityContract.IngredientDispenser))
        {
            return $"Device model must support '{DeviceCapabilityContract.IngredientDispenser}'.";
        }

        return null;
    }
}

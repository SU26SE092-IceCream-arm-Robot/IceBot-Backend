using Application.Devices.Support;
using Application.Inventory.Abstractions;
using Application.Inventory.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Inventory.Queries;

public sealed class GetKioskInventoryTopologyQueryHandler(IInventoryStore inventory)
{
    public async Task<ApiResult<KioskInventoryTopologyResult>> HandleAsync(
        GetKioskInventoryTopologyQuery query,
        CancellationToken cancellationToken = default)
    {
        var kiosk = await inventory.GetKioskForInventoryTopologyAsync(query.KioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<KioskInventoryTopologyResult>.Fail("Kiosk not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.InventoryView,
                query.UserContext,
                kiosk.OrganizationId,
                kiosk.StoreId,
                kiosk.Id))
        {
            return ApiResult<KioskInventoryTopologyResult>.Fail("Access denied.", 403);
        }

        var devices = await inventory.ListDevicesForInventoryTopologyAsync(kiosk.Id, cancellationToken);
        var states = await inventory.ListStatesForInventoryTopologyAsync(kiosk.Id, cancellationToken);
        var statesByDevice = states.ToLookup(state => state.DeviceId);

        var result = new KioskInventoryTopologyResult
        {
            KioskId = kiosk.Id,
            KioskCode = kiosk.Code,
            KioskName = kiosk.Name,
            Devices = devices.Select(device =>
            {
                var capabilities = DeviceCapabilityContract.Deserialize(device.DeviceModel?.CapabilitiesJson);
                var containers = statesByDevice[device.Id]
                    .Select(state => new InventoryTopologyContainerResult
                    {
                        DispenserStateId = state.Id,
                        ContainerCode = state.ContainerCode,
                        IngredientId = state.IngredientId,
                        IngredientCode = state.Ingredient.Code,
                        IngredientName = state.Ingredient.Name,
                        CurrentLevelStatus = state.CurrentLevelStatus,
                        EstimatedQuantity = state.EstimatedQuantity,
                        CapacityQuantity = state.CapacityQuantity,
                        Unit = state.Unit,
                        IsActive = state.IsActive,
                        IngredientIsActive = state.Ingredient.IsActive,
                        Warnings = BuildContainerWarnings(device, state)
                    })
                    .ToList();

                return new InventoryTopologyDeviceResult
                {
                    DeviceId = device.Id,
                    Code = device.Code,
                    Name = device.Name,
                    Status = device.Status,
                    DeviceTypeId = device.DeviceTypeId,
                    DeviceTypeCode = device.DeviceType.Code,
                    DeviceModelId = device.DeviceModelId,
                    DeviceModelCode = device.DeviceModel?.Code,
                    Capabilities = capabilities,
                    CanHostDispenser = capabilities.Contains(
                        DeviceCapabilityContract.IngredientDispenser,
                        StringComparer.OrdinalIgnoreCase),
                    HasConfiguredContainers = containers.Count > 0,
                    Warnings = BuildDeviceWarnings(device),
                    Containers = containers
                };
            }).ToList()
        };

        return ApiResult<KioskInventoryTopologyResult>.Success(result);
    }

    private static IReadOnlyList<string> BuildDeviceWarnings(Domain.Devices.Entities.Device device)
    {
        var warnings = new List<string>();
        if (device.DeletedAt.HasValue || device.Status == Domain.Devices.Enums.DeviceStatus.Retired)
            warnings.Add("DeviceInactive");
        else if (device.Status != Domain.Devices.Enums.DeviceStatus.Online)
            warnings.Add("DeviceUnavailable");
        return warnings;
    }

    private static IReadOnlyList<string> BuildContainerWarnings(
        Domain.Devices.Entities.Device device,
        Domain.Inventory.Entities.IngredientDispenserState state)
    {
        var warnings = new List<string>(BuildDeviceWarnings(device));
        if (!state.IsActive) warnings.Add("ContainerInactive");
        if (!state.Ingredient.IsActive) warnings.Add("IngredientInactive");
        return warnings;
    }
}

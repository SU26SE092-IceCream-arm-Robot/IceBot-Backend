using Application.Devices.Catalog.Abstractions;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Connectivity.Abstractions;
using Application.Devices.Credentials.Abstractions;
using Application.Devices.Catalog.Mapping;
using Application.Devices.ExecutionEndpoints.Mapping;
using Application.Devices.Telemetry.Mapping;
using Application.Devices.Catalog.Results;
using Application.Devices.ExecutionEndpoints.Results;
using Application.Devices.Telemetry.Results;
using Application.Devices.Connectivity.Results;
using Application.Devices.Credentials.Results;
using Application.Shared.Wrappers;
using Domain.Devices.Catalog;

namespace Application.Devices.Catalog.Commands;

public sealed class CreateDeviceTypeCommandHandler(IDeviceManagementStore store)
{
    public async Task<ApiResult<DeviceTypeResult>> HandleAsync(
        CreateDeviceTypeCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;
        var code = NormalizeCode(request.Code);
        if (await store.DeviceTypeCodeExistsAsync(code, cancellationToken: cancellationToken))
        {
            return ApiResult<DeviceTypeResult>.Fail("Device type code already exists.", 409);
        }

        var entity = new DeviceType
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = TrimToNull(request.Description),
            Category = request.Category.Trim(),
            RequiresKioskAssignment = request.RequiresKioskAssignment,
            DisplayOrder = request.DisplayOrder,
            IsActive = true,
            CreatedByAccountId = command.ActorId
        };
        await store.AddDeviceTypeAsync(entity, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        return ApiResult<DeviceTypeResult>.Success(DeviceCatalogResultMapper.ToResult(entity), "Device type created.", 201);
    }

    internal static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
    internal static string? TrimToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

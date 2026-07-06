using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Domain.Devices.Entities;

namespace Application.Devices.Commands;

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

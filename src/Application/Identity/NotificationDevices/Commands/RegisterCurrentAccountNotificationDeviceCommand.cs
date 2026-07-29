using Application.Identity.NotificationDevices.Requests;

namespace Application.Identity.NotificationDevices.Commands;

public sealed class RegisterCurrentAccountNotificationDeviceCommand
{
    public Guid AccountId { get; init; }

    public Guid InstallationId { get; init; }

    public required RegisterCurrentAccountNotificationDeviceRequest Request { get; init; }
}

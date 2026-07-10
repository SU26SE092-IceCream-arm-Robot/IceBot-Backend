namespace Application.Identity.NotificationDevices.Commands;

public sealed record UnregisterCurrentAccountNotificationDeviceCommand(Guid AccountId, Guid InstallationId);

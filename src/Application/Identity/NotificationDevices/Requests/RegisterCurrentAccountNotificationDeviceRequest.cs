namespace Application.Identity.NotificationDevices.Requests;

public sealed class RegisterCurrentAccountNotificationDeviceRequest
{
    public string Platform { get; set; } = string.Empty;

    public string PushToken { get; set; } = string.Empty;

    public string? DeviceName { get; set; }

    public string? AppVersion { get; set; }
}

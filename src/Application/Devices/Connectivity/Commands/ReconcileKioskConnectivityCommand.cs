namespace Application.Devices.Connectivity.Commands;

public sealed record ReconcileKioskConnectivityCommand
{
    public required Guid KioskId { get; init; }
    public required DateTimeOffset ObservedAt { get; init; }
}

namespace Domain.Sync.Enums;

public enum EdgeCommandDeliveryOutcome
{
    Sent = 1,
    ExecutorBusy = 2,
    TransportFailure = 3,
    DeliveryFailed = 4
}

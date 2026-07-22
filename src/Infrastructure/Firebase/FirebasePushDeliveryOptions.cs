namespace Infrastructure.Firebase;

public sealed class FirebasePushDeliveryOptions
{
    public const string SectionName = "Firebase:PushDelivery";

    public int OperationTimeoutSeconds { get; set; } = 30;
}

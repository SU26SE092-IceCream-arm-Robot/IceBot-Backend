namespace Domain.ServiceRegistration.Enums;

public enum ServiceRegistrationStatus
{
    Submitted = 1,
    UnderReview = 2,
    Rejected = 3,
    Provisioning = 4,
    ProvisioningFailed = 5,
    Provisioned = 6,
    Cancelled = 7
}

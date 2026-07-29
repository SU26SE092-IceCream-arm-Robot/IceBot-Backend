namespace Application.Devices.Credentials.Abstractions;

public interface IMqttEndpointCredentialProvisioner
{
    string ProviderName { get; }
    string GetSubscribeTopic(Guid endpointId);
    string GetUplinkPublishTopicPattern(Guid endpointId);
    string GetUplinkResultTopic(Guid endpointId);
    Task ProvisionOrReplaceAsync(Guid endpointId, string username, string password, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid endpointId, string username, CancellationToken cancellationToken = default);
}

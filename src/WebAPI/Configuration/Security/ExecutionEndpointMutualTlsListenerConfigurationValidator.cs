namespace WebAPI.Configuration.Security;

public static class ExecutionEndpointMutualTlsListenerConfigurationValidator
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var listener = configuration
            .GetSection(ExecutionEndpointMutualTlsListenerOptions.SectionName)
            .Get<ExecutionEndpointMutualTlsListenerOptions>() ?? new ExecutionEndpointMutualTlsListenerOptions();
        if (!listener.Required)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(listener.EndpointName))
        {
            throw new InvalidOperationException(
                "ExecutionEndpointTransport:MutualTlsListener:EndpointName is required when the dedicated Edge mTLS listener is enabled.");
        }

        var endpointSection = configuration.GetSection($"Kestrel:Endpoints:{listener.EndpointName}");
        var url = endpointSection["Url"];
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Kestrel:Endpoints:{listener.EndpointName}:Url must be an HTTPS URL when the dedicated Edge mTLS listener is enabled.");
        }

        var endpointCertificate = endpointSection.GetSection("Certificate");
        var defaultCertificate = configuration.GetSection("Kestrel:Certificates:Default");
        if (!HasCertificateSource(endpointCertificate) && !HasCertificateSource(defaultCertificate))
        {
            throw new InvalidOperationException(
                "A Kestrel certificate must be configured for the dedicated Edge mTLS listener or as Kestrel:Certificates:Default.");
        }
    }

    private static bool HasCertificateSource(IConfigurationSection certificate)
    {
        return !string.IsNullOrWhiteSpace(certificate["Path"]) ||
               !string.IsNullOrWhiteSpace(certificate["Subject"]) ||
               !string.IsNullOrWhiteSpace(certificate["Thumbprint"]);
    }
}

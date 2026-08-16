using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using WebAPI.Configuration.Security;

namespace IceBot.IntegrationTests.WebApi;

public sealed class ExecutionEndpointMutualTlsListenerConfigurationTests
{
    [Fact]
    public void Production_DedicatedMtlsListenerWithoutHttpsEndpoint_FailsStartupValidation()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ExecutionEndpointTransport:MutualTlsListener:Required"] = "true",
            ["ExecutionEndpointTransport:MutualTlsListener:EndpointName"] = "EdgeMtls",
            ["Kestrel:Endpoints:EdgeMtls:Url"] = "http://+:8443",
            ["Kestrel:Endpoints:EdgeMtls:Certificate:Path"] = "/https/edge-api.pfx"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ExecutionEndpointMutualTlsListenerConfigurationValidator.Validate(
                configuration,
                new ProductionHostEnvironment()));

        Assert.Contains("must be an HTTPS URL", exception.Message);
    }

    [Fact]
    public void Production_DedicatedMtlsListenerWithHttpsAndCertificate_PassesStartupValidation()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ExecutionEndpointTransport:MutualTlsListener:Required"] = "true",
            ["ExecutionEndpointTransport:MutualTlsListener:EndpointName"] = "EdgeMtls",
            ["Kestrel:Endpoints:EdgeMtls:Url"] = "https://+:8443",
            ["Kestrel:Endpoints:EdgeMtls:Certificate:Path"] = "/https/edge-api.pfx"
        });

        ExecutionEndpointMutualTlsListenerConfigurationValidator.Validate(
            configuration,
            new ProductionHostEnvironment());
    }

    private static IConfiguration BuildConfiguration(IReadOnlyDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private sealed class ProductionHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "IceBot.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

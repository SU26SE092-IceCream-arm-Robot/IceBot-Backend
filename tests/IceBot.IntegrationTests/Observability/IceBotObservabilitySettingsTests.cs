using Microsoft.Extensions.Configuration;
using WebAPI.Configuration.Observability;

namespace IceBot.IntegrationTests.Observability;

public sealed class IceBotObservabilitySettingsTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ReadsMetricsAndTracingExporterSwitchesIndependently(bool metricsEnabled, bool tracingEnabled)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:OpenTelemetry:Metrics:ExporterEnabled"] = metricsEnabled.ToString(),
                ["Observability:OpenTelemetry:Tracing:ExporterEnabled"] = tracingEnabled.ToString(),
                ["Observability:OpenTelemetry:Metrics:OtlpEndpoint"] = "http://metrics-collector:4317",
                ["Observability:OpenTelemetry:Tracing:OtlpEndpoint"] = "http://traces-collector:4318",
                ["Observability:OpenTelemetry:Tracing:OtlpProtocol"] = "http/protobuf"
            })
            .Build();

        var settings = IceBotObservabilitySettingsReader.Read(configuration, "Staging", "instance-1");

        Assert.Equal(metricsEnabled, settings.MetricsExporter.Enabled);
        Assert.Equal(tracingEnabled, settings.TracingExporter.Enabled);
        Assert.Equal("http://metrics-collector:4317", settings.MetricsExporter.Endpoint);
        Assert.Equal("http://traces-collector:4318", settings.TracingExporter.Endpoint);
        Assert.Equal("http/protobuf", settings.TracingExporter.Protocol);
        Assert.Equal("Staging", settings.DeploymentEnvironment);
        Assert.Equal("instance-1", settings.InstanceId);
    }
}

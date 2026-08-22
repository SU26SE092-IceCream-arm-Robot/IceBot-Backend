namespace WebAPI.Configuration.Observability;

public sealed record OtlpSignalExporterSettings(bool Enabled, string Endpoint, string Protocol);

public sealed record IceBotObservabilitySettings(
    string ServiceName,
    bool OpenTelemetryEnabled,
    OtlpSignalExporterSettings MetricsExporter,
    OtlpSignalExporterSettings TracingExporter,
    string SerilogEndpoint,
    string SerilogProtocol,
    string DeploymentEnvironment,
    string InstanceId);

public static class IceBotObservabilitySettingsReader
{
    private const string DefaultServiceName = "IceBot.WebAPI";
    private const string DefaultOtlpEndpoint = "http://localhost:18889";
    private const string DefaultOtlpProtocol = "grpc";

    public static IceBotObservabilitySettings Read(
        IConfiguration configuration,
        string environmentName,
        string instanceId)
    {
        var otelSection = configuration.GetSection("Observability:OpenTelemetry");

        return new IceBotObservabilitySettings(
            configuration.GetValue<string>("Observability:ServiceName") ?? DefaultServiceName,
            otelSection.GetValue("Enabled", true),
            ReadSignalExporter(otelSection.GetSection("Metrics")),
            ReadSignalExporter(otelSection.GetSection("Tracing")),
            configuration.GetValue<string>("Observability:Serilog:OtlpEndpoint") ?? DefaultOtlpEndpoint,
            configuration.GetValue<string>("Observability:Serilog:OtlpProtocol") ?? DefaultOtlpProtocol,
            configuration.GetValue<string>("Observability:DeploymentEnvironment") ?? environmentName,
            configuration.GetValue<string>("Observability:InstanceId") ?? instanceId);
    }

    private static OtlpSignalExporterSettings ReadSignalExporter(IConfigurationSection section) => new(
        section.GetValue("ExporterEnabled", false),
        section.GetValue<string>("OtlpEndpoint") ?? DefaultOtlpEndpoint,
        section.GetValue<string>("OtlpProtocol") ?? DefaultOtlpProtocol);
}

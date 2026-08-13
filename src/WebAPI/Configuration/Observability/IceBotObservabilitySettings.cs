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
        var legacyEndpoint = otelSection.GetValue<string>("OtlpEndpoint") ?? DefaultOtlpEndpoint;
        var legacyProtocol = otelSection.GetValue<string>("OtlpProtocol") ?? DefaultOtlpProtocol;
        var legacyExporterEnabled = otelSection.GetValue("OtlpExporterEnabled", false);

        return new IceBotObservabilitySettings(
            configuration.GetValue<string>("Observability:ServiceName") ?? DefaultServiceName,
            otelSection.GetValue("Enabled", true),
            ReadSignalExporter(otelSection.GetSection("Metrics"), legacyExporterEnabled, legacyEndpoint, legacyProtocol),
            ReadSignalExporter(otelSection.GetSection("Tracing"), legacyExporterEnabled, legacyEndpoint, legacyProtocol),
            configuration.GetValue<string>("Observability:Serilog:OtlpEndpoint") ?? legacyEndpoint,
            configuration.GetValue<string>("Observability:Serilog:OtlpProtocol") ?? legacyProtocol,
            configuration.GetValue<string>("Observability:DeploymentEnvironment") ?? environmentName,
            configuration.GetValue<string>("Observability:InstanceId") ?? instanceId);
    }

    private static OtlpSignalExporterSettings ReadSignalExporter(
        IConfigurationSection section,
        bool legacyExporterEnabled,
        string legacyEndpoint,
        string legacyProtocol) => new(
        section.GetValue<bool?>("ExporterEnabled") ?? legacyExporterEnabled,
        section.GetValue<string>("OtlpEndpoint") ?? legacyEndpoint,
        section.GetValue<string>("OtlpProtocol") ?? legacyProtocol);
}

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using Application.EdgeIntegration.Observability;
using Application.Payments.PaymentSessions.Observability;
using Application.ProductionPackages.Upgrades;
using Infrastructure.Payments.Observability;
using Infrastructure.Firebase;
using Application.RobotConfiguration.AuthoringImports;
using Infrastructure.Operations.Notifications;
using Infrastructure.Operations.Automation;
using Infrastructure.Devices.Credentials.Observability;

namespace WebAPI.Configuration.Observability;

public static class ObservabilityExtensions
{
    private const string DefaultServiceName = "IceBot.WebAPI";

    /// <summary>
    /// Configures Serilog structured logging and OpenTelemetry traces/metrics
    /// as a single observability extension for IceBot.WebAPI.
    /// </summary>
    public static WebApplicationBuilder AddIceBotObservability(this WebApplicationBuilder builder)
    {
        var serviceName = builder.Configuration.GetValue<string>("Observability:ServiceName")
                          ?? DefaultServiceName;

        var serviceVersion = typeof(ObservabilityExtensions).Assembly
            .GetName().Version?.ToString() ?? "0.0.0";

        var otelSection = builder.Configuration.GetSection("Observability:OpenTelemetry");
        var otlpEndpoint = otelSection.GetValue<string>("OtlpEndpoint") ?? "http://localhost:18889";
        var otlpProtocol = otelSection.GetValue<string>("OtlpProtocol") ?? "grpc";

        // --- Serilog ---
        builder.Host.UseSerilog(
            (ctx, services, config) =>
            {
                config.ReadFrom.Configuration(ctx.Configuration)
                      .ReadFrom.Services(services);

                var serilogOtlpSinkEnabled = ctx.Configuration
                    .GetValue("Observability:Serilog:OtlpSinkEnabled", false);

                if (serilogOtlpSinkEnabled)
                {
                    config.WriteTo.OpenTelemetry(options =>
                    {
                        options.Endpoint = otlpEndpoint;
                        options.Protocol = ParseSerilogOtlpProtocol(otlpProtocol);
                        options.ResourceAttributes = new Dictionary<string, object>
                        {
                            ["service.name"] = serviceName,
                            ["service.version"] = serviceVersion
                        };
                        options.OnBeginSuppressInstrumentation =
                            OpenTelemetry.SuppressInstrumentationScope.Begin;
                    });
                }
            },
            writeToProviders: !builder.Environment.IsDevelopment());

        // --- OpenTelemetry ---
        var otelEnabled = otelSection.GetValue("Enabled", true);

        if (!otelEnabled)
        {
            return builder;
        }

        var otlpExporterEnabled = otelSection.GetValue("OtlpExporterEnabled", false);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion);
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(RobotAuthoringImportObservability.InstrumentationName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Filter out health and swagger noise
                        options.Filter = httpContext =>
                        {
                            var path = httpContext.Request.Path.Value ?? string.Empty;
                            return !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                                && !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                                && !path.StartsWith("/info", StringComparison.OrdinalIgnoreCase);
                        };
                    })
                    .AddHttpClientInstrumentation();

                if (otlpExporterEnabled)
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = ParseOtlpProtocol(otlpProtocol);
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(IceBotEdgeMetrics.MeterName)
                    .AddMeter(PaymentWebhookMetrics.MeterName)
                    .AddMeter(PayOsResilienceMetrics.MeterName)
                    .AddMeter(FirebaseAccountPushNotificationSender.MeterName)
                    .AddMeter(RobotAuthoringImportObservability.InstrumentationName)
                    .AddMeter(ProductionPackageUpgradeMetrics.MeterName)
                    .AddMeter(NotificationDeliveryMetrics.MeterName)
                    .AddMeter(OperationalAutomationMetrics.MeterName)
                    .AddMeter(MqttCredentialReconciliationMetrics.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (otlpExporterEnabled)
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = ParseOtlpProtocol(otlpProtocol);
                    });
                }
            });

        return builder;
    }

    private static OpenTelemetry.Exporter.OtlpExportProtocol ParseOtlpProtocol(string protocol)
    {
        return protocol.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase)
            ? OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf
            : OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    }

    private static OtlpProtocol ParseSerilogOtlpProtocol(string protocol)
    {
        return protocol.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase)
            ? OtlpProtocol.HttpProtobuf
            : OtlpProtocol.Grpc;
    }
}

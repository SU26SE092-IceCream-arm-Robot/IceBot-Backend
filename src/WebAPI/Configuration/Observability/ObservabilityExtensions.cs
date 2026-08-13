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
using Infrastructure.SalesCatalog.Caching;

namespace WebAPI.Configuration.Observability;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Configures Serilog structured logging and OpenTelemetry traces/metrics
    /// as a single observability extension for IceBot.WebAPI.
    /// </summary>
    public static WebApplicationBuilder AddIceBotObservability(this WebApplicationBuilder builder)
    {
        var serviceVersion = typeof(ObservabilityExtensions).Assembly
            .GetName().Version?.ToString() ?? "0.0.0";
        var settings = IceBotObservabilitySettingsReader.Read(
            builder.Configuration,
            builder.Environment.EnvironmentName,
            $"{Environment.MachineName}:{Environment.ProcessId}");

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
                        options.Endpoint = settings.SerilogEndpoint;
                        options.Protocol = ParseSerilogOtlpProtocol(settings.SerilogProtocol);
                        options.ResourceAttributes = new Dictionary<string, object>
                        {
                            ["service.name"] = settings.ServiceName,
                            ["service.version"] = serviceVersion,
                            ["service.instance.id"] = settings.InstanceId,
                            ["deployment.environment.name"] = settings.DeploymentEnvironment
                        };
                        options.OnBeginSuppressInstrumentation =
                            OpenTelemetry.SuppressInstrumentationScope.Begin;
                    });
                }
            },
            writeToProviders: !builder.Environment.IsDevelopment());

        // --- OpenTelemetry ---
        if (!settings.OpenTelemetryEnabled)
        {
            return builder;
        }

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: settings.ServiceName,
                    serviceVersion: serviceVersion)
                    .AddAttributes([
                        new KeyValuePair<string, object>("service.instance.id", settings.InstanceId),
                        new KeyValuePair<string, object>("deployment.environment.name", settings.DeploymentEnvironment)
                    ]);
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

                if (settings.TracingExporter.Enabled)
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(settings.TracingExporter.Endpoint);
                        options.Protocol = ParseOtlpProtocol(settings.TracingExporter.Protocol);
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
                    .AddMeter(RuntimeMenuProjectionCache.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (settings.MetricsExporter.Enabled)
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(settings.MetricsExporter.Endpoint);
                        options.Protocol = ParseOtlpProtocol(settings.MetricsExporter.Protocol);
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

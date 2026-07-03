using Application.Abstractions.Persistence;
using Application.Dashboard.Abstractions;
using Application.Devices.Abstractions;
using Application.Email;
using Application.Inventory.Abstractions;
using Application.Operations.Abstractions;
using Application.EdgeIntegration.Abstractions;
using Application.ProductionConfiguration.Abstractions;
using Application.RobotConfiguration.Abstractions;
using Infrastructure.Catalog;
using Infrastructure.Dashboard.Persistence;
using Infrastructure.Data;
using Infrastructure.Devices.Persistence;
using Infrastructure.Email;
using Infrastructure.EdgeIntegration.Persistence;
using Infrastructure.EdgeIntegration.Mqtt;
using Infrastructure.Identity;
using Infrastructure.Inventory.Persistence;
using Infrastructure.Operations.Persistence;
using Infrastructure.Orders;
using Infrastructure.Payments;
using Infrastructure.ProductionConfiguration.Persistence;
using Infrastructure.RobotConfiguration.ObjectStorage;
using Infrastructure.RobotConfiguration.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.SalesCatalog;
using Infrastructure.Sync;
using Infrastructure.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<IceBotDbContext>((sp, opt) =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();

                var cs = configuration["CONNECTIONSTRING"];

                if (string.IsNullOrWhiteSpace(cs))
                    cs = configuration.GetConnectionString("IceBot_DB");

                if (string.IsNullOrWhiteSpace(cs))
                    throw new InvalidOperationException(
                        "Missing DB connection string. Set CONNECTIONSTRING or ConnectionStrings:IceBot_DB.");

                opt.UseNpgsql(cs);
            });

            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddScoped<Concurrency.PostgresAdvisoryLockManager>();
            services.Configure<EmailOptions>(config.GetSection(EmailOptions.SectionName));
            services.AddScoped<IEmailSender, MailKitEmailSender>();
            services.AddCatalogInfrastructure();
            services.AddIdentityInfrastructure();
            services.AddOrdersInfrastructure();
            services.AddPaymentsInfrastructure(config);
            services.AddSalesCatalogInfrastructure();
            services.AddTenantsInfrastructure();
            services.AddScoped<IInventoryStore, InventoryStore>();
            services.AddOptions<Persistence.Jobs.DataRetentionOptions>()
                .Bind(config.GetSection(Persistence.Jobs.DataRetentionOptions.SectionName))
                .Validate(options =>
                        options.IntervalHours > 0 &&
                        options.HeartbeatDays > 0 &&
                        options.DeviceEventDays > 0 &&
                        options.OperationLogDays > 0 &&
                        options.ProcessedSyncInboxDays > 0 &&
                        options.BatchSize > 0 &&
                        options.MaxBatchesPerRun > 0,
                    "Data retention settings must be positive.")
                .ValidateOnStart();
            services.AddScoped<Persistence.Jobs.DataRetentionPurger>();
            services.AddHostedService<Persistence.Jobs.DataRetentionJob>();
            services.AddScoped<IKioskTelemetryStore, KioskTelemetryStore>();
            services.AddScoped<EdgeTelemetryIngestionStore>();
            services.AddScoped<IEdgeTelemetryIngestionStore>(provider => provider.GetRequiredService<EdgeTelemetryIngestionStore>());
            services.AddScoped<IAlertIngestionStore>(provider => provider.GetRequiredService<EdgeTelemetryIngestionStore>());
            services.AddScoped<IBatchEventSyncStore, BatchEventSyncStore>();
            services.AddScoped<IProductionEventSyncStore, ProductionEventSyncStore>();
            services.AddScoped<IExecutionReadinessStore, ExecutionReadinessStore>();
            services.AddScoped<Application.Sync.Abstractions.ISyncDeadLetterStore, SyncDeadLetterStore>();
            services.AddOptions<Application.Devices.EdgeTelemetryIngestionOptions>()
                .Bind(config.GetSection(Application.Devices.EdgeTelemetryIngestionOptions.SectionName))
                .Validate(options =>
                        options.MaxFutureClockSkewSeconds >= 0 &&
                        options.HeartbeatTimeoutSeconds > 0 &&
                        options.ConnectivityReconciliationIntervalSeconds > 0 &&
                        options.ConnectivityReconciliationBatchSize > 0 &&
                        options.MaxBatchEventCount > 0 &&
                        options.AlertCorrelationWindowMinutes > 0,
                    "Edge telemetry clock skew and connectivity reconciliation settings are invalid.")
                .ValidateOnStart();
            services.AddHostedService<Devices.Jobs.KioskConnectivityReconciliationJob>();
            services.AddScoped<IDeviceManagementStore, DeviceManagementStore>();
            services.AddScoped<IExecutionEndpointStore, ExecutionEndpointStore>();
            services.AddScoped<IDashboardStore, DashboardStore>();
            services.AddScoped<IMaintenanceTicketStore, MaintenanceTicketStore>();
            services.AddScoped<IAlertStore, AlertStore>();
            services.AddOptions<RobotArtifactObjectStorageOptions>()
                .Bind(config.GetSection(RobotArtifactObjectStorageOptions.SectionName))
                .Validate(options =>
                        !string.IsNullOrWhiteSpace(options.Endpoint) &&
                        !string.IsNullOrWhiteSpace(options.AccessKey) &&
                        !string.IsNullOrWhiteSpace(options.SecretKey) &&
                        !string.IsNullOrWhiteSpace(options.BucketName) &&
                        options.DownloadUrlExpirySeconds is >= 60 and <= 604800 &&
                        options.OrphanGracePeriodHours is >= 1 and <= 720 &&
                        options.OrphanCleanupIntervalHours is >= 1 and <= 168 &&
                        options.OrphanCleanupMaxDeletesPerRun is >= 1 and <= 10000,
                    "Robot artifact object storage settings are invalid.")
                .ValidateOnStart();
            services.AddScoped<IArtifactObjectStorage, MinioArtifactObjectStorage>();
            services.AddHostedService<RobotArtifactObjectStorageStartupValidator>();
            services.AddHostedService<RobotConfiguration.Jobs.RobotArtifactOrphanCleanupJob>();
            services.AddScoped<IRobotConfigurationStore, RobotConfigurationStore>();
            services.AddScoped<IRobotArtifactTemplateStore, RobotArtifactTemplateStore>();
            services.AddScoped<IProductionConfigurationStore, ProductionConfigurationStore>();
            services.AddOptions<Application.ProductionConfiguration.LowCostControllerCapacityOptions>()
                .Bind(config.GetSection(Application.ProductionConfiguration.LowCostControllerCapacityOptions.SectionName))
                .Validate(options => options.MaxArtifactCount > 0 && options.MaxArtifactStorageBytes > 0,
                    "Low-cost controller capacity limits must be positive.")
                .ValidateOnStart();
            services.Configure<ProductionConfiguration.Jobs.DeploymentTimeoutReconciliationOptions>(
                config.GetSection(ProductionConfiguration.Jobs.DeploymentTimeoutReconciliationOptions.SectionName));
            services.AddHostedService<ProductionConfiguration.Jobs.DeploymentTimeoutReconciliationJob>();
            services.AddScoped<IEdgeCommandStore, EdgeCommandStore>();
            services.AddScoped<IOrderExecutionDispatchStore, OrderExecutionDispatchStore>();
            services.AddScoped<IOrderExecutionTimeoutStore, OrderExecutionTimeoutStore>();
            services.AddScoped<IExecutionEndpointTransportAuthStore, ExecutionEndpointTransportAuthStore>();
            services.AddScoped<ExecutionReportStore>();
            services.AddScoped<IExecutionReportReceiptStore>(provider => provider.GetRequiredService<ExecutionReportStore>());
            services.AddScoped<IDeploymentReportStore>(provider => provider.GetRequiredService<ExecutionReportStore>());
            services.AddScoped<IProductionExecutionReportStore>(provider => provider.GetRequiredService<ExecutionReportStore>());
            services.AddScoped<IExecutionStockEvidenceStore>(provider => provider.GetRequiredService<ExecutionReportStore>());
            services.AddOptions<EdgeCommandMqttOptions>()
                .Bind(config.GetSection(EdgeCommandMqttOptions.SectionName))
                .Validate(options =>
                        !options.Enabled ||
                        (!string.IsNullOrWhiteSpace(options.Host) &&
                         options.Port is > 0 and <= 65535 &&
                         !string.IsNullOrWhiteSpace(options.ClientId) &&
                         !string.IsNullOrWhiteSpace(options.TopicPrefix) &&
                         options.ConnectTimeoutSeconds > 0),
                    "Enabled MQTT command wake-up settings are incomplete or invalid.")
                .ValidateOnStart();
            services.AddSingleton<IEdgeCommandWakeUpPublisher, MqttEdgeCommandWakeUpPublisher>();
            services.AddOptions<MqttCredentialProvisioningOptions>()
                .Bind(config.GetSection(MqttCredentialProvisioningOptions.SectionName))
                .Validate(options => !options.Enabled ||
                    (!string.IsNullOrWhiteSpace(options.Host) && options.Port > 0 &&
                     !string.IsNullOrWhiteSpace(options.AdminUsername) &&
                     !string.IsNullOrWhiteSpace(options.AdminPassword) &&
                     !string.IsNullOrWhiteSpace(options.SubscriberRole) &&
                     !string.IsNullOrWhiteSpace(options.TopicPrefix) && options.TimeoutSeconds > 0),
                    "Enabled MQTT credential provisioning settings are incomplete or invalid.")
                .ValidateOnStart();
            services.AddScoped<IMqttEndpointCredentialProvisioner, MosquittoDynamicSecurityCredentialProvisioner>();
            services.AddOptions<Application.EdgeIntegration.ExecutionReportIngestionOptions>()
                .Bind(config.GetSection(Application.EdgeIntegration.ExecutionReportIngestionOptions.SectionName))
                .Validate(options => options.MaxFutureClockSkewSeconds >= 0,
                    "Execution report future clock skew cannot be negative.")
                .ValidateOnStart();
            services.AddOptions<Application.EdgeIntegration.OrderExecutionDispatchOptions>()
                .Bind(config.GetSection(Application.EdgeIntegration.OrderExecutionDispatchOptions.SectionName))
                .Validate(options =>
                        options.CommandExpiryMinutes > 0 &&
                        options.MaxActiveCommandsPerEndpoint > 0 &&
                        options.ReconciliationIntervalSeconds > 0 &&
                        options.ReconciliationBatchSize > 0 &&
                        options.TimeoutReconciliationIntervalSeconds > 0 &&
                        options.TimeoutReconciliationBatchSize > 0 &&
                        options.AcceptedReportTimeoutMinutes > 0 &&
                        options.RunningReportTimeoutMinutes > 0 &&
                        options.HeartbeatUnreachableMinutes > 0 &&
                        options.UnreachableSupportEscalationMinutes > 0 &&
                        options.MaxDispatchAttempts > 0,
                    "Order execution dispatch settings must be positive.")
                .ValidateOnStart();
            services.AddHostedService<EdgeIntegration.Jobs.OrderExecutionDispatchReconciliationJob>();
            services.AddHostedService<EdgeIntegration.Jobs.OrderExecutionTimeoutReconciliationJob>();
            services.AddHostedService<EdgeIntegration.Jobs.ExecutionMetricsCollectionJob>();

            return services;
        }
    }
}

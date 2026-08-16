using Infrastructure.RobotConfiguration.Storage.Jobs;
using Infrastructure.RobotConfiguration.ArtifactTemplates.Persistence;
using Application.RobotConfiguration.ArtifactTemplates.Abstractions;
using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using Infrastructure.Devices.Connectivity.Jobs;
using Infrastructure.Devices.Connectivity.Persistence;
using Infrastructure.Devices.Telemetry.Persistence;
using Infrastructure.Sync.Persistence;
using Infrastructure.Devices.ExecutionEndpoints.Persistence;
using Application.Abstractions.Persistence;
using Application.Dashboard.Abstractions;
using Application.Devices.Catalog.Abstractions;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Connectivity.Abstractions;
using Application.Devices.Credentials.Abstractions;
using Application.Sync.Ingestion.Abstractions;
using Application.Email;
using Application.Inventory.Abstractions;
using Application.Operations.Abstractions;
using Application.Operations.OperationLogs.Abstractions;
using Application.EdgeIntegration.Abstractions;
using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Deployments.Queries;
using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Routes.Abstractions;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Programs.Abstractions;
using Infrastructure.Catalog;
using Infrastructure.Catalog.Bootstrap;
using Infrastructure.Devices.Bootstrap;
using Infrastructure.Dashboard.Persistence;
using Infrastructure.Data;
using Infrastructure.Devices.Catalog.Persistence;
using Infrastructure.Email;
using Infrastructure.EdgeIntegration.Persistence;
using Infrastructure.EdgeIntegration.Mqtt;
using Infrastructure.Identity;
using Infrastructure.Inventory.Persistence;
using Infrastructure.Operations.Persistence;
using Infrastructure.Orders;
using Infrastructure.Payments;
using Infrastructure.ProductionConfiguration.Persistence.Deployments;
using Infrastructure.ProductionConfiguration.Persistence.Releases;
using Infrastructure.ProductionConfiguration.Persistence.Routes;
using Infrastructure.ProductionConfiguration.Persistence.Bindings;
using Infrastructure.ProductionConfiguration.ObjectStorage;
using Infrastructure.ProductionPackages;
using Application.ProductionPackages;
using Application.ProductionPackages.Installation;
using Infrastructure.RobotConfiguration.Storage.ObjectStorage;
using Infrastructure.RobotConfiguration.Artifacts.Persistence;
using Infrastructure.RobotConfiguration.Programs.Persistence;
using Infrastructure.RobotConfiguration.ArtifactContracts;
using Infrastructure.RobotConfiguration.AuthoringImports.Persistence;
using Application.RobotConfiguration.AuthoringImports;
using Application.RobotConfiguration.AuthoringImports.RecipeSuggestions;
using Application.RobotConfiguration.ArtifactContracts;
using Infrastructure.Persistence.Repositories;
using Infrastructure.SalesCatalog;
using Infrastructure.ServiceRegistration;
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
            services.AddOptions<EmailOptions>()
                .Bind(config.GetSection(EmailOptions.SectionName))
                .Validate(options => options.OperationTimeoutSeconds is >= 1 and <= 300,
                    "SMTP operation timeout must be between 1 and 300 seconds.")
                .Validate(options => Uri.TryCreate(options.InvitationBaseUrl, UriKind.Absolute, out _),
                    "Email invitation base URL must be an absolute URL.")
                .ValidateOnStart();
            services.AddScoped<IEmailSender, MailKitEmailSender>();
            services.AddCatalogInfrastructure();
            services.AddIdentityInfrastructure(config);
            services.AddScoped<DevelopmentIceBotDemoReset>();
            services.AddHostedService<DevelopmentVanillaSoftServeCatalogSeedHostedService>();
            services.AddHostedService<DevelopmentExecutionEndpointSeedHostedService>();
            services.AddHostedService<DevelopmentVanillaSoftServeTopologySeedHostedService>();
            services.AddOrdersInfrastructure();
            services.AddOptions<Application.Orders.Management.Automation.FulfillmentReminderOptions>()
                .Bind(config.GetSection(Application.Orders.Management.Automation.FulfillmentReminderOptions.SectionName))
                .Validate(options => options.IntervalSeconds > 0 && options.BatchSize is >= 1 and <= 500,
                    "Fulfillment reminder settings are invalid.")
                .ValidateOnStart();
            services.AddHostedService<Orders.Jobs.FulfillmentReminderJob>();
            services.AddPaymentsInfrastructure(config);
            services.AddSalesCatalogInfrastructure(config);
            services.AddServiceRegistrationInfrastructure();
            services.AddTenantsInfrastructure();
            services.AddScoped<IInventoryStore, InventoryStore>();
            services.AddScoped<IInventorySensorObservationStore, InventorySensorObservationStore>();
            services.AddOptions<Persistence.Jobs.DataRetentionOptions>()
                .Bind(config.GetSection(Persistence.Jobs.DataRetentionOptions.SectionName))
                .Validate(options =>
                        options.IntervalHours > 0 &&
                        options.HeartbeatDays > 0 &&
                        options.DeviceEventDays > 0 &&
                        options.OperationLogDays > 0 &&
                        options.ProcessedSyncInboxDays > 0 &&
                        options.ExpiredIdentityCredentialDays > 0 &&
                        options.NotificationDeliveryDays > 0 &&
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
            services.AddScoped<IExecutionEndpointReportedDeviceStore>(provider =>
                (ExecutionReadinessStore)provider.GetRequiredService<IExecutionReadinessStore>());
            services.AddScoped<Application.Sync.Abstractions.ISyncDeadLetterStore, SyncDeadLetterStore>();
            services.AddOptions<Application.Devices.Telemetry.EdgeTelemetryIngestionOptions>()
                .Bind(config.GetSection(Application.Devices.Telemetry.EdgeTelemetryIngestionOptions.SectionName))
                .Validate(options =>
                        options.MaxFutureClockSkewSeconds >= 0 &&
                        options.HeartbeatTimeoutSeconds > 0 &&
                        options.ConnectivityReconciliationIntervalSeconds > 0 &&
                        options.ConnectivityReconciliationBatchSize > 0 &&
                        options.MaxBatchEventCount > 0 &&
                        options.AlertCorrelationWindowMinutes > 0 &&
                        options.AlertAutomationMaxEventAgeMinutes > 0 &&
                        options.ReadinessTimeoutSeconds > 0,
                    "Edge telemetry clock skew and connectivity reconciliation settings are invalid.")
                .ValidateOnStart();
            services.AddHostedService<KioskConnectivityReconciliationJob>();
            services.AddScoped<IDeviceManagementStore, DeviceManagementStore>();
            services.AddScoped<IExecutionEndpointStore, ExecutionEndpointStore>();
            services.AddScoped<IDashboardStore, DashboardStore>();
            services.AddScoped<IMaintenanceTicketStore, MaintenanceTicketStore>();
            services.AddScoped<IAlertStore, AlertStore>();
            services.AddScoped<Application.Operations.Alerts.Automation.IInventoryAlertAutomationStore,
                InventoryAlertAutomationStore>();
            services.AddScoped<Application.Operations.Alerts.Automation.IMqttCredentialAlertAutomationStore,
                MqttCredentialAlertAutomationStore>();
            services.AddOptions<Application.Operations.Alerts.Automation.InventoryAlertAutomationOptions>()
                .Bind(config.GetSection(Application.Operations.Alerts.Automation.InventoryAlertAutomationOptions.SectionName))
                .Validate(options =>
                        options.IntervalSeconds > 0 && options.BatchSize is >= 1 and <= 500 &&
                        options.MaxBatchesPerRun > 0,
                    "Inventory alert automation settings are invalid.")
                .ValidateOnStart();
            services.AddScoped(provider => new Application.Operations.Alerts.Automation.InventoryAlertReconciler(
                provider.GetRequiredService<Application.Operations.Alerts.Automation.IInventoryAlertAutomationStore>(),
                provider.GetRequiredService<Application.Abstractions.Realtime.IRealtimeNotificationPublisher>(),
                provider.GetRequiredService<Application.Operations.Alerts.Notifications.IInventoryOperationalAlertNotifier>(),
                provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Application.Operations.Alerts.Automation.InventoryAlertAutomationOptions>>().Value,
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Application.Operations.Alerts.Automation.InventoryAlertReconciler>>()));
            services.AddHostedService<Operations.Jobs.InventoryAlertReconciliationJob>();
            services.AddScoped<Application.Operations.Alerts.Notifications.IOperationalAlertNotificationRecipientStore,
                CriticalAlertNotificationRecipientStore>();
            services.AddScoped<Application.Operations.Alerts.Notifications.INotificationDeliveryStore,
                NotificationDeliveryStore>();
            services.AddScoped<Application.Operations.Notifications.Diagnostics.INotificationDeliveryReadStore,
                NotificationDeliveryReadStore>();
            services.AddScoped<Application.Operations.Notifications.IMaintenanceAssignmentNotificationRecipientStore,
                MaintenanceAssignmentNotificationRecipientStore>();
            services.AddOptions<Operations.Notifications.NotificationDeliveryOptions>()
                .Bind(config.GetSection(Operations.Notifications.NotificationDeliveryOptions.SectionName))
                .Validate(options =>
                        options.IntervalSeconds > 0 && options.BatchSize is >= 1 and <= 500 &&
                        options.ProcessingTimeoutSeconds > 0 && options.BaseRetryDelaySeconds > 0,
                    "Notification delivery settings are invalid.")
                .ValidateOnStart();
            services.AddHostedService<Operations.Notifications.NotificationDeliveryJob>();
            services.AddScoped<IOperationLogStore, OperationLogStore>();
            services.AddOptions<RobotArtifactObjectStorageOptions>()
                .Bind(config.GetSection(RobotArtifactObjectStorageOptions.SectionName))
                .Validate(options =>
                        !string.IsNullOrWhiteSpace(options.Endpoint) &&
                        !string.IsNullOrWhiteSpace(options.AccessKey) &&
                        !string.IsNullOrWhiteSpace(options.SecretKey) &&
                        !string.IsNullOrWhiteSpace(options.BucketName) &&
                        options.DownloadUrlExpirySeconds is >= 60 and <= 604800 &&
                        options.ReadRetryCount is >= 0 and <= 5 &&
                        options.ReadRetryDelayMilliseconds is >= 1 and <= 10000 &&
                        options.OrphanGracePeriodHours is >= 1 and <= 720 &&
                        options.OrphanCleanupIntervalHours is >= 1 and <= 168 &&
                        options.OrphanCleanupMaxDeletesPerRun is >= 1 and <= 10000 &&
                        options.AuthoringImportRetentionHours is >= 24 and <= 2160,
                    "Robot artifact object storage settings are invalid.")
                .ValidateOnStart();
            services.AddScoped<IArtifactObjectStorage, MinioArtifactObjectStorage>();
            services.AddScoped<IArtifactObjectReferenceSource, RobotConfigurationObjectReferenceSource>();
            services.AddScoped<IArtifactObjectReferenceSource, ConfigurationReleaseBundleReferenceSource>();
            services.AddHostedService<RobotArtifactObjectStorageStartupValidator>();
            services.AddHostedService<RobotArtifactOrphanCleanupJob>();
            services.AddScoped<IRobotArtifactStore, RobotArtifactStore>();
            services.AddScoped<IRobotArtifactUsageReader, RobotArtifactUsageReader>();
            services.AddScoped<IRobotProgramStore, RobotProgramStore>();
            services.AddScoped<IRobotArtifactTemplateStore, RobotArtifactTemplateStore>();
            services.AddScoped<IRobotArtifactTechnicalContractStore, RobotArtifactTechnicalContractStore>();
            services.AddScoped<IRobotAuthoringImportStore, RobotAuthoringImportStore>();
            services.AddScoped<IRobotAuthoringRecipeSuggestionStore, RobotAuthoringRecipeSuggestionStore>();
            services.AddScoped<Application.Shared.Concurrency.ITechnicalResourceMutationCoordinator,
                Concurrency.PostgresTechnicalResourceMutationCoordinator>();
            services.AddScoped<IConfigurationReleaseStore, ConfigurationReleaseStore>();
              services.AddScoped<IConfigurationRouteStore, ConfigurationRouteStore>();
              services.AddScoped<Application.ProductionConfiguration.Bindings.IProductionProgramBindingStore,
                  ProductionProgramBindingStore>();
            services.AddScoped<ConfigurationDeploymentStore>();
            services.AddScoped<IConfigurationDeploymentStore>(provider =>
                provider.GetRequiredService<ConfigurationDeploymentStore>());
            services.AddScoped<IConfigurationDeploymentObservationReader>(provider =>
                provider.GetRequiredService<ConfigurationDeploymentStore>());
            services.AddScoped<IConfigurationDeploymentArtifactReader, ConfigurationDeploymentArtifactReader>();
            services.AddScoped<Application.ProductionConfiguration.Deployments.Notifications.IDeploymentFailureNotificationStore,
                DeploymentFailureNotificationStore>();
            services.AddScoped<IProductionPackageStore, ProductionPackageStore>();
            services.AddScoped<IProductionPackageInstallationStore, ProductionPackageInstallationStore>();
            services.AddScoped<Application.ProductionPackages.Upgrades.IProductionPackageUpgradeStore, ProductionPackageUpgradeStore>();
            services.AddOptions<ProductionPackages.Jobs.ProductionPackageUpgradeReconciliationOptions>()
                .Bind(config.GetSection(
                    ProductionPackages.Jobs.ProductionPackageUpgradeReconciliationOptions.SectionName))
                .Validate(options => options.IntervalSeconds is >= 10 and <= 3600 &&
                                     options.MaterializingTimeoutMinutes is >= 1 and <= 1440 &&
                                     options.BatchSize is >= 1 and <= 500,
                    "Production package upgrade reconciliation settings are invalid.")
                .ValidateOnStart();
            services.AddHostedService<ProductionPackages.Jobs.ProductionPackageUpgradeReconciliationJob>();
            services.AddScoped<Application.ProductionPackages.Workspace.IProductionPackageWorkspaceStore, ProductionPackageWorkspaceStore>();
            services.AddScoped<Application.ProductionPackages.Ownership.IProductionPackageTechnicalOwnershipStore,
                ProductionPackageTechnicalOwnershipStore>();
            services.AddOptions<Application.ProductionConfiguration.Deployments.LowCostControllerCapacityOptions>()
                .Bind(config.GetSection(Application.ProductionConfiguration.Deployments.LowCostControllerCapacityOptions.SectionName))
                .Validate(options => options.MaxArtifactCount > 0 && options.MaxArtifactStorageBytes > 0,
                    "Low-cost controller capacity limits must be positive.")
                .ValidateOnStart();
            services.AddOptions<Application.ProductionConfiguration.Readiness.InventoryReadinessPolicyOptions>()
                .Bind(config.GetSection(Application.ProductionConfiguration.Readiness.InventoryReadinessPolicyOptions.SectionName))
                .Validate(options => Enum.IsDefined(options.PublishPolicy) && Enum.IsDefined(options.DeployPolicy),
                    "Production inventory readiness policies must be Warn or Block.")
                .ValidateOnStart();
            services.Configure<ProductionConfiguration.Jobs.DeploymentTimeoutReconciliationOptions>(
                config.GetSection(ProductionConfiguration.Jobs.DeploymentTimeoutReconciliationOptions.SectionName));
            services.AddHostedService<ProductionConfiguration.Jobs.DeploymentTimeoutReconciliationJob>();
            services.AddOptions<Application.ProductionConfiguration.Deployments.Notifications.DeploymentFailureNotificationOptions>()
                .Bind(config.GetSection(Application.ProductionConfiguration.Deployments.Notifications.DeploymentFailureNotificationOptions.SectionName))
                .Validate(options => options.IntervalSeconds > 0 && options.BatchSize is >= 1 and <= 500,
                    "Deployment failure notification settings are invalid.")
                .ValidateOnStart();
            services.AddHostedService<ProductionConfiguration.Jobs.DeploymentFailureNotificationJob>();
            services.AddScoped<IEdgeCommandStore, EdgeCommandStore>();
            services.AddScoped<IOrderExecutionDispatchStore, OrderExecutionDispatchStore>();
            services.AddScoped<IOrderExecutionTimeoutStore, OrderExecutionTimeoutStore>();
            services.AddScoped<IExecutionEndpointTransportAuthStore, ExecutionEndpointTransportAuthStore>();
            services.AddScoped<ExecutionReportStore>();
            services.AddScoped<IExecutionReportUnitOfWork>(provider => provider.GetRequiredService<ExecutionReportStore>());
            services.AddOptions<EdgeCommandMqttOptions>()
                .Bind(config.GetSection(EdgeCommandMqttOptions.SectionName))
                .Validate(options =>
                        !options.Enabled ||
                        (!string.IsNullOrWhiteSpace(options.Host) &&
                         options.Port is > 0 and <= 65535 &&
                         !string.IsNullOrWhiteSpace(options.ClientId) &&
                         !string.IsNullOrWhiteSpace(options.TopicPrefix) &&
                         options.ConnectTimeoutSeconds > 0 &&
                         options.PublishTimeoutSeconds >= options.ConnectTimeoutSeconds &&
                         options.PublishRetryCount is >= 0 and <= 3 &&
                         options.PublishRetryDelayMilliseconds >= 0),
                    "Enabled MQTT command wake-up settings are incomplete or invalid.")
                .ValidateOnStart();
            services.AddSingleton<IMqttEdgeCommandTransport, MqttNetEdgeCommandTransport>();
            services.AddSingleton<IEdgeCommandWakeUpPublisher, MqttEdgeCommandWakeUpPublisher>();
            services.AddOptions<EdgeUplinkMqttOptions>()
                .Bind(config.GetSection(EdgeUplinkMqttOptions.SectionName))
                .Validate(options =>
                        !options.Enabled ||
                        (!string.IsNullOrWhiteSpace(options.Host) &&
                         options.Port is > 0 and <= 65535 &&
                         !string.IsNullOrWhiteSpace(options.Username) &&
                         !string.IsNullOrWhiteSpace(options.Password) &&
                         !string.IsNullOrWhiteSpace(options.ClientId) &&
                         options.HasValidTopicConfiguration() &&
                         options.ConnectTimeoutSeconds > 0 &&
                         options.PublishTimeoutSeconds > 0 &&
                         options.ReconnectDelaySeconds > 0 &&
                         options.MaxPayloadBytes is >= 1024 and <= 4 * 1024 * 1024 &&
                         options.MaxConcurrentMessages is >= 1 and <= 256),
                    "Enabled MQTT Edge uplink settings are incomplete or invalid.")
                .ValidateOnStart();
            services.AddHostedService<MqttEdgeUplinkConsumer>();
            services.AddOptions<MqttCredentialProvisioningOptions>()
                .Bind(config.GetSection(MqttCredentialProvisioningOptions.SectionName))
                .Validate(options => !options.Enabled ||
                    (!string.IsNullOrWhiteSpace(options.Host) && options.Port > 0 &&
                     !string.IsNullOrWhiteSpace(options.AdminUsername) &&
                     !string.IsNullOrWhiteSpace(options.AdminPassword) &&
                     !string.IsNullOrWhiteSpace(options.SubscriberRole) &&
                     !string.IsNullOrWhiteSpace(options.TopicPrefix) && options.TimeoutSeconds > 0 &&
                     options.RetryCount is >= 0 and <= 3 && options.RetryDelayMilliseconds >= 0 &&
                     options.ReconciliationIntervalSeconds > 0 &&
                     options.ReconciliationBatchSize > 0),
                    "Enabled MQTT credential provisioning settings are incomplete or invalid.")
                .ValidateOnStart();
            services.AddScoped<IMqttEndpointCredentialProvisioner, MosquittoDynamicSecurityCredentialProvisioner>();
            services.AddHostedService<Devices.Credentials.Jobs.MqttCredentialReconciliationJob>();
            services.AddOptions<Application.EdgeIntegration.Reports.ExecutionReportIngestionOptions>()
                .Bind(config.GetSection(Application.EdgeIntegration.Reports.ExecutionReportIngestionOptions.SectionName))
                .Validate(options => options.MaxFutureClockSkewSeconds >= 0,
                    "Execution report future clock skew cannot be negative.")
                .ValidateOnStart();
            services.AddOptions<Application.EdgeIntegration.Dispatch.OrderExecutionDispatchOptions>()
                .Bind(config.GetSection(Application.EdgeIntegration.Dispatch.OrderExecutionDispatchOptions.SectionName))
                .Validate(options =>
                        options.CommandExpiryMinutes > 0 &&
                        options.MaxActiveCommandsPerEndpoint > 0 &&
                        options.ReconciliationIntervalSeconds > 0 &&
                        options.ReconciliationBatchSize > 0 &&
                        options.InitialDispatchSupportEscalationMinutes > 0 &&
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

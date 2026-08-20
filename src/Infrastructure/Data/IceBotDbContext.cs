using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Mapping;
using Application.RobotConfiguration.Programs.Results;
using Application.RobotConfiguration.Programs.Queries;
using Application.RobotConfiguration.Programs.Commands;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Application.RobotConfiguration.ArtifactTemplates.Results;
using Application.RobotConfiguration.ArtifactTemplates.Queries;
using Application.RobotConfiguration.ArtifactTemplates.Commands;
using Domain.RobotConfiguration.ArtifactTemplates;
using Domain.Sync.DeadLetters;
using Domain.Sync.Ingestion;
using Domain.Devices.Telemetry;
using Domain.Devices.ExecutionEndpoints;
using Domain.Catalog.Entities;
using Domain.ContentManagement.Entities;
using Domain.Common;
using Domain.Devices.Catalog;
using Domain.Identity.Entities;
using Domain.Inventory.Entities;
using Domain.Operations.Entities;
using Domain.Orders.Entities;
using Domain.Orders.Incidents;
using Domain.Payments.Entities;
using Domain.ProductionExecution.Projections;
using Domain.ProductionConfiguration.Entities;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.AuthoringImports;
using Domain.ProductionPackages;
using Domain.SalesCatalog.Entities;
using Domain.Sync.Entities;
using Domain.Tenants.Entities;
using Domain.ServiceRegistration.Entities;
using ServiceRegistrationEntity = Domain.ServiceRegistration.Entities.ServiceRegistration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using Domain.Devices.ExecutionEndpoints.Projections;
using Domain.Devices.Connectivity;

namespace Infrastructure.Data;

public class IceBotDbContext : DbContext
{
    // These principals have required, non-soft-deleted evidence dependents. Filtering
    // them globally would hide those dependents or produce EF's required-navigation warning.
    // Normal operational stores must call WhereNotDeleted() for these entity types.
    private static readonly HashSet<Type> ExplicitSoftDeleteVisibilityTypes =
    [
        typeof(Account),
        typeof(Organization),
        typeof(Store),
        typeof(Kiosk),
        typeof(Device),
        typeof(Product),
        typeof(Ingredient),
        typeof(IngredientDispenserState),
        typeof(KioskIngredientInventory),
        typeof(Order),
        typeof(PaymentTransaction),
        typeof(ConfigurationRelease),
        typeof(KioskExecutionEndpoint)
    ];

    public IceBotDbContext(DbContextOptions<IceBotDbContext> options)
        : base(options)
    {
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSaving();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        OnBeforeSaving();
        return base.SaveChanges();
    }

    private void OnBeforeSaving()
    {
        var entries = ChangeTracker.Entries();
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.Entity is IAuditable auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        if (auditable.CreatedAt == default)
                        {
                            auditable.CreatedAt = utcNow;
                        }
                        break;

                    case EntityState.Modified:
                        auditable.UpdatedAt = utcNow;
                        break;
                }
            }
        }
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountRole> AccountRoles => Set<AccountRole>();
    public DbSet<AccountNotificationDevice> AccountNotificationDevices => Set<AccountNotificationDevice>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
    public DbSet<AccountInvitation> AccountInvitations => Set<AccountInvitation>();
    public DbSet<StaffWorkforceLifecycleTransition> StaffWorkforceLifecycleTransitions => Set<StaffWorkforceLifecycleTransition>();
    public DbSet<StaffWorkforceCreateReplay> StaffWorkforceCreateReplays => Set<StaffWorkforceCreateReplay>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationStatusTransition> OrganizationStatusTransitions => Set<OrganizationStatusTransition>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Kiosk> Kiosks => Set<Kiosk>();
    public DbSet<KioskOperationalStateTransition> KioskOperationalStateTransitions => Set<KioskOperationalStateTransition>();
    public DbSet<FranchiseOnboarding> FranchiseOnboardings => Set<FranchiseOnboarding>();
    public DbSet<ServiceRegistrationEntity> ServiceRegistrations => Set<ServiceRegistrationEntity>();

    public DbSet<ContentPage> ContentPages => Set<ContentPage>();
    public DbSet<ContentPageRevision> ContentPageRevisions => Set<ContentPageRevision>();

    public DbSet<DeviceType> DeviceTypes => Set<DeviceType>();
    public DbSet<DeviceModel> DeviceModels => Set<DeviceModel>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceEvent> DeviceEvents => Set<DeviceEvent>();
    public DbSet<KioskHeartbeat> KioskHeartbeats => Set<KioskHeartbeat>();
    public DbSet<KioskConnectivityProjection> KioskConnectivityProjections => Set<KioskConnectivityProjection>();
    public DbSet<KioskExecutionEndpoint> KioskExecutionEndpoints => Set<KioskExecutionEndpoint>();
    public DbSet<ExecutionEndpointCredentialBinding> ExecutionEndpointCredentialBindings => Set<ExecutionEndpointCredentialBinding>();
    public DbSet<ExecutionEndpointMqttCredential> ExecutionEndpointMqttCredentials => Set<ExecutionEndpointMqttCredential>();
    public DbSet<ExecutionEndpointReadinessProjection> ExecutionEndpointReadinessProjections => Set<ExecutionEndpointReadinessProjection>();
    public DbSet<ExecutionEndpointCapabilityProjection> ExecutionEndpointCapabilityProjections => Set<ExecutionEndpointCapabilityProjection>();
    public DbSet<ExecutionEndpointRequestNonce> ExecutionEndpointRequestNonces => Set<ExecutionEndpointRequestNonce>();
    public DbSet<ExecutionEndpointReportedDevice> ExecutionEndpointReportedDevices => Set<ExecutionEndpointReportedDevice>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<OptionGroup> OptionGroups => Set<OptionGroup>();
    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();
    public DbSet<ProductOptionIngredientRequirement> ProductOptionIngredientRequirements => Set<ProductOptionIngredientRequirement>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<IngredientDispenserState> IngredientDispenserStates => Set<IngredientDispenserState>();
    public DbSet<KioskIngredientInventory> KioskIngredientInventories => Set<KioskIngredientInventory>();
    public DbSet<InventoryRefillTask> InventoryRefillTasks => Set<InventoryRefillTask>();
    public DbSet<InventoryRefillTaskTransition> InventoryRefillTaskTransitions => Set<InventoryRefillTaskTransition>();
    public DbSet<InventoryReconciliationCase> InventoryReconciliationCases => Set<InventoryReconciliationCase>();
    public DbSet<InventorySensorObservation> InventorySensorObservations => Set<InventorySensorObservation>();
    public DbSet<InventoryTopologyRebindRecord> InventoryTopologyRebindRecords => Set<InventoryTopologyRebindRecord>();
    public DbSet<InventoryTopologyChangeRecord> InventoryTopologyChangeRecords => Set<InventoryTopologyChangeRecord>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemProductOption> MenuItemProductOptions => Set<MenuItemProductOption>();
    public DbSet<KioskMenuItemAvailability> KioskMenuItemAvailabilities => Set<KioskMenuItemAvailability>();
    public DbSet<KioskMenuItemAvailabilityTransition> KioskMenuItemAvailabilityTransitions => Set<KioskMenuItemAvailabilityTransition>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemOption> OrderItemOptions => Set<OrderItemOption>();
    public DbSet<OrderItemOptionIngredientRequirement> OrderItemOptionIngredientRequirements => Set<OrderItemOptionIngredientRequirement>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<OrderItemStatusHistory> OrderItemStatusHistories => Set<OrderItemStatusHistory>();

    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PaymentCallback> PaymentCallbacks => Set<PaymentCallback>();
    public DbSet<PaymentProviderObservation> PaymentProviderObservations => Set<PaymentProviderObservation>();
    public DbSet<Refund> Refunds => Set<Refund>();

    public DbSet<RobotProgram> RobotPrograms => Set<RobotProgram>();
    public DbSet<RobotArtifact> RobotArtifacts => Set<RobotArtifact>();
    public DbSet<RobotArtifactTemplate> RobotArtifactTemplates => Set<RobotArtifactTemplate>();
    public DbSet<RobotArtifactTechnicalContract> RobotArtifactTechnicalContracts => Set<RobotArtifactTechnicalContract>();
    public DbSet<RobotArtifactDeclaredEffect> RobotArtifactDeclaredEffects => Set<RobotArtifactDeclaredEffect>();
    public DbSet<RobotArtifactOrderingConstraint> RobotArtifactOrderingConstraints => Set<RobotArtifactOrderingConstraint>();
    public DbSet<RobotProgramArtifact> RobotProgramArtifacts => Set<RobotProgramArtifact>();
    public DbSet<RobotAuthoringImport> RobotAuthoringImports => Set<RobotAuthoringImport>();
    public DbSet<RobotAuthoringImportItem> RobotAuthoringImportItems => Set<RobotAuthoringImportItem>();
    public DbSet<ProductionPackage> ProductionPackages => Set<ProductionPackage>();
    public DbSet<ProductionPackageVersion> ProductionPackageVersions => Set<ProductionPackageVersion>();
    public DbSet<ProductionPackageProductDefinition> ProductionPackageProductDefinitions => Set<ProductionPackageProductDefinition>();
    public DbSet<ProductionPackageArtifactDefinition> ProductionPackageArtifactDefinitions => Set<ProductionPackageArtifactDefinition>();
    public DbSet<ProductionPackageProgramBlueprint> ProductionPackageProgramBlueprints => Set<ProductionPackageProgramBlueprint>();
    public DbSet<ProductionPackageProgramSlot> ProductionPackageProgramSlots => Set<ProductionPackageProgramSlot>();
    public DbSet<ProductionPackageRouteBlueprint> ProductionPackageRouteBlueprints => Set<ProductionPackageRouteBlueprint>();
    public DbSet<ProductionPackageInstallation> ProductionPackageInstallations => Set<ProductionPackageInstallation>();
    public DbSet<ProductionPackageMaterialization> ProductionPackageMaterializations => Set<ProductionPackageMaterialization>();
    public DbSet<ProductionPackageUpgrade> ProductionPackageUpgrades => Set<ProductionPackageUpgrade>();
    public DbSet<ProductionPackageUpgradeMenuChange> ProductionPackageUpgradeMenuChanges => Set<ProductionPackageUpgradeMenuChange>();
    public DbSet<ProductionPackageUpgradeMenuOptionChange> ProductionPackageUpgradeMenuOptionChanges => Set<ProductionPackageUpgradeMenuOptionChange>();
    public DbSet<ProductionPackageUpgradeEndpointTarget> ProductionPackageUpgradeEndpointTargets => Set<ProductionPackageUpgradeEndpointTarget>();
    public DbSet<ProductionPackageUpgradeRollbackAttempt> ProductionPackageUpgradeRollbackAttempts => Set<ProductionPackageUpgradeRollbackAttempt>();
    public DbSet<ProductionPackageUpgradeCatalogIdentityChange> ProductionPackageUpgradeCatalogIdentityChanges => Set<ProductionPackageUpgradeCatalogIdentityChange>();
    public DbSet<ProductionPackageUpgradeAvailabilityChange> ProductionPackageUpgradeAvailabilityChanges => Set<ProductionPackageUpgradeAvailabilityChange>();
    public DbSet<ProductionComposition> ProductionCompositions => Set<ProductionComposition>();
    public DbSet<ConfigurationRelease> ConfigurationReleases => Set<ConfigurationRelease>();
    public DbSet<ProductionProgramBinding> ProductionProgramBindings => Set<ProductionProgramBinding>();
    public DbSet<ExecutionRoute> ExecutionRoutes => Set<ExecutionRoute>();
    public DbSet<ExecutionRouteRobotBinding> ExecutionRouteRobotBindings => Set<ExecutionRouteRobotBinding>();
    public DbSet<KioskConfigurationDeployment> KioskConfigurationDeployments => Set<KioskConfigurationDeployment>();
    public DbSet<ControllerArtifactSetDeployment> ControllerArtifactSetDeployments => Set<ControllerArtifactSetDeployment>();
    public DbSet<ControllerArtifactSetItem> ControllerArtifactSetItems => Set<ControllerArtifactSetItem>();
    public DbSet<OrderExecutionRecord> OrderExecutionRecords => Set<OrderExecutionRecord>();
    public DbSet<ProductionExecutionRecord> ProductionExecutionRecords => Set<ProductionExecutionRecord>();
    public DbSet<ProductionIncident> ProductionIncidents => Set<ProductionIncident>();
    public DbSet<ProductionIncidentHistory> ProductionIncidentHistories => Set<ProductionIncidentHistory>();

    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<SyncEventInbox> SyncEventInbox => Set<SyncEventInbox>();
    public DbSet<ProductionEventCheckpoint> ProductionEventCheckpoints => Set<ProductionEventCheckpoint>();
    public DbSet<EdgeStateSummary> EdgeStateSummaries => Set<EdgeStateSummary>();
    public DbSet<SyncDeadLetter> SyncDeadLetters => Set<SyncDeadLetter>();
    public DbSet<SyncDeadLetterRetryAttempt> SyncDeadLetterRetryAttempts => Set<SyncDeadLetterRetryAttempt>();
    public DbSet<EdgeCommand> EdgeCommands => Set<EdgeCommand>();
    public DbSet<EdgeCommandDeliveryAttempt> EdgeCommandDeliveryAttempts => Set<EdgeCommandDeliveryAttempt>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
        configurationBuilder.Properties<string>().HaveMaxLength(500);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IceBotDbContext).Assembly);
        ConfigureEntityConventions(modelBuilder);
    }

    private static void ConfigureEntityConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties().Where(property =>
                property.ClrType == typeof(string) && property.Name.EndsWith("Json", StringComparison.Ordinal)))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasColumnType("jsonb");
            }

            if (typeof(GuidEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Property(nameof(GuidEntity.Id)).ValueGeneratedNever();
            }

            if (typeof(LongEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Property(nameof(LongEntity.Id)).ValueGeneratedOnAdd();
            }

            if (typeof(IRobotSyncEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasIndex(nameof(IRobotSyncEntity.OriginNodeId), nameof(IRobotSyncEntity.Version));
            }

            if (typeof(IOrganizationScoped).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasIndex(nameof(IOrganizationScoped.OrganizationId));
            }

            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType) &&
                !ExplicitSoftDeleteVisibilityTypes.Contains(entityType.ClrType))
            {
                ApplySoftDeleteFilter(modelBuilder, entityType);
            }
        }

        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        var parameter = Expression.Parameter(entityType.ClrType, "entity");
        var deletedAt = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
        var body = Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTimeOffset?)));
        var lambda = Expression.Lambda(body, parameter);

        modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
    }
}

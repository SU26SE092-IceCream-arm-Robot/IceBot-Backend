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
using Domain.Common;
using Domain.Devices.Catalog;
using Domain.Identity.Entities;
using Domain.Inventory.Entities;
using Domain.Operations.Entities;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using Domain.ProductionExecution.Projections;
using Domain.ProductionConfiguration.Entities;
using Domain.RobotConfiguration.Artifacts;
using Domain.SalesCatalog.Entities;
using Domain.Sync.Entities;
using Domain.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using Domain.Devices.ExecutionEndpoints.Projections;

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
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Kiosk> Kiosks => Set<Kiosk>();

    public DbSet<DeviceType> DeviceTypes => Set<DeviceType>();
    public DbSet<DeviceModel> DeviceModels => Set<DeviceModel>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceEvent> DeviceEvents => Set<DeviceEvent>();
    public DbSet<KioskHeartbeat> KioskHeartbeats => Set<KioskHeartbeat>();
    public DbSet<KioskExecutionEndpoint> KioskExecutionEndpoints => Set<KioskExecutionEndpoint>();
    public DbSet<ExecutionEndpointCredentialBinding> ExecutionEndpointCredentialBindings => Set<ExecutionEndpointCredentialBinding>();
    public DbSet<ExecutionEndpointMqttCredential> ExecutionEndpointMqttCredentials => Set<ExecutionEndpointMqttCredential>();
    public DbSet<ExecutionEndpointReadinessProjection> ExecutionEndpointReadinessProjections => Set<ExecutionEndpointReadinessProjection>();
    public DbSet<ExecutionEndpointCapabilityProjection> ExecutionEndpointCapabilityProjections => Set<ExecutionEndpointCapabilityProjection>();
    public DbSet<ExecutionEndpointRequestNonce> ExecutionEndpointRequestNonces => Set<ExecutionEndpointRequestNonce>();
    public DbSet<ExecutionEndpointSupportedRobotTarget> ExecutionEndpointSupportedRobotTargets => Set<ExecutionEndpointSupportedRobotTarget>();

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
    public DbSet<InventoryTopologyRebindRecord> InventoryTopologyRebindRecords => Set<InventoryTopologyRebindRecord>();
    public DbSet<InventoryTopologyChangeRecord> InventoryTopologyChangeRecords => Set<InventoryTopologyChangeRecord>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemProductOption> MenuItemProductOptions => Set<MenuItemProductOption>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemOption> OrderItemOptions => Set<OrderItemOption>();
    public DbSet<OrderItemOptionIngredientRequirement> OrderItemOptionIngredientRequirements => Set<OrderItemOptionIngredientRequirement>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PaymentCallback> PaymentCallbacks => Set<PaymentCallback>();
    public DbSet<Refund> Refunds => Set<Refund>();

    public DbSet<RobotProgram> RobotPrograms => Set<RobotProgram>();
    public DbSet<RobotArtifact> RobotArtifacts => Set<RobotArtifact>();
    public DbSet<RobotArtifactTemplate> RobotArtifactTemplates => Set<RobotArtifactTemplate>();
    public DbSet<RobotProgramArtifact> RobotProgramArtifacts => Set<RobotProgramArtifact>();
    public DbSet<ConfigurationRelease> ConfigurationReleases => Set<ConfigurationRelease>();
    public DbSet<ExecutionRoute> ExecutionRoutes => Set<ExecutionRoute>();
    public DbSet<ExecutionRouteRobotBinding> ExecutionRouteRobotBindings => Set<ExecutionRouteRobotBinding>();
    public DbSet<KioskConfigurationDeployment> KioskConfigurationDeployments => Set<KioskConfigurationDeployment>();
    public DbSet<ControllerArtifactSetDeployment> ControllerArtifactSetDeployments => Set<ControllerArtifactSetDeployment>();
    public DbSet<ControllerArtifactSetItem> ControllerArtifactSetItems => Set<ControllerArtifactSetItem>();
    public DbSet<OrderExecutionRecord> OrderExecutionRecords => Set<OrderExecutionRecord>();
    public DbSet<ProductionExecutionRecord> ProductionExecutionRecords => Set<ProductionExecutionRecord>();

    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();
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

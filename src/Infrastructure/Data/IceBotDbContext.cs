using Domain.Catalog.Entities;
using Domain.Common;
using Domain.Devices.Entities;
using Domain.Identity.Entities;
using Domain.Identity.ValueObjects;
using Domain.Inventory.Entities;
using Domain.Operations.Entities;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using Domain.ProductionExecution.Projections;
using Domain.ProductionConfiguration.Entities;
using Domain.RobotConfiguration.Entities;
using Domain.SalesCatalog.Entities;
using Domain.Sync.Entities;
using Domain.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

namespace Infrastructure.Data;

public class IceBotDbContext : DbContext
{
    private const string ActiveRowFilter = "\"DeletedAt\" IS NULL";

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
    public DbSet<AccountDevice> AccountDevices => Set<AccountDevice>();
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
    public DbSet<ExecutionEndpointRequestNonce> ExecutionEndpointRequestNonces => Set<ExecutionEndpointRequestNonce>();
    public DbSet<ExecutionEndpointSupportedRobotTarget> ExecutionEndpointSupportedRobotTargets => Set<ExecutionEndpointSupportedRobotTarget>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<OptionGroup> OptionGroups => Set<OptionGroup>();
    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<IngredientDispenserState> IngredientDispenserStates => Set<IngredientDispenserState>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PaymentCallback> PaymentCallbacks => Set<PaymentCallback>();
    public DbSet<Refund> Refunds => Set<Refund>();

    public DbSet<RobotProgram> RobotPrograms => Set<RobotProgram>();
    public DbSet<RobotArtifact> RobotArtifacts => Set<RobotArtifact>();
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
    public DbSet<SyncDeadLetter> SyncDeadLetters => Set<SyncDeadLetter>();
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

        ConfigureIdentity(modelBuilder);
        ConfigureTopology(modelBuilder);
        ConfigureCatalog(modelBuilder);
        ConfigureSalesCatalog(modelBuilder);
        ConfigureOrdersAndPayments(modelBuilder);
        ConfigureRobot(modelBuilder);
        ConfigureProductionConfiguration(modelBuilder);
        ConfigureProductionExecution(modelBuilder);
        ConfigureOperations(modelBuilder);
        ConfigureSync(modelBuilder);
        ConfigureEntityConventions(modelBuilder);
    }

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Accounts");
            entity.HasIndex(x => x.UserName).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasIndex(x => x.Email).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasIndex(x => x.GoogleSubjectId).IsUnique().HasFilter(NotNullAndActive(nameof(Account.GoogleSubjectId)));
            entity.HasIndex(x => x.GoogleEmail).HasFilter("\"GoogleEmail\" IS NOT NULL");

            entity.Property(x => x.Password)
                .HasConversion(
                    password => password == null ? null : password.Value,
                    hash => string.IsNullOrWhiteSpace(hash) ? null : HashedPassword.From(hash))
                .HasColumnName("PasswordHash")
                .HasMaxLength(512);

            entity.HasMany(x => x.AccountRoles)
                .WithOne(x => x.Account)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Stores)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "AccountStores",
                    right => right.HasOne<Store>().WithMany().HasForeignKey("StoreId").OnDelete(DeleteBehavior.Restrict),
                    left => left.HasOne<Account>().WithMany().HasForeignKey("AccountId").OnDelete(DeleteBehavior.Restrict),
                    join =>
                    {
                        join.ToTable("AccountStores");
                        join.HasKey("AccountId", "StoreId");
                    });
        });

        modelBuilder.Entity<PasswordResetRequest>(entity =>
        {
            entity.ToTable("PasswordResetRequests");
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.AccountId, x.RequestedAt });
            entity.Property(x => x.TokenHash).HasMaxLength(128);
            entity.HasOne(x => x.Account)
                .WithMany(x => x.PasswordResetRequests)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccountInvitation>(entity =>
        {
            entity.ToTable("AccountInvitations");
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.AccountId, x.InvitedAt });
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Purpose).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Account)
                .WithMany(x => x.AccountInvitations)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccountRole>(entity =>
        {
            entity.ToTable("AccountRoles");
            entity.HasIndex(x => new { x.AccountId, x.RoleId, x.OrganizationId, x.StoreId, x.KioskId }).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId });
            entity.HasOne(x => x.Role)
                .WithMany(x => x.AccountRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Store)
                .WithMany()
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Kiosk)
                .WithMany()
                .HasForeignKey(x => x.KioskId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedByAccount)
                .WithMany()
                .HasForeignKey(x => x.AssignedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccountDevice>(entity =>
        {
            entity.ToTable("AccountDevices");
            entity.HasIndex(x => new { x.AccountId, x.DeviceTokenHash });
            entity.HasOne(x => x.Account)
                .WithMany(x => x.AccountDevices)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AccountDevice)
                .WithMany()
                .HasForeignKey(x => x.AccountDeviceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReplacedByToken)
                .WithMany()
                .HasForeignKey(x => x.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasIndex(x => x.Code).IsUnique();
        });
    }

    private static void ConfigureTopology(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations");
            entity.HasIndex(x => x.Code).IsUnique().HasFilter(ActiveRowFilter);
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.ToTable("Stores");
            entity.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasOne(x => x.Organization)
                .WithMany(x => x.Stores)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Kiosk>(entity =>
        {
            entity.ToTable("Kiosks");
            entity.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasIndex(x => x.SerialNumber).IsUnique().HasFilter(NotNullAndActive(nameof(Kiosk.SerialNumber)));
            entity.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Store)
                .WithMany(x => x.Kiosks)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCatalog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeviceType>(entity =>
        {
            entity.ToTable("DeviceTypes");
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<DeviceModel>(entity =>
        {
            entity.ToTable("DeviceModels");
            entity.HasIndex(x => new { x.DeviceTypeId, x.Code }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasOne(x => x.DeviceType)
                .WithMany(x => x.DeviceModels)
                .HasForeignKey(x => x.DeviceTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("Devices");
            entity.HasIndex(x => new { x.KioskId, x.Code }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasIndex(x => x.SerialNumber).IsUnique().HasFilter(NotNullAndActive(nameof(Device.SerialNumber)));
            entity.HasOne(x => x.DeviceType).WithMany().HasForeignKey(x => x.DeviceTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DeviceModel).WithMany().HasForeignKey(x => x.DeviceModelId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Kiosk).WithMany(x => x.Devices).HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KioskExecutionEndpoint>(entity =>
        {
            entity.ToTable("KioskExecutionEndpoints", table =>
                table.HasCheckConstraint(
                    "CK_KioskExecutionEndpoints_ProfileIdentity",
                    "((\"ExecutionProfile\" = 1 AND \"ControllerId\" IS NULL) OR (\"ExecutionProfile\" = 2 AND \"FullEdgeRuntimeId\" IS NULL)) AND (\"Status\" <> 2 OR ((\"ExecutionProfile\" = 1 AND \"FullEdgeRuntimeId\" IS NOT NULL) OR (\"ExecutionProfile\" = 2 AND \"ControllerId\" IS NOT NULL)))"));
            entity.HasIndex(x => new { x.KioskId, x.EndpointCode }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasIndex(x => new { x.Id, x.KioskId }).IsUnique();
            entity.HasIndex(x => new { x.Id, x.KioskId, x.FullEdgeRuntimeId }).IsUnique().HasFilter("\"FullEdgeRuntimeId\" IS NOT NULL");
            entity.HasIndex(x => x.FullEdgeRuntimeId).IsUnique().HasFilter("\"FullEdgeRuntimeId\" IS NOT NULL");
            entity.HasIndex(x => x.ControllerId).IsUnique().HasFilter("\"ControllerId\" IS NOT NULL");
            entity.HasIndex(x => x.CredentialBindingId).IsUnique().HasFilter("\"CredentialBindingId\" IS NOT NULL");
            entity.HasIndex(x => x.LastEdgeActivationEventId).IsUnique().HasFilter("\"LastEdgeActivationEventId\" IS NOT NULL");
            entity.HasIndex(x => x.LastControllerActivationReportId).IsUnique().HasFilter("\"LastControllerActivationReportId\" IS NOT NULL");
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CredentialBinding)
                .WithMany()
                .HasForeignKey(x => x.CredentialBindingId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.SupportedRobotTargets)
                .WithOne(x => x.KioskExecutionEndpoint)
                .HasForeignKey(x => x.KioskExecutionEndpointId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(x => x.SupportedRobotTargets).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ExecutionEndpointCredentialBinding>(entity =>
        {
            entity.ToTable("ExecutionEndpointCredentialBindings");
            entity.HasIndex(x => x.CredentialReference).IsUnique();
            entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.Status });
            entity.HasOne(x => x.KioskExecutionEndpoint)
                .WithMany()
                .HasForeignKey(x => x.KioskExecutionEndpointId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExecutionEndpointRequestNonce>(entity =>
        {
            entity.ToTable("ExecutionEndpointRequestNonces");
            entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.Nonce }).IsUnique();
            entity.HasIndex(x => x.ExpiresAt);
            entity.HasOne(x => x.KioskExecutionEndpoint)
                .WithMany()
                .HasForeignKey(x => x.KioskExecutionEndpointId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExecutionEndpointSupportedRobotTarget>(entity =>
        {
            entity.ToTable("ExecutionEndpointSupportedRobotTargets");
            entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.RuntimeTargetCode, x.MachineModelCode })
                .IsUnique()
                .HasFilter("\"DeviceId\" IS NULL");
            entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.RuntimeTargetCode, x.MachineModelCode, x.DeviceId })
                .IsUnique()
                .HasFilter("\"DeviceId\" IS NOT NULL");
            entity.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.ToTable("ProductCategories");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasOne(x => x.ParentCategory)
                .WithMany(x => x.ChildCategories)
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.Code }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TemplateProduct).WithMany().HasForeignKey(x => x.TemplateProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.ProductOptions)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "ProductProductOptions",
                    right => right.HasOne<ProductOption>().WithMany().HasForeignKey("ProductOptionId").OnDelete(DeleteBehavior.Restrict),
                    left => left.HasOne<Product>().WithMany().HasForeignKey("ProductId").OnDelete(DeleteBehavior.Restrict),
                    join =>
                    {
                        join.ToTable("ProductProductOptions");
                        join.HasKey("ProductId", "ProductOptionId");
                    });
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("ProductVariants");
            entity.HasIndex(x => new { x.ProductId, x.Code }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasIndex(x => new { x.ProductId, x.DisplayOrder });
            entity.HasOne(x => x.Product).WithMany(x => x.ProductVariants).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OptionGroup>(entity =>
        {
            entity.ToTable("OptionGroups");
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<ProductOption>(entity =>
        {
            entity.ToTable("ProductOptions");
            entity.HasIndex(x => new { x.OrganizationId, x.OptionGroupId, x.Code }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TemplateProductOption).WithMany().HasForeignKey(x => x.TemplateProductOptionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.OptionGroup).WithMany(x => x.ProductOptions).HasForeignKey(x => x.OptionGroupId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("Recipes");
            entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.ProductVariantId, x.Code, x.Version }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ProductVariant).WithMany(x => x.Recipes).HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TemplateRecipe).WithMany().HasForeignKey(x => x.TemplateRecipeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecipeItem>(entity =>
        {
            entity.ToTable("RecipeItems");
            entity.HasIndex(x => new { x.RecipeId, x.IngredientId, x.StepOrder }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasOne(x => x.Recipe).WithMany(x => x.RecipeItems).HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Ingredient).WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.ToTable("Ingredients");
            entity.HasIndex(x => x.Code).IsUnique().HasFilter(ActiveRowFilter);
        });

        modelBuilder.Entity<IngredientDispenserState>(entity =>
        {
            entity.ToTable("IngredientDispenserStates");
            entity.HasIndex(x => new { x.DeviceId, x.ContainerCode }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasOne(x => x.Device).WithMany(x => x.IngredientDispenserStates).HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Ingredient).WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("StockMovements");
            entity.HasIndex(x => x.SourceEventId).IsUnique().HasFilter("\"SourceEventId\" IS NOT NULL");
            entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.OccurredAt });
            entity.HasOne(x => x.CreatedByAccount).WithMany().HasForeignKey(x => x.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Ingredient).WithMany().HasForeignKey(x => x.IngredientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.IngredientDispenserState).WithMany().HasForeignKey(x => x.IngredientDispenserStateId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureOrdersAndPayments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasIndex(x => x.OrderNumber).IsUnique();
            entity.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
            entity.HasIndex(x => new { x.KioskId, x.ClientOrderId }).IsUnique().HasFilter("\"ClientOrderId\" IS NOT NULL");
            entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.PlacedAt });
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasIndex(x => new { x.OrderId, x.ClientLineId }).IsUnique().HasFilter(NotNullAndActive(nameof(OrderItem.ClientLineId)));
            entity.HasOne(x => x.Order).WithMany(x => x.OrderItems).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MenuItem).WithMany().HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.ProductOptions)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "OrderItemProductOptions",
                    right => right.HasOne<ProductOption>().WithMany().HasForeignKey("ProductOptionId").OnDelete(DeleteBehavior.Restrict),
                    left => left.HasOne<OrderItem>().WithMany().HasForeignKey("OrderItemId").OnDelete(DeleteBehavior.Restrict),
                    join =>
                    {
                        join.ToTable("OrderItemProductOptions");
                        join.HasKey("OrderItemId", "ProductOptionId");
                    });
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("OrderStatusHistories");
            entity.HasIndex(x => new { x.OrderId, x.ChangedAt });
            entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ChangedByAccount).WithMany().HasForeignKey(x => x.ChangedByAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("PaymentMethods");
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("PaymentTransactions");
            entity.HasIndex(x => x.TransactionNumber).IsUnique();
            entity.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
            entity.HasIndex(x => x.ProviderOrderCode).HasFilter("\"ProviderOrderCode\" IS NOT NULL");
            entity.HasIndex(x => x.ProviderTransactionId).HasFilter("\"ProviderTransactionId\" IS NOT NULL");
            entity.Property(x => x.ProviderOrderCode).HasMaxLength(100);
            entity.Property(x => x.ProviderPaymentLinkId).HasMaxLength(200);
            entity.Property(x => x.ProviderStatus).HasMaxLength(100);
            entity.Property(x => x.CheckoutUrl).HasMaxLength(2048);
            entity.Property(x => x.QrCodePayload).HasMaxLength(2048);
            entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PaymentMethod).WithMany().HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentCallback>(entity =>
        {
            entity.ToTable("PaymentCallbacks");
            entity.HasIndex(x => new { x.Provider, x.ProviderEventId }).IsUnique().HasFilter("\"ProviderEventId\" IS NOT NULL");
            entity.HasOne(x => x.PaymentTransaction).WithMany().HasForeignKey(x => x.PaymentTransactionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Refund>(entity =>
        {
            entity.ToTable("Refunds");
            entity.HasIndex(x => x.RefundNumber).IsUnique();
            entity.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
            entity.HasIndex(x => x.ProviderRefundId).HasFilter("\"ProviderRefundId\" IS NOT NULL");
            entity.HasOne(x => x.PaymentTransaction).WithMany().HasForeignKey(x => x.PaymentTransactionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RequestedByAccount).WithMany().HasForeignKey(x => x.RequestedByAccountId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSalesCatalog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Menu>(entity =>
        {
            entity.ToTable("Menus");
            entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.Code }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.Status });
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("MenuItems");
            entity.HasIndex(x => new { x.MenuId, x.Code }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasIndex(x => new { x.MenuId, x.Status, x.DisplayOrder });
            entity.HasOne(x => x.Menu).WithMany(x => x.MenuItems).HasForeignKey(x => x.MenuId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRobot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RobotProgram>(entity =>
        {
            entity.ToTable("RobotPrograms");
            entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.DeviceId, x.Code }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.ProgramManifestJson).HasColumnType("jsonb");
            entity.HasIndex(x => x.ProgramManifestChecksum).IsUnique().HasFilter("\"ProgramManifestChecksum\" IS NOT NULL");
            entity.Navigation(x => x.RobotProgramArtifacts).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<RobotArtifact>(entity =>
        {
            entity.ToTable("RobotArtifacts");
            entity.HasIndex(x => new { x.OrganizationId, x.ArtifactCode, x.Checksum }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasIndex(x => x.StorageKey).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasIndex(x => new { x.RuntimeTargetCode, x.MachineModelCode, x.Status });
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RobotProgramArtifact>(entity =>
        {
            entity.ToTable("RobotProgramArtifacts");
            entity.HasIndex(x => new { x.RobotProgramId, x.RunOrder }).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasIndex(x => x.RobotArtifactId);
            entity.HasOne(x => x.RobotProgram).WithMany(x => x.RobotProgramArtifacts).HasForeignKey(x => x.RobotProgramId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RobotArtifact).WithMany().HasForeignKey(x => x.RobotArtifactId).OnDelete(DeleteBehavior.Restrict);
        });

    }

    private static void ConfigureProductionConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfigurationRelease>(entity =>
        {
            entity.ToTable("ConfigurationReleases");
            entity.HasIndex(x => new { x.OrganizationId, x.ReleaseNumber })
                .IsUnique()
                .HasFilter(ActiveRowFilter);
            entity.HasIndex(x => new { x.Status, x.PublishedAt });
            entity.HasIndex(x => x.ReleaseChecksum).IsUnique().HasFilter("\"ReleaseChecksum\" IS NOT NULL");
            entity.Property(x => x.ManifestJson).HasColumnType("jsonb");
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(x => x.ExecutionRoutes).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ExecutionRoute>(entity =>
        {
            entity.ToTable("ExecutionRoutes");
            entity.HasIndex(x => new { x.ConfigurationReleaseId, x.RouteCode })
                .IsUnique()
                .HasFilter(ActiveRowFilter);
            entity.HasIndex(x => new { x.ConfigurationReleaseId, x.ProductVariantId, x.RecipeId, x.Priority });
            entity.Property(x => x.RequiredCapabilitiesJson).HasColumnType("jsonb");
            entity.HasOne(x => x.ConfigurationRelease)
                .WithMany(x => x.ExecutionRoutes)
                .HasForeignKey(x => x.ConfigurationReleaseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Recipe).WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(x => x.RobotBindings).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ExecutionRouteRobotBinding>(entity =>
        {
            entity.ToTable("ExecutionRouteRobotBindings");
            entity.HasIndex(x => new { x.ExecutionRouteId, x.BindingOrder })
                .IsUnique()
                .HasFilter(ActiveRowFilter);
            entity.HasIndex(x => x.RobotProgramId);
            entity.HasOne(x => x.ExecutionRoute)
                .WithMany(x => x.RobotBindings)
                .HasForeignKey(x => x.ExecutionRouteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RobotProgram).WithMany().HasForeignKey(x => x.RobotProgramId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KioskConfigurationDeployment>(entity =>
        {
            entity.ToTable("KioskConfigurationDeployments");
            entity.HasIndex(x => new { x.KioskId, x.ConfigurationReleaseId, x.AttemptNo }).IsUnique();
            entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.IdempotencyKey }).IsUnique();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(200);
            entity.HasIndex(x => new { x.KioskId, x.Status, x.RequestedAt });
            entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.Status });
            entity.HasIndex(x => x.KioskId).IsUnique().HasFilter("\"Status\" IN (1, 2)");
            entity.HasOne(x => x.ConfigurationRelease)
                .WithMany()
                .HasForeignKey(x => x.ConfigurationReleaseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.KioskExecutionEndpoint)
                .WithMany()
                .HasForeignKey(x => new { x.KioskExecutionEndpointId, x.KioskId, x.EdgeRuntimeId })
                .HasPrincipalKey(endpoint => new { endpoint.Id, endpoint.KioskId, endpoint.FullEdgeRuntimeId })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.RequestedByAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ControllerArtifactSetDeployment>(entity =>
        {
            entity.ToTable("ControllerArtifactSetDeployments");
            entity.HasIndex(x => new { x.ControllerId, x.ActiveSetVersion }).IsUnique();
            entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.IdempotencyKey }).IsUnique();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(200);
            entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.Status, x.RequestedAt });
            entity.HasIndex(x => new { x.ControllerId, x.LastControllerReportId }).IsUnique().HasFilter("\"LastControllerReportId\" IS NOT NULL");
            entity.HasOne(x => x.SourceConfigurationRelease).WithMany().HasForeignKey(x => x.SourceConfigurationReleaseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.KioskExecutionEndpoint).WithMany().HasForeignKey(x => x.KioskExecutionEndpointId).OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ControllerArtifactSetItem>(entity =>
        {
            entity.ToTable("ControllerArtifactSetItems");
            entity.HasIndex(x => new { x.ControllerArtifactSetDeploymentId, x.ExecutionRouteId, x.RobotProgramId, x.RunOrder, x.RobotArtifactId }).IsUnique();
            entity.Property(x => x.ParametersJson).HasColumnType("jsonb");
            entity.HasOne(x => x.ControllerArtifactSetDeployment).WithMany(x => x.Items).HasForeignKey(x => x.ControllerArtifactSetDeploymentId).OnDelete(DeleteBehavior.Cascade);
        });

    }

    private static void ConfigureProductionExecution(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderExecutionRecord>(entity =>
        {
            entity.ToTable("OrderExecutionRecords");
            entity.HasIndex(x => x.SourceCommandId).IsUnique();
            entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.SourceExecutorId, x.LastAppliedSourceEventId }).IsUnique();
            entity.HasIndex(x => new { x.OrderId, x.CloudReceivedAt });
            entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.Status, x.LastExecutorReportedAt });
            entity.HasOne(x => x.KioskExecutionEndpoint)
                .WithMany()
                .HasForeignKey(x => x.KioskExecutionEndpointId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SourceCommand)
                .WithMany()
                .HasForeignKey(x => x.SourceCommandId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SourceConfigurationRelease)
                .WithMany()
                .HasForeignKey(x => x.SourceConfigurationReleaseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductionExecutionRecord>(entity =>
        {
            entity.ToTable("ProductionExecutionRecords");
            entity.HasIndex(x => new { x.SourceCommandId, x.SourceProductionJobId }).IsUnique().HasFilter("\"SourceProductionJobId\" IS NOT NULL");
            entity.HasIndex(x => x.SourceCommandId).IsUnique().HasFilter("\"SourceProductionJobId\" IS NULL");
            entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.SourceExecutorId, x.LastAppliedSourceEventId }).IsUnique();
            entity.HasIndex(x => new { x.KioskExecutionEndpointId, x.Status, x.LastExecutorReportedAt });
            entity.HasOne(x => x.KioskExecutionEndpoint)
                .WithMany()
                .HasForeignKey(x => x.KioskExecutionEndpointId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SourceCommand)
                .WithMany()
                .HasForeignKey(x => x.SourceCommandId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureOperations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeviceEvent>(entity =>
        {
            entity.ToTable("DeviceEvents");
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.HasIndex(x => new { x.DeviceId, x.OccurredAt });
            entity.HasIndex(x => new { x.KioskId, x.OccurredAt });
            entity.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KioskHeartbeat>(entity =>
        {
            entity.ToTable("KioskHeartbeats");
            entity.HasIndex(x => new { x.KioskId, x.NodeId, x.HeartbeatSequence }).IsUnique().HasFilter("\"HeartbeatSequence\" IS NOT NULL");
            entity.HasIndex(x => new { x.KioskId, x.ReportedAt });
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.ToTable("Alerts");
            entity.HasIndex(x => new { x.KioskId, x.Status, x.RaisedAt });
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AcknowledgedByAccount).WithMany().HasForeignKey(x => x.AcknowledgedByAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MaintenanceTicket>(entity =>
        {
            entity.ToTable("MaintenanceTickets");
            entity.HasIndex(x => x.TicketNumber).IsUnique().HasFilter(ActiveRowFilter);
            entity.HasIndex(x => new { x.OrganizationId, x.StoreId, x.KioskId, x.Status, x.ReportedAt });
            entity.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DeviceEvent).WithMany().HasForeignKey(x => x.DeviceEventId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedToAccount).WithMany().HasForeignKey(x => x.AssignedToAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByAccount).WithMany().HasForeignKey(x => x.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OperationLog>(entity =>
        {
            entity.ToTable("OperationLogs");
            entity.HasIndex(x => new { x.KioskId, x.OccurredAt });
            entity.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSync(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncEventInbox>(entity =>
        {
            entity.ToTable("SyncEventInbox");
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.HasIndex(x => new { x.SourceNodeId, x.EventType, x.OccurredAt });
            entity.HasIndex(x => new { x.Status, x.NextRetryAt, x.LockedUntil });
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SyncDeadLetter>(entity =>
        {
            entity.ToTable("SyncDeadLetters");
            entity.HasIndex(x => new { x.Status, x.FailedAt });
            entity.HasIndex(x => x.EventId).HasFilter("\"EventId\" IS NOT NULL");
            entity.HasOne(x => x.SyncEventInbox).WithMany().HasForeignKey(x => x.SyncEventInboxId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Kiosk).WithMany().HasForeignKey(x => x.KioskId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ResolvedByAccount).WithMany().HasForeignKey(x => x.ResolvedByAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EdgeCommand>(entity =>
        {
            entity.ToTable("EdgeCommands");
            entity.HasIndex(x => new { x.TargetExecutionEndpointId, x.Status, x.CreatedAt });
            entity.HasIndex(x => x.DeploymentId).IsUnique().HasFilter("\"DeploymentId\" IS NOT NULL");
            entity.HasIndex(x => new { x.OrderId, x.DispatchAttemptNo }).IsUnique().HasFilter("\"OrderId\" IS NOT NULL AND \"DispatchAttemptNo\" IS NOT NULL");
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb");
            entity.Navigation(x => x.DeliveryAttempts).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasOne(x => x.TargetExecutionEndpoint)
                .WithMany()
                .HasForeignKey(x => new { x.TargetExecutionEndpointId, x.KioskId })
                .HasPrincipalKey(endpoint => new { endpoint.Id, endpoint.KioskId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EdgeCommandDeliveryAttempt>(entity =>
        {
            entity.ToTable("EdgeCommandDeliveryAttempts");
            entity.HasIndex(x => new { x.EdgeCommandId, x.DeliveryAttemptNo }).IsUnique();
            entity.HasOne(x => x.EdgeCommand).WithMany(x => x.DeliveryAttempts).HasForeignKey(x => x.EdgeCommandId).OnDelete(DeleteBehavior.Restrict);
        });
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

            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
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

    private static string NotNullAndActive(string columnName)
    {
        return $"\"{columnName}\" IS NOT NULL AND {ActiveRowFilter}";
    }
}

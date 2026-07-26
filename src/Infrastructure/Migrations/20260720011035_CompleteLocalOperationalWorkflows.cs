using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteLocalOperationalWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CompleteLocalOperationalWorkflowsManualSteps.EnsureUniqueProviderPaymentIdentity(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_ProviderOrderCode",
                table: "PaymentTransactions");

            migrationBuilder.AddColumn<string>(
                name: "MaterializationIdentitySuffix",
                table: "ProductionPackageInstallations",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AlertId",
                table: "MaintenanceTickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FranchiseOnboardings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    RequestJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    PackageInstallationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReadyAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FranchiseOnboardings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FranchiseOnboardings_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FranchiseOnboardings_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FranchiseOnboardings_ProductionPackageInstallations_Package~",
                        column: x => x.PackageInstallationId,
                        principalTable: "ProductionPackageInstallations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FranchiseOnboardings_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecipientAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DataJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessingStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_Accounts_RecipientAccountId",
                        column: x => x.RecipientAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_Kiosks_KioskId_OrganizationId",
                        columns: x => new { x.KioskId, x.OrganizationId },
                        principalTable: "Kiosks",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_Stores_StoreId_OrganizationId",
                        columns: x => new { x.StoreId, x.OrganizationId },
                        principalTable: "Stores",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageUpgrades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceInstallationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetPackageVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetInstallationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PreviewChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceManifestChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TargetManifestChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SelectedProductSourceKeysJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApprovedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RollbackRequestedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RollbackRequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RolledBackByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RolledBackAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackageUpgrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionPackageUpgrades_ProductionPackageInstallations_So~",
                        column: x => x.SourceInstallationId,
                        principalTable: "ProductionPackageInstallations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPackageUpgrades_ProductionPackageInstallations_Ta~",
                        column: x => x.TargetInstallationId,
                        principalTable: "ProductionPackageInstallations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPackageUpgrades_ProductionPackageVersions_TargetP~",
                        column: x => x.TargetPackageVersionId,
                        principalTable: "ProductionPackageVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotAuthoringImports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientExportId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProposedProgramCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProposedProgramName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RuntimeTargetCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MachineModelCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StagingStorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ValidationReportJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    AppliedRobotProgramId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedConfigurationReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ComposedRecipeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ComposedOptionCodesJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CompositionPreviewChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleaseLinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompositionConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DiscardedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotAuthoringImports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotAuthoringImports_ConfigurationReleases_LinkedConfigura~",
                        column: x => x.LinkedConfigurationReleaseId,
                        principalTable: "ConfigurationReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotAuthoringImports_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotAuthoringImports_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotAuthoringImports_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotAuthoringImports_Recipes_ComposedRecipeId",
                        column: x => x.ComposedRecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotAuthoringImports_RobotPrograms_AppliedRobotProgramId",
                        column: x => x.AppliedRobotProgramId,
                        principalTable: "RobotPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotAuthoringImports_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageUpgradeAvailabilityChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UpgradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceKind = table.Column<int>(type: "integer", nullable: false),
                    ResourceSourceKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SourceResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAvailabilityBefore = table.Column<bool>(type: "boolean", nullable: false),
                    TargetAvailabilityBefore = table.Column<bool>(type: "boolean", nullable: false),
                    TargetAvailabilityAfter = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackageUpgradeAvailabilityChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionPackageUpgradeAvailabilityChanges_ProductionPacka~",
                        column: x => x.UpgradeId,
                        principalTable: "ProductionPackageUpgrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageUpgradeCatalogIdentityChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UpgradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductSourceKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCodeBefore = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceCodeAfter = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetCodeBefore = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetCodeAfter = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BeforeChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AfterChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackageUpgradeCatalogIdentityChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionPackageUpgradeCatalogIdentityChanges_ProductionPa~",
                        column: x => x.UpgradeId,
                        principalTable: "ProductionPackageUpgrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPackageUpgradeCatalogIdentityChanges_Products_Sou~",
                        column: x => x.SourceProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPackageUpgradeCatalogIdentityChanges_Products_Tar~",
                        column: x => x.TargetProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageUpgradeEndpointTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UpgradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceConfigurationReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDeploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDeploymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RollbackDeploymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackageUpgradeEndpointTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionPackageUpgradeEndpointTargets_KioskExecutionEndpo~",
                        column: x => x.KioskExecutionEndpointId,
                        principalTable: "KioskExecutionEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPackageUpgradeEndpointTargets_ProductionPackageUp~",
                        column: x => x.UpgradeId,
                        principalTable: "ProductionPackageUpgrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageUpgradeMenuChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UpgradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeKind = table.Column<int>(type: "integer", nullable: false),
                    MenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    BeforeProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    AfterProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    BeforeProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AfterProductVariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    BeforeRecipeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AfterRecipeId = table.Column<Guid>(type: "uuid", nullable: true),
                    BeforeMenuItemStatus = table.Column<int>(type: "integer", nullable: false),
                    AfterMenuItemStatus = table.Column<int>(type: "integer", nullable: false),
                    BeforeBindingChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AfterBindingChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackageUpgradeMenuChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionPackageUpgradeMenuChanges_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPackageUpgradeMenuChanges_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPackageUpgradeMenuChanges_ProductionPackageUpgrad~",
                        column: x => x.UpgradeId,
                        principalTable: "ProductionPackageUpgrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotAuthoringImportItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotAuthoringImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SidecarFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RunOrder = table.Column<int>(type: "integer", nullable: false),
                    LuaChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SidecarChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RobotArtifactId = table.Column<Guid>(type: "uuid", nullable: true),
                    TechnicalContractId = table.Column<Guid>(type: "uuid", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotAuthoringImportItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotAuthoringImportItems_RobotArtifactTechnicalContracts_T~",
                        column: x => x.TechnicalContractId,
                        principalTable: "RobotArtifactTechnicalContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotAuthoringImportItems_RobotArtifacts_RobotArtifactId",
                        column: x => x.RobotArtifactId,
                        principalTable: "RobotArtifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotAuthoringImportItems_RobotAuthoringImports_RobotAuthor~",
                        column: x => x.RobotAuthoringImportId,
                        principalTable: "RobotAuthoringImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageUpgradeRollbackAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UpgradeEndpointTargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNo = table.Column<int>(type: "integer", nullable: false),
                    DeploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplacedDeploymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackageUpgradeRollbackAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionPackageUpgradeRollbackAttempts_ProductionPackageU~",
                        column: x => x.UpgradeEndpointTargetId,
                        principalTable: "ProductionPackageUpgradeEndpointTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageUpgradeMenuOptionChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UpgradeMenuChangeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionSourceKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    BeforeProductOptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AfterProductOptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackageUpgradeMenuOptionChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionPackageUpgradeMenuOptionChanges_ProductionPackage~",
                        column: x => x.UpgradeMenuChangeId,
                        principalTable: "ProductionPackageUpgradeMenuChanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageMaterializations_ResourceKind_TargetKey",
                table: "ProductionPackageMaterializations",
                columns: new[] { "ResourceKind", "TargetKey" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Provider_ProviderOrderCode",
                table: "PaymentTransactions",
                columns: new[] { "Provider", "ProviderOrderCode" },
                unique: true,
                filter: "\"ProviderOrderCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_AlertId",
                table: "MaintenanceTickets",
                column: "AlertId",
                unique: true,
                filter: "\"AlertId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FranchiseOnboardings_KioskId",
                table: "FranchiseOnboardings",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_FranchiseOnboardings_OrganizationId",
                table: "FranchiseOnboardings",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_FranchiseOnboardings_OrganizationId_IdempotencyKey",
                table: "FranchiseOnboardings",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FranchiseOnboardings_OrganizationId_Status_UpdatedAt",
                table: "FranchiseOnboardings",
                columns: new[] { "OrganizationId", "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FranchiseOnboardings_PackageInstallationId",
                table: "FranchiseOnboardings",
                column: "PackageInstallationId",
                unique: true,
                filter: "\"PackageInstallationId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FranchiseOnboardings_StoreId",
                table: "FranchiseOnboardings",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_DeliveryKey",
                table: "NotificationDeliveries",
                column: "DeliveryKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_KioskId_OrganizationId",
                table: "NotificationDeliveries",
                columns: new[] { "KioskId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_NotificationType_SubjectId",
                table: "NotificationDeliveries",
                columns: new[] { "NotificationType", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_OrganizationId_Status_NextAttemptAt",
                table: "NotificationDeliveries",
                columns: new[] { "OrganizationId", "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_RecipientAccountId",
                table: "NotificationDeliveries",
                column: "RecipientAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_StoreId_OrganizationId",
                table: "NotificationDeliveries",
                columns: new[] { "StoreId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgradeAvailabilityChanges_UpgradeId_Resou~",
                table: "ProductionPackageUpgradeAvailabilityChanges",
                columns: new[] { "UpgradeId", "ResourceKind", "SourceResourceId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgradeCatalogIdentityChanges_SourceProduc~",
                table: "ProductionPackageUpgradeCatalogIdentityChanges",
                column: "SourceProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgradeCatalogIdentityChanges_TargetProduc~",
                table: "ProductionPackageUpgradeCatalogIdentityChanges",
                column: "TargetProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgradeCatalogIdentityChanges_UpgradeId_Pr~",
                table: "ProductionPackageUpgradeCatalogIdentityChanges",
                columns: new[] { "UpgradeId", "ProductSourceKey" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgradeEndpointTargets_KioskExecutionEndpo~",
                table: "ProductionPackageUpgradeEndpointTargets",
                column: "KioskExecutionEndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgradeEndpointTargets_UpgradeId_KioskExec~",
                table: "ProductionPackageUpgradeEndpointTargets",
                columns: new[] { "UpgradeId", "KioskExecutionEndpointId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgradeMenuChanges_MenuId",
                table: "ProductionPackageUpgradeMenuChanges",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgradeMenuChanges_MenuItemId",
                table: "ProductionPackageUpgradeMenuChanges",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgradeMenuChanges_UpgradeId_MenuItemId",
                table: "ProductionPackageUpgradeMenuChanges",
                columns: new[] { "UpgradeId", "MenuItemId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgradeMenuOptionChanges_UpgradeMenuChange~",
                table: "ProductionPackageUpgradeMenuOptionChanges",
                columns: new[] { "UpgradeMenuChangeId", "OptionSourceKey" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgradeRollbackAttempts_DeploymentId",
                table: "ProductionPackageUpgradeRollbackAttempts",
                column: "DeploymentId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgradeRollbackAttempts_UpgradeEndpointTar~",
                table: "ProductionPackageUpgradeRollbackAttempts",
                columns: new[] { "UpgradeEndpointTargetId", "AttemptNo" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgrades_OrganizationId_IdempotencyKey",
                table: "ProductionPackageUpgrades",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgrades_OrganizationId_SourceInstallatio~1",
                table: "ProductionPackageUpgrades",
                columns: new[] { "OrganizationId", "SourceInstallationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgrades_OrganizationId_SourceInstallation~",
                table: "ProductionPackageUpgrades",
                columns: new[] { "OrganizationId", "SourceInstallationId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"Status\" IN (0, 1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgrades_SourceInstallationId",
                table: "ProductionPackageUpgrades",
                column: "SourceInstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgrades_TargetInstallationId",
                table: "ProductionPackageUpgrades",
                column: "TargetInstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageUpgrades_TargetPackageVersionId",
                table: "ProductionPackageUpgrades",
                column: "TargetPackageVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotAuthoringImportItems_RobotArtifactId",
                table: "RobotAuthoringImportItems",
                column: "RobotArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotAuthoringImportItems_RobotAuthoringImportId_ArtifactCo~",
                table: "RobotAuthoringImportItems",
                columns: new[] { "RobotAuthoringImportId", "ArtifactCode" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotAuthoringImportItems_RobotAuthoringImportId_RunOrder",
                table: "RobotAuthoringImportItems",
                columns: new[] { "RobotAuthoringImportId", "RunOrder" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotAuthoringImportItems_TechnicalContractId",
                table: "RobotAuthoringImportItems",
                column: "TechnicalContractId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotAuthoringImports_AppliedRobotProgramId",
                table: "RobotAuthoringImports",
                column: "AppliedRobotProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotAuthoringImports_ComposedRecipeId",
                table: "RobotAuthoringImports",
                column: "ComposedRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotAuthoringImports_DeviceId",
                table: "RobotAuthoringImports",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotAuthoringImports_KioskId",
                table: "RobotAuthoringImports",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotAuthoringImports_LinkedConfigurationReleaseId",
                table: "RobotAuthoringImports",
                column: "LinkedConfigurationReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotAuthoringImports_OrganizationId_ClientExportId_ImportC~",
                table: "RobotAuthoringImports",
                columns: new[] { "OrganizationId", "ClientExportId", "ImportChecksum" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotAuthoringImports_OrganizationId_IdempotencyKey",
                table: "RobotAuthoringImports",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotAuthoringImports_Status_CreatedAt",
                table: "RobotAuthoringImports",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotAuthoringImports_StoreId",
                table: "RobotAuthoringImports",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceTickets_Alerts_AlertId",
                table: "MaintenanceTickets",
                column: "AlertId",
                principalTable: "Alerts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceTickets_Alerts_AlertId",
                table: "MaintenanceTickets");

            migrationBuilder.DropTable(
                name: "FranchiseOnboardings");

            migrationBuilder.DropTable(
                name: "NotificationDeliveries");

            migrationBuilder.DropTable(
                name: "ProductionPackageUpgradeAvailabilityChanges");

            migrationBuilder.DropTable(
                name: "ProductionPackageUpgradeCatalogIdentityChanges");

            migrationBuilder.DropTable(
                name: "ProductionPackageUpgradeMenuOptionChanges");

            migrationBuilder.DropTable(
                name: "ProductionPackageUpgradeRollbackAttempts");

            migrationBuilder.DropTable(
                name: "RobotAuthoringImportItems");

            migrationBuilder.DropTable(
                name: "ProductionPackageUpgradeMenuChanges");

            migrationBuilder.DropTable(
                name: "ProductionPackageUpgradeEndpointTargets");

            migrationBuilder.DropTable(
                name: "RobotAuthoringImports");

            migrationBuilder.DropTable(
                name: "ProductionPackageUpgrades");

            migrationBuilder.DropIndex(
                name: "IX_ProductionPackageMaterializations_ResourceKind_TargetKey",
                table: "ProductionPackageMaterializations");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_Provider_ProviderOrderCode",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_AlertId",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "MaterializationIdentitySuffix",
                table: "ProductionPackageInstallations");

            migrationBuilder.DropColumn(
                name: "AlertId",
                table: "MaintenanceTickets");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ProviderOrderCode",
                table: "PaymentTransactions",
                column: "ProviderOrderCode",
                filter: "\"ProviderOrderCode\" IS NOT NULL");
        }
    }
}

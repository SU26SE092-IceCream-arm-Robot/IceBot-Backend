using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantFulfillmentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_RobotJobs_RobotJobId",
                table: "Alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceEvents_RobotJobs_RobotJobId",
                table: "DeviceEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_OperationLogs_RobotJobs_RobotJobId",
                table: "OperationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_RobotPrograms_Accounts_PointValidatedByAccountId",
                table: "RobotPrograms");

            migrationBuilder.DropForeignKey(
                name: "FK_RobotPrograms_RobotPrograms_TemplateProgramId",
                table: "RobotPrograms");

            migrationBuilder.DropTable(
                name: "KioskRecipeExecutionProfiles");

            migrationBuilder.DropTable(
                name: "RobotJobEvents");

            migrationBuilder.DropTable(
                name: "RobotJobSteps");

            migrationBuilder.DropTable(
                name: "RobotJobs");

            migrationBuilder.DropTable(
                name: "RobotProgramSteps");

            migrationBuilder.DropIndex(
                name: "IX_RobotPrograms_OrganizationId_StoreId_KioskId_DeviceId_Code_~",
                table: "RobotPrograms");

            migrationBuilder.DropIndex(
                name: "IX_RobotPrograms_PointValidatedByAccountId",
                table: "RobotPrograms");

            migrationBuilder.DropIndex(
                name: "IX_RobotPrograms_TemplateProgramId",
                table: "RobotPrograms");

            migrationBuilder.DropIndex(
                name: "IX_OperationLogs_RobotJobId",
                table: "OperationLogs");

            migrationBuilder.DropIndex(
                name: "IX_DeviceEvents_RobotJobId",
                table: "DeviceEvents");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_RobotJobId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationSeconds",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "PointSnapshotJson",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "PointSnapshotSchemaVersion",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "PointStatus",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "PointValidatedAt",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "PointValidatedByAccountId",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "ProgramPayloadJson",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "ProgramPayloadSchemaVersion",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "ProgramVersion",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "SupportedDeviceTypeId",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "TemplateProgramId",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "Vendor",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "VendorProgramId",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "RobotJobId",
                table: "OperationLogs");

            migrationBuilder.DropColumn(
                name: "RobotJobId",
                table: "DeviceEvents");

            migrationBuilder.DropColumn(
                name: "RobotJobId",
                table: "Alerts");

            migrationBuilder.RenameColumn(
                name: "VendorProgramVersion",
                table: "RobotPrograms",
                newName: "ProgramManifestChecksum");

            migrationBuilder.RenameColumn(
                name: "SafetyZoneSchemaVersion",
                table: "RobotPrograms",
                newName: "ProgramManifestSchemaVersion");

            migrationBuilder.RenameColumn(
                name: "SafetyZoneJson",
                table: "RobotPrograms",
                newName: "ProgramManifestJson");

            migrationBuilder.AddColumn<int>(
                name: "FulfillmentType",
                table: "ProductVariants",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "ConfigurationReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseNumber = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReleaseManifestSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    ManifestJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    ReleaseChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RetiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationReleases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurationReleases_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ArtifactName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Checksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RuntimeTargetCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MachineModelCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentLengthBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotArtifacts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RequiredCapabilitiesJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionRoutes_ConfigurationReleases_ConfigurationReleaseId",
                        column: x => x.ConfigurationReleaseId,
                        principalTable: "ConfigurationReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionRoutes_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionRoutes_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotProgramArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunOrder = table.Column<int>(type: "integer", nullable: false),
                    ParametersSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotProgramArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotProgramArtifacts_RobotArtifacts_RobotArtifactId",
                        column: x => x.RobotArtifactId,
                        principalTable: "RobotArtifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotProgramArtifacts_RobotPrograms_RobotProgramId",
                        column: x => x.RobotProgramId,
                        principalTable: "RobotPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionRouteRobotBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionRouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    BindingOrder = table.Column<int>(type: "integer", nullable: false),
                    RequiredWorkcellCapabilityCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RobotProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionRouteRobotBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionRouteRobotBindings_ExecutionRoutes_ExecutionRouteId",
                        column: x => x.ExecutionRouteId,
                        principalTable: "ExecutionRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionRouteRobotBindings_RobotPrograms_RobotProgramId",
                        column: x => x.RobotProgramId,
                        principalTable: "RobotPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ControllerArtifactSetDeployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    ControllerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceConfigurationReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ActiveSetVersion = table.Column<long>(type: "bigint", nullable: false),
                    ActiveSetChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MaxArtifactCount = table.Column<int>(type: "integer", nullable: false),
                    MaxArtifactStorageBytes = table.Column<long>(type: "bigint", nullable: false),
                    RequestedArtifactCount = table.Column<int>(type: "integer", nullable: false),
                    RequestedArtifactStorageBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ControllerReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastControllerReportId = table.Column<Guid>(type: "uuid", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControllerArtifactSetDeployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControllerArtifactSetDeployments_ConfigurationReleases_Sour~",
                        column: x => x.SourceConfigurationReleaseId,
                        principalTable: "ConfigurationReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ControllerArtifactSetItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ControllerArtifactSetDeploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionRouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotProgramManifestChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RobotArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RuntimeTargetCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MachineModelCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContentLengthBytes = table.Column<long>(type: "bigint", nullable: false),
                    RunOrder = table.Column<int>(type: "integer", nullable: false),
                    ParametersSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControllerArtifactSetItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControllerArtifactSetItems_ControllerArtifactSetDeployments~",
                        column: x => x.ControllerArtifactSetDeploymentId,
                        principalTable: "ControllerArtifactSetDeployments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EdgeCommandDeliveryAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EdgeCommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryAttemptNo = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    ResponseCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResponseMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EdgeCommandDeliveryAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EdgeCommands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandType = table.Column<int>(type: "integer", nullable: false),
                    DispatchAttemptNo = table.Column<int>(type: "integer", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    CommandExpiryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectionCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RejectionMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RespondedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EdgeCommands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionEndpointCredentialBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthenticationMode = table.Column<int>(type: "integer", nullable: false),
                    CredentialReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProvisionedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionEndpointCredentialBindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KioskExecutionEndpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndpointCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExecutionProfile = table.Column<int>(type: "integer", nullable: false),
                    AuthenticationMode = table.Column<int>(type: "integer", nullable: false),
                    CredentialBindingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FullEdgeRuntimeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ControllerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProvisionedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActiveConfigurationDeploymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveConfigurationReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveConfigurationReleaseChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastEdgeActivationEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveConfigurationEdgeReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActiveConfigurationCloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActiveArtifactSetDeploymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveArtifactSetReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveArtifactSetReleaseChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ActiveArtifactSetVersion = table.Column<long>(type: "bigint", nullable: true),
                    ActiveArtifactSetChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastControllerActivationReportId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveArtifactSetControllerReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActiveArtifactSetCloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KioskExecutionEndpoints", x => x.Id);
                    table.UniqueConstraint("AK_KioskExecutionEndpoints_Id_KioskId", x => new { x.Id, x.KioskId });
                    table.UniqueConstraint("AK_KioskExecutionEndpoints_Id_KioskId_FullEdgeRuntimeId", x => new { x.Id, x.KioskId, x.FullEdgeRuntimeId });
                    table.CheckConstraint("CK_KioskExecutionEndpoints_ProfileIdentity", "((\"ExecutionProfile\" = 1 AND \"ControllerId\" IS NULL) OR (\"ExecutionProfile\" = 2 AND \"FullEdgeRuntimeId\" IS NULL)) AND (\"Status\" <> 2 OR ((\"ExecutionProfile\" = 1 AND \"FullEdgeRuntimeId\" IS NOT NULL) OR (\"ExecutionProfile\" = 2 AND \"ControllerId\" IS NOT NULL)))");
                    table.ForeignKey(
                        name: "FK_KioskExecutionEndpoints_ExecutionEndpointCredentialBindings~",
                        column: x => x.CredentialBindingId,
                        principalTable: "ExecutionEndpointCredentialBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskExecutionEndpoints_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionEndpointSupportedRobotTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuntimeTargetCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MachineModelCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionEndpointSupportedRobotTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointSupportedRobotTargets_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~",
                        column: x => x.KioskExecutionEndpointId,
                        principalTable: "KioskExecutionEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KioskConfigurationDeployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    EdgeRuntimeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AttemptNo = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    EdgeReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastEdgeDeploymentEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KioskConfigurationDeployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KioskConfigurationDeployments_Accounts_RequestedByAccountId",
                        column: x => x.RequestedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskConfigurationDeployments_ConfigurationReleases_Configu~",
                        column: x => x.ConfigurationReleaseId,
                        principalTable: "ConfigurationReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskConfigurationDeployments_KioskExecutionEndpoints_Kiosk~",
                        columns: x => new { x.KioskExecutionEndpointId, x.KioskId, x.EdgeRuntimeId },
                        principalTable: "KioskExecutionEndpoints",
                        principalColumns: new[] { "Id", "KioskId", "FullEdgeRuntimeId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderExecutionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchAttemptNo = table.Column<int>(type: "integer", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionProfile = table.Column<int>(type: "integer", nullable: false),
                    SourceConfigurationReleaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleaseChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ObservationStatus = table.Column<int>(type: "integer", nullable: false),
                    CustomerExecutionStatus = table.Column<int>(type: "integer", nullable: false),
                    SourceExecutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastAppliedSourceEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastAppliedSequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    LastEdgeCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastExecutorReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderExecutionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderExecutionRecords_ConfigurationReleases_SourceConfigura~",
                        column: x => x.SourceConfigurationReleaseId,
                        principalTable: "ConfigurationReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderExecutionRecords_EdgeCommands_SourceCommandId",
                        column: x => x.SourceCommandId,
                        principalTable: "EdgeCommands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderExecutionRecords_KioskExecutionEndpoints_KioskExecutio~",
                        column: x => x.KioskExecutionEndpointId,
                        principalTable: "KioskExecutionEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionExecutionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionProfile = table.Column<int>(type: "integer", nullable: false),
                    SourceProductionJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkcellId = table.Column<Guid>(type: "uuid", nullable: true),
                    ControllerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExecutionPlanChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ActiveSetVersion = table.Column<long>(type: "bigint", nullable: true),
                    ActiveSetChecksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PhysicalOutputState = table.Column<int>(type: "integer", nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SourceExecutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastAppliedSourceEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastAppliedSequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    LastEdgeCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastExecutorReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionExecutionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionExecutionRecords_EdgeCommands_SourceCommandId",
                        column: x => x.SourceCommandId,
                        principalTable: "EdgeCommands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionExecutionRecords_KioskExecutionEndpoints_KioskExe~",
                        column: x => x.KioskExecutionEndpointId,
                        principalTable: "KioskExecutionEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RobotPrograms_OrganizationId_StoreId_KioskId_DeviceId_Code",
                table: "RobotPrograms",
                columns: new[] { "OrganizationId", "StoreId", "KioskId", "DeviceId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotPrograms_ProgramManifestChecksum",
                table: "RobotPrograms",
                column: "ProgramManifestChecksum",
                unique: true,
                filter: "\"ProgramManifestChecksum\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationReleases_OrganizationId_ReleaseNumber",
                table: "ConfigurationReleases",
                columns: new[] { "OrganizationId", "ReleaseNumber" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationReleases_ReleaseChecksum",
                table: "ConfigurationReleases",
                column: "ReleaseChecksum",
                unique: true,
                filter: "\"ReleaseChecksum\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationReleases_Status_PublishedAt",
                table: "ConfigurationReleases",
                columns: new[] { "Status", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_ControllerId_ActiveSetVers~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "ControllerId", "ActiveSetVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_ControllerId_LastControlle~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "ControllerId", "LastControllerReportId" },
                unique: true,
                filter: "\"LastControllerReportId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_KioskExecutionEndpointId_S~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "KioskExecutionEndpointId", "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_SourceConfigurationRelease~",
                table: "ControllerArtifactSetDeployments",
                column: "SourceConfigurationReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetItems_ControllerArtifactSetDeploymentI~",
                table: "ControllerArtifactSetItems",
                columns: new[] { "ControllerArtifactSetDeploymentId", "ExecutionRouteId", "RobotProgramId", "RunOrder", "RobotArtifactId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EdgeCommandDeliveryAttempts_EdgeCommandId_DeliveryAttemptNo",
                table: "EdgeCommandDeliveryAttempts",
                columns: new[] { "EdgeCommandId", "DeliveryAttemptNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EdgeCommands_OrderId_DispatchAttemptNo",
                table: "EdgeCommands",
                columns: new[] { "OrderId", "DispatchAttemptNo" },
                unique: true,
                filter: "\"OrderId\" IS NOT NULL AND \"DispatchAttemptNo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EdgeCommands_TargetExecutionEndpointId_KioskId",
                table: "EdgeCommands",
                columns: new[] { "TargetExecutionEndpointId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_EdgeCommands_TargetExecutionEndpointId_Status_CreatedAt",
                table: "EdgeCommands",
                columns: new[] { "TargetExecutionEndpointId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointCredentialBindings_CredentialReference",
                table: "ExecutionEndpointCredentialBindings",
                column: "CredentialReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointCredentialBindings_KioskExecutionEndpointI~",
                table: "ExecutionEndpointCredentialBindings",
                columns: new[] { "KioskExecutionEndpointId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_DeviceId",
                table: "ExecutionEndpointSupportedRobotTargets",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~1",
                table: "ExecutionEndpointSupportedRobotTargets",
                columns: new[] { "KioskExecutionEndpointId", "RuntimeTargetCode", "MachineModelCode", "DeviceId" },
                unique: true,
                filter: "\"DeviceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~",
                table: "ExecutionEndpointSupportedRobotTargets",
                columns: new[] { "KioskExecutionEndpointId", "RuntimeTargetCode", "MachineModelCode" },
                unique: true,
                filter: "\"DeviceId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRouteRobotBindings_ExecutionRouteId_BindingOrder",
                table: "ExecutionRouteRobotBindings",
                columns: new[] { "ExecutionRouteId", "BindingOrder" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRouteRobotBindings_RobotProgramId",
                table: "ExecutionRouteRobotBindings",
                column: "RobotProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRoutes_ConfigurationReleaseId_ProductVariantId_Rec~",
                table: "ExecutionRoutes",
                columns: new[] { "ConfigurationReleaseId", "ProductVariantId", "RecipeId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRoutes_ConfigurationReleaseId_RouteCode",
                table: "ExecutionRoutes",
                columns: new[] { "ConfigurationReleaseId", "RouteCode" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRoutes_ProductVariantId",
                table: "ExecutionRoutes",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRoutes_RecipeId",
                table: "ExecutionRoutes",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_ConfigurationReleaseId",
                table: "KioskConfigurationDeployments",
                column: "ConfigurationReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Kios~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskExecutionEndpointId", "KioskId", "EdgeRuntimeId" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Stat~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskExecutionEndpointId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskId",
                table: "KioskConfigurationDeployments",
                column: "KioskId",
                unique: true,
                filter: "\"Status\" IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskId_ConfigurationReleaseI~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskId", "ConfigurationReleaseId", "AttemptNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskId_Status_RequestedAt",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskId", "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_RequestedByAccountId",
                table: "KioskConfigurationDeployments",
                column: "RequestedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_ControllerId",
                table: "KioskExecutionEndpoints",
                column: "ControllerId",
                unique: true,
                filter: "\"ControllerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_CredentialBindingId",
                table: "KioskExecutionEndpoints",
                column: "CredentialBindingId",
                unique: true,
                filter: "\"CredentialBindingId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_FullEdgeRuntimeId",
                table: "KioskExecutionEndpoints",
                column: "FullEdgeRuntimeId",
                unique: true,
                filter: "\"FullEdgeRuntimeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_Id_KioskId",
                table: "KioskExecutionEndpoints",
                columns: new[] { "Id", "KioskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_Id_KioskId_FullEdgeRuntimeId",
                table: "KioskExecutionEndpoints",
                columns: new[] { "Id", "KioskId", "FullEdgeRuntimeId" },
                unique: true,
                filter: "\"FullEdgeRuntimeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_KioskId_EndpointCode",
                table: "KioskExecutionEndpoints",
                columns: new[] { "KioskId", "EndpointCode" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_LastControllerActivationReportId",
                table: "KioskExecutionEndpoints",
                column: "LastControllerActivationReportId",
                unique: true,
                filter: "\"LastControllerActivationReportId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_LastEdgeActivationEventId",
                table: "KioskExecutionEndpoints",
                column: "LastEdgeActivationEventId",
                unique: true,
                filter: "\"LastEdgeActivationEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderExecutionRecords_KioskExecutionEndpointId_SourceExecut~",
                table: "OrderExecutionRecords",
                columns: new[] { "KioskExecutionEndpointId", "SourceExecutorId", "LastAppliedSourceEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderExecutionRecords_KioskExecutionEndpointId_Status_LastE~",
                table: "OrderExecutionRecords",
                columns: new[] { "KioskExecutionEndpointId", "Status", "LastExecutorReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderExecutionRecords_OrderId_CloudReceivedAt",
                table: "OrderExecutionRecords",
                columns: new[] { "OrderId", "CloudReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderExecutionRecords_SourceCommandId",
                table: "OrderExecutionRecords",
                column: "SourceCommandId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderExecutionRecords_SourceConfigurationReleaseId",
                table: "OrderExecutionRecords",
                column: "SourceConfigurationReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExecutionRecords_KioskExecutionEndpointId_SourceE~",
                table: "ProductionExecutionRecords",
                columns: new[] { "KioskExecutionEndpointId", "SourceExecutorId", "LastAppliedSourceEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExecutionRecords_KioskExecutionEndpointId_Status_~",
                table: "ProductionExecutionRecords",
                columns: new[] { "KioskExecutionEndpointId", "Status", "LastExecutorReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExecutionRecords_SourceCommandId",
                table: "ProductionExecutionRecords",
                column: "SourceCommandId",
                unique: true,
                filter: "\"SourceProductionJobId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExecutionRecords_SourceCommandId_SourceProduction~",
                table: "ProductionExecutionRecords",
                columns: new[] { "SourceCommandId", "SourceProductionJobId" },
                unique: true,
                filter: "\"SourceProductionJobId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifacts_OrganizationId_ArtifactCode_Checksum",
                table: "RobotArtifacts",
                columns: new[] { "OrganizationId", "ArtifactCode", "Checksum" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifacts_OriginNodeId_Version",
                table: "RobotArtifacts",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifacts_RuntimeTargetCode_MachineModelCode_Status",
                table: "RobotArtifacts",
                columns: new[] { "RuntimeTargetCode", "MachineModelCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifacts_StorageKey",
                table: "RobotArtifacts",
                column: "StorageKey",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotProgramArtifacts_OriginNodeId_Version",
                table: "RobotProgramArtifacts",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotProgramArtifacts_RobotArtifactId",
                table: "RobotProgramArtifacts",
                column: "RobotArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotProgramArtifacts_RobotProgramId_RunOrder",
                table: "RobotProgramArtifacts",
                columns: new[] { "RobotProgramId", "RunOrder" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ControllerArtifactSetDeployments_KioskExecutionEndpoints_Ki~",
                table: "ControllerArtifactSetDeployments",
                column: "KioskExecutionEndpointId",
                principalTable: "KioskExecutionEndpoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EdgeCommandDeliveryAttempts_EdgeCommands_EdgeCommandId",
                table: "EdgeCommandDeliveryAttempts",
                column: "EdgeCommandId",
                principalTable: "EdgeCommands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EdgeCommands_KioskExecutionEndpoints_TargetExecutionEndpoin~",
                table: "EdgeCommands",
                columns: new[] { "TargetExecutionEndpointId", "KioskId" },
                principalTable: "KioskExecutionEndpoints",
                principalColumns: new[] { "Id", "KioskId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionEndpointCredentialBindings_KioskExecutionEndpoints~",
                table: "ExecutionEndpointCredentialBindings",
                column: "KioskExecutionEndpointId",
                principalTable: "KioskExecutionEndpoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionEndpointCredentialBindings_KioskExecutionEndpoints~",
                table: "ExecutionEndpointCredentialBindings");

            migrationBuilder.DropTable(
                name: "ControllerArtifactSetItems");

            migrationBuilder.DropTable(
                name: "EdgeCommandDeliveryAttempts");

            migrationBuilder.DropTable(
                name: "ExecutionEndpointSupportedRobotTargets");

            migrationBuilder.DropTable(
                name: "ExecutionRouteRobotBindings");

            migrationBuilder.DropTable(
                name: "KioskConfigurationDeployments");

            migrationBuilder.DropTable(
                name: "OrderExecutionRecords");

            migrationBuilder.DropTable(
                name: "ProductionExecutionRecords");

            migrationBuilder.DropTable(
                name: "RobotProgramArtifacts");

            migrationBuilder.DropTable(
                name: "ControllerArtifactSetDeployments");

            migrationBuilder.DropTable(
                name: "ExecutionRoutes");

            migrationBuilder.DropTable(
                name: "EdgeCommands");

            migrationBuilder.DropTable(
                name: "RobotArtifacts");

            migrationBuilder.DropTable(
                name: "ConfigurationReleases");

            migrationBuilder.DropTable(
                name: "KioskExecutionEndpoints");

            migrationBuilder.DropTable(
                name: "ExecutionEndpointCredentialBindings");

            migrationBuilder.DropIndex(
                name: "IX_RobotPrograms_OrganizationId_StoreId_KioskId_DeviceId_Code",
                table: "RobotPrograms");

            migrationBuilder.DropIndex(
                name: "IX_RobotPrograms_ProgramManifestChecksum",
                table: "RobotPrograms");

            migrationBuilder.DropColumn(
                name: "FulfillmentType",
                table: "ProductVariants");

            migrationBuilder.RenameColumn(
                name: "ProgramManifestSchemaVersion",
                table: "RobotPrograms",
                newName: "SafetyZoneSchemaVersion");

            migrationBuilder.RenameColumn(
                name: "ProgramManifestJson",
                table: "RobotPrograms",
                newName: "SafetyZoneJson");

            migrationBuilder.RenameColumn(
                name: "ProgramManifestChecksum",
                table: "RobotPrograms",
                newName: "VendorProgramVersion");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActivatedAt",
                table: "RobotPrograms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EffectiveFrom",
                table: "RobotPrograms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EffectiveTo",
                table: "RobotPrograms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationSeconds",
                table: "RobotPrograms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "RobotPrograms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PointSnapshotJson",
                table: "RobotPrograms",
                type: "jsonb",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PointSnapshotSchemaVersion",
                table: "RobotPrograms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointStatus",
                table: "RobotPrograms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PointValidatedAt",
                table: "RobotPrograms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PointValidatedByAccountId",
                table: "RobotPrograms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "RobotPrograms",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProgramPayloadJson",
                table: "RobotPrograms",
                type: "jsonb",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgramPayloadSchemaVersion",
                table: "RobotPrograms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProgramVersion",
                table: "RobotPrograms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SupportedDeviceTypeId",
                table: "RobotPrograms",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TemplateProgramId",
                table: "RobotPrograms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Vendor",
                table: "RobotPrograms",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VendorProgramId",
                table: "RobotPrograms",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RobotJobId",
                table: "OperationLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RobotJobId",
                table: "DeviceEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RobotJobId",
                table: "Alerts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KioskRecipeExecutionProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Code = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExecutionSnapshotJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    ExecutionSnapshotSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolverPolicyJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    ResolverPolicySchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    RetiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KioskRecipeExecutionProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KioskRecipeExecutionProfiles_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskRecipeExecutionProfiles_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskRecipeExecutionProfiles_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskRecipeExecutionProfiles_ProductVariants_ProductVariant~",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskRecipeExecutionProfiles_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskRecipeExecutionProfiles_RobotPrograms_RobotProgramId",
                        column: x => x.RobotProgramId,
                        principalTable: "RobotPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskRecipeExecutionProfiles_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: true),
                    RobotProgramId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    JobNumber = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LastErrorCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProductionRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipeSnapshotJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    RecipeSnapshotSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    RecipeVersion = table.Column<int>(type: "integer", nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotJobs_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotJobs_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotJobs_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotJobs_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotJobs_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotJobs_RobotPrograms_RobotProgramId",
                        column: x => x.RobotProgramId,
                        principalTable: "RobotPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotProgramSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    CoordinateSystem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpectedDurationMs = table.Column<int>(type: "integer", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    MotionProfileCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NextOnFailureStepNumber = table.Column<int>(type: "integer", nullable: true),
                    NextOnSuccessStepNumber = table.Column<int>(type: "integer", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    ParametersOverrideJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    ParametersOverrideSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    ParametersSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    PointSnapshotJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    PointSnapshotSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    RetryPolicyJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    RetryPolicySchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    SafetyClearanceMm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    SpeedScale = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    StepCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StepCommandType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TargetPointCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ToolFrameCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    VendorPointName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    WorkpieceFrameCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotProgramSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotProgramSteps_RobotProgramSteps_TemplateStepId",
                        column: x => x.TemplateStepId,
                        principalTable: "RobotProgramSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotProgramSteps_RobotPrograms_RobotProgramId",
                        column: x => x.RobotProgramId,
                        principalTable: "RobotPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotJobSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotProgramStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CoordinateSystem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    MotionProfileCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    ParametersSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StepCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StepCommandType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TargetPointCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ToolFrameCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    VendorPointName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    WorkpieceFrameCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotJobSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotJobSteps_RobotJobs_RobotJobId",
                        column: x => x.RobotJobId,
                        principalTable: "RobotJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotJobSteps_RobotProgramSteps_RobotProgramStepId",
                        column: x => x.RobotProgramStepId,
                        principalTable: "RobotProgramSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotJobEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RobotJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotJobStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotJobEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotJobEvents_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotJobEvents_RobotJobSteps_RobotJobStepId",
                        column: x => x.RobotJobStepId,
                        principalTable: "RobotJobSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotJobEvents_RobotJobs_RobotJobId",
                        column: x => x.RobotJobId,
                        principalTable: "RobotJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RobotPrograms_OrganizationId_StoreId_KioskId_DeviceId_Code_~",
                table: "RobotPrograms",
                columns: new[] { "OrganizationId", "StoreId", "KioskId", "DeviceId", "Code", "ProgramVersion" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotPrograms_PointValidatedByAccountId",
                table: "RobotPrograms",
                column: "PointValidatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotPrograms_TemplateProgramId",
                table: "RobotPrograms",
                column: "TemplateProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_RobotJobId",
                table: "OperationLogs",
                column: "RobotJobId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_RobotJobId",
                table: "DeviceEvents",
                column: "RobotJobId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_RobotJobId",
                table: "Alerts",
                column: "RobotJobId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskRecipeExecutionProfiles_DeviceId",
                table: "KioskRecipeExecutionProfiles",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskRecipeExecutionProfiles_KioskId_DeviceId_RecipeId_Stat~",
                table: "KioskRecipeExecutionProfiles",
                columns: new[] { "KioskId", "DeviceId", "RecipeId", "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskRecipeExecutionProfiles_OrganizationId",
                table: "KioskRecipeExecutionProfiles",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskRecipeExecutionProfiles_OrganizationId_StoreId_KioskId~",
                table: "KioskRecipeExecutionProfiles",
                columns: new[] { "OrganizationId", "StoreId", "KioskId", "DeviceId", "RecipeId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskRecipeExecutionProfiles_OriginNodeId_Version",
                table: "KioskRecipeExecutionProfiles",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskRecipeExecutionProfiles_ProductVariantId",
                table: "KioskRecipeExecutionProfiles",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskRecipeExecutionProfiles_RecipeId",
                table: "KioskRecipeExecutionProfiles",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskRecipeExecutionProfiles_RobotProgramId",
                table: "KioskRecipeExecutionProfiles",
                column: "RobotProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskRecipeExecutionProfiles_StoreId",
                table: "KioskRecipeExecutionProfiles",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobEvents_DeviceId",
                table: "RobotJobEvents",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobEvents_EventId",
                table: "RobotJobEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobEvents_OriginNodeId_Version",
                table: "RobotJobEvents",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobEvents_RobotJobId_OccurredAt",
                table: "RobotJobEvents",
                columns: new[] { "RobotJobId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobEvents_RobotJobStepId",
                table: "RobotJobEvents",
                column: "RobotJobStepId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobs_DeviceId",
                table: "RobotJobs",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobs_IdempotencyKey",
                table: "RobotJobs",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobs_JobNumber",
                table: "RobotJobs",
                column: "JobNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobs_KioskId_RequestedAt",
                table: "RobotJobs",
                columns: new[] { "KioskId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobs_OrderId",
                table: "RobotJobs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobs_OrderItemId",
                table: "RobotJobs",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobs_OriginNodeId_Version",
                table: "RobotJobs",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobs_RecipeId",
                table: "RobotJobs",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobs_RobotProgramId",
                table: "RobotJobs",
                column: "RobotProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobSteps_OriginNodeId_Version",
                table: "RobotJobSteps",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobSteps_RobotJobId_StepNumber",
                table: "RobotJobSteps",
                columns: new[] { "RobotJobId", "StepNumber" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotJobSteps_RobotProgramStepId",
                table: "RobotJobSteps",
                column: "RobotProgramStepId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotProgramSteps_OriginNodeId_Version",
                table: "RobotProgramSteps",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotProgramSteps_RobotProgramId_StepCode",
                table: "RobotProgramSteps",
                columns: new[] { "RobotProgramId", "StepCode" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotProgramSteps_RobotProgramId_StepNumber",
                table: "RobotProgramSteps",
                columns: new[] { "RobotProgramId", "StepNumber" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotProgramSteps_TemplateStepId",
                table: "RobotProgramSteps",
                column: "TemplateStepId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_RobotJobs_RobotJobId",
                table: "Alerts",
                column: "RobotJobId",
                principalTable: "RobotJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceEvents_RobotJobs_RobotJobId",
                table: "DeviceEvents",
                column: "RobotJobId",
                principalTable: "RobotJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OperationLogs_RobotJobs_RobotJobId",
                table: "OperationLogs",
                column: "RobotJobId",
                principalTable: "RobotJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RobotPrograms_Accounts_PointValidatedByAccountId",
                table: "RobotPrograms",
                column: "PointValidatedByAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RobotPrograms_RobotPrograms_TemplateProgramId",
                table: "RobotPrograms",
                column: "TemplateProgramId",
                principalTable: "RobotPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

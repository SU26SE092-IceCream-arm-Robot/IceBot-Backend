using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatchUpProductionPackageAndExecutionWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "ProductionExecutionRecords") THEN
                        RAISE EXCEPTION 'ProductionExecutionRecords must be empty before adding production-unit identity. Preserve and migrate those records explicitly before retrying.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategories_ProductCategories_ParentCategoryId",
                table: "ProductCategories");

            migrationBuilder.DropIndex(
                name: "IX_ProductCategories_ParentCategoryId",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "ConfigJson",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "ConfigSchemaVersion",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "SupportsOfflineMode",
                table: "Kiosks");

            migrationBuilder.AddColumn<string>(
                name: "RequiredOptionCode",
                table: "RobotProgramArtifacts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalContractChecksum",
                table: "RobotArtifactTemplates",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TechnicalContractId",
                table: "RobotArtifactTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalContractChecksum",
                table: "RobotArtifacts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TechnicalContractId",
                table: "RobotArtifacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExecutionImpact",
                table: "ProductOptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderItemId",
                table: "ProductionExecutionRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "ProductionUnitNo",
                table: "ProductionExecutionRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductionUnitQuantity",
                table: "ProductionExecutionRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FulfillmentType",
                table: "OrderItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExecutionImpact",
                table: "OrderItemOptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RiskAcknowledgedAt",
                table: "KioskConfigurationDeployments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RiskAcknowledgedByAccountId",
                table: "KioskConfigurationDeployments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskLevel",
                table: "KioskConfigurationDeployments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationReportChecksum",
                table: "KioskConfigurationDeployments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarningCodesJson",
                table: "KioskConfigurationDeployments",
                type: "jsonb",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductionDefinitionChecksum",
                table: "ExecutionRoutes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductionDefinitionJson",
                table: "ExecutionRoutes",
                type: "jsonb",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductionDefinitionSchemaVersion",
                table: "ExecutionRoutes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportedOptionCodesJson",
                table: "ExecutionRoutes",
                type: "jsonb",
                maxLength: 10000,
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "RequiredOptionCode",
                table: "ControllerArtifactSetItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RiskAcknowledgedAt",
                table: "ControllerArtifactSetDeployments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RiskAcknowledgedByAccountId",
                table: "ControllerArtifactSetDeployments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskLevel",
                table: "ControllerArtifactSetDeployments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationReportChecksum",
                table: "ControllerArtifactSetDeployments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarningCodesJson",
                table: "ControllerArtifactSetDeployments",
                type: "jsonb",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "OrderItems" AS item
                SET "FulfillmentType" = variant."FulfillmentType"
                FROM "ProductVariants" AS variant
                WHERE variant."Id" = item."ProductVariantId";

                UPDATE "OrderItems"
                SET "FulfillmentType" = 2
                WHERE "FulfillmentType" IS NULL;

                ALTER TABLE "OrderItems"
                    ALTER COLUMN "FulfillmentType" SET NOT NULL;

                UPDATE "KioskConfigurationDeployments"
                SET "ValidationReportChecksum" = 'legacy',
                    "RiskLevel" = 'Legacy',
                    "WarningCodesJson" = '[]'::jsonb;

                UPDATE "ControllerArtifactSetDeployments"
                SET "ValidationReportChecksum" = 'legacy',
                    "RiskLevel" = 'Legacy',
                    "WarningCodesJson" = '[]'::jsonb;

                ALTER TABLE "KioskConfigurationDeployments"
                    ALTER COLUMN "ValidationReportChecksum" SET NOT NULL,
                    ALTER COLUMN "RiskLevel" SET NOT NULL,
                    ALTER COLUMN "WarningCodesJson" SET NOT NULL;

                ALTER TABLE "ControllerArtifactSetDeployments"
                    ALTER COLUMN "ValidationReportChecksum" SET NOT NULL,
                    ALTER COLUMN "RiskLevel" SET NOT NULL,
                    ALTER COLUMN "WarningCodesJson" SET NOT NULL;
                """);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Stores_Id_OrganizationId",
                table: "Stores",
                columns: new[] { "Id", "OrganizationId" });

            migrationBuilder.CreateTable(
                name: "KioskConnectivityProjections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastHeartbeatSequence = table.Column<long>(type: "bigint", nullable: true),
                    LastObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastTransitionedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KioskConnectivityProjections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KioskConnectivityProjections_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemOptionIngredientRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientCodeSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IngredientNameSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    QuantityPerOption = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequiredWorkcellCapabilityCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemOptionIngredientRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemOptionIngredientRequirements_OrderItemOptions_Orde~",
                        column: x => x.OrderItemOptionId,
                        principalTable: "OrderItemOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourcePayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FromStatus = table.Column<int>(type: "integer", nullable: false),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemStatusHistories_Accounts_ChangedByAccountId",
                        column: x => x.ChangedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemStatusHistories_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductOptionIngredientRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequiredWorkcellCapabilityCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOptionIngredientRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductOptionIngredientRequirements_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductOptionIngredientRequirements_ProductOptions_ProductO~",
                        column: x => x.ProductOptionId,
                        principalTable: "ProductOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotArtifactTechnicalContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceContractId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContractCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContractVersion = table.Column<int>(type: "integer", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    RuntimeTargetCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MachineModelCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ContractJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    ContractChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_RobotArtifactTechnicalContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotArtifactTechnicalContracts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RobotArtifactTechnicalContracts_RobotArtifactTechnicalContr~",
                        column: x => x.SourceContractId,
                        principalTable: "RobotArtifactTechnicalContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionPackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ManifestSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    ManifestJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    ManifestChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_ProductionPackageVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionPackageVersions_ProductionPackages_ProductionPack~",
                        column: x => x.ProductionPackageId,
                        principalTable: "ProductionPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotArtifactDeclaredEffects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EffectKind = table.Column<int>(type: "integer", nullable: false),
                    IngredientCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OptionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QuantityMode = table.Column<int>(type: "integer", nullable: false),
                    FixedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RequiredWorkcellCapabilityCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotArtifactDeclaredEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotArtifactDeclaredEffects_RobotArtifactTechnicalContract~",
                        column: x => x.TechnicalContractId,
                        principalTable: "RobotArtifactTechnicalContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RobotArtifactOrderingConstraints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstraintType = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortHint = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotArtifactOrderingConstraints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotArtifactOrderingConstraints_RobotArtifactTechnicalCont~",
                        column: x => x.TechnicalContractId,
                        principalTable: "RobotArtifactTechnicalContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageArtifactDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RobotArtifactTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArtifactChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TechnicalContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalContractChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackageArtifactDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionPackageArtifactDefinitions_ProductionPackageVersi~",
                        column: x => x.PackageVersionId,
                        principalTable: "ProductionPackageVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPackageArtifactDefinitions_RobotArtifactTechnical~",
                        column: x => x.TechnicalContractId,
                        principalTable: "RobotArtifactTechnicalContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPackageArtifactDefinitions_RobotArtifactTemplates~",
                        column: x => x.RobotArtifactTemplateId,
                        principalTable: "RobotArtifactTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageInstallations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    PackageVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageManifestChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SelectedProductSourceKeysJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OwnershipMode = table.Column<int>(type: "integer", nullable: false),
                    DraftConfigurationReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackageInstallations", x => x.Id);
                    table.CheckConstraint("CK_ProductionPackageInstallations_KioskRequiresStore", "\"KioskId\" IS NULL OR \"StoreId\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_ProductionPackageInstallations_ConfigurationReleases_DraftC~",
                        column: x => x.DraftConfigurationReleaseId,
                        principalTable: "ConfigurationReleases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPackageInstallations_Kiosks_KioskId_OrganizationId",
                        columns: x => new { x.KioskId, x.OrganizationId },
                        principalTable: "Kiosks",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPackageInstallations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPackageInstallations_ProductionPackageVersions_Pa~",
                        column: x => x.PackageVersionId,
                        principalTable: "ProductionPackageVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPackageInstallations_Stores_StoreId_OrganizationId",
                        columns: x => new { x.StoreId, x.OrganizationId },
                        principalTable: "Stores",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageProductDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductSnapshotJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    ProductSnapshotChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackageProductDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionPackageProductDefinitions_ProductionPackageVersio~",
                        column: x => x.PackageVersionId,
                        principalTable: "ProductionPackageVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPackageProductDefinitions_Products_SourceProductId",
                        column: x => x.SourceProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageProgramBlueprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlueprintCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RuntimeTargetCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MachineModelCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackageProgramBlueprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionPackageProgramBlueprints_ProductionPackageVersion~",
                        column: x => x.PackageVersionId,
                        principalTable: "ProductionPackageVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageRouteBlueprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductSourceKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductVariantSourceKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecipeSourceKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SupportedOptionCodesJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    ProgramBlueprintCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequiredCapabilitiesJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackageRouteBlueprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionPackageRouteBlueprints_ProductionPackageVersions_~",
                        column: x => x.PackageVersionId,
                        principalTable: "ProductionPackageVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionCompositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: true),
                    RuntimeTargetCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MachineModelCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InputSchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    InputJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    InputChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GeneratedRobotProgramId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionCompositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionCompositions_KioskExecutionEndpoints_TargetExecut~",
                        column: x => x.TargetExecutionEndpointId,
                        principalTable: "KioskExecutionEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionCompositions_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionCompositions_ProductionPackageInstallations_Insta~",
                        column: x => x.InstallationId,
                        principalTable: "ProductionPackageInstallations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionCompositions_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionCompositions_RobotPrograms_GeneratedRobotProgramId",
                        column: x => x.GeneratedRobotProgramId,
                        principalTable: "RobotPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageMaterializations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceKind = table.Column<int>(type: "integer", nullable: false),
                    SourceKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TargetKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackageMaterializations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionPackageMaterializations_ProductionPackageInstalla~",
                        column: x => x.InstallationId,
                        principalTable: "ProductionPackageInstallations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPackageProgramSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramBlueprintId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ArtifactSourceKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequiredEffectCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phase = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    AllowMultiple = table.Column<bool>(type: "boolean", nullable: false),
                    SortHint = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPackageProgramSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionPackageProgramSlots_ProductionPackageProgramBluep~",
                        column: x => x.ProgramBlueprintId,
                        principalTable: "ProductionPackageProgramBlueprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifactTemplates_TechnicalContractId",
                table: "RobotArtifactTemplates",
                column: "TechnicalContractId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifacts_TechnicalContractId",
                table: "RobotArtifacts",
                column: "TechnicalContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExecutionRecords_OrderItemId",
                table: "ProductionExecutionRecords",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExecutionRecords_SourceCommandId_OrderItemId_Prod~",
                table: "ProductionExecutionRecords",
                columns: new[] { "SourceCommandId", "OrderItemId", "ProductionUnitNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_RiskAcknowledgedByAccountId",
                table: "KioskConfigurationDeployments",
                column: "RiskAcknowledgedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRoutes_ProductionDefinitionChecksum",
                table: "ExecutionRoutes",
                column: "ProductionDefinitionChecksum",
                filter: "\"ProductionDefinitionChecksum\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_RequestedByAccountId",
                table: "ControllerArtifactSetDeployments",
                column: "RequestedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_RiskAcknowledgedByAccountId",
                table: "ControllerArtifactSetDeployments",
                column: "RiskAcknowledgedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskConnectivityProjections_KioskId",
                table: "KioskConnectivityProjections",
                column: "KioskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KioskConnectivityProjections_Status_LastObservedAt",
                table: "KioskConnectivityProjections",
                columns: new[] { "Status", "LastObservedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemOptionIngredientRequirements_OrderItemOptionId_Ing~",
                table: "OrderItemOptionIngredientRequirements",
                columns: new[] { "OrderItemOptionId", "IngredientId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemStatusHistories_ChangedByAccountId",
                table: "OrderItemStatusHistories",
                column: "ChangedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemStatusHistories_OrderItemId_ChangedAt",
                table: "OrderItemStatusHistories",
                columns: new[] { "OrderItemId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemStatusHistories_OrderItemId_SourceEventId",
                table: "OrderItemStatusHistories",
                columns: new[] { "OrderItemId", "SourceEventId" },
                unique: true,
                filter: "\"SourceEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCompositions_GeneratedRobotProgramId",
                table: "ProductionCompositions",
                column: "GeneratedRobotProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCompositions_InstallationId_InputChecksum",
                table: "ProductionCompositions",
                columns: new[] { "InstallationId", "InputChecksum" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCompositions_OrganizationId_ProductVariantId_Stat~",
                table: "ProductionCompositions",
                columns: new[] { "OrganizationId", "ProductVariantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCompositions_ProductVariantId",
                table: "ProductionCompositions",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCompositions_RecipeId",
                table: "ProductionCompositions",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCompositions_TargetExecutionEndpointId",
                table: "ProductionCompositions",
                column: "TargetExecutionEndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageArtifactDefinitions_PackageVersionId_Sourc~",
                table: "ProductionPackageArtifactDefinitions",
                columns: new[] { "PackageVersionId", "SourceKey" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageArtifactDefinitions_RobotArtifactTemplateId",
                table: "ProductionPackageArtifactDefinitions",
                column: "RobotArtifactTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageArtifactDefinitions_TechnicalContractId",
                table: "ProductionPackageArtifactDefinitions",
                column: "TechnicalContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageInstallations_DraftConfigurationReleaseId",
                table: "ProductionPackageInstallations",
                column: "DraftConfigurationReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageInstallations_KioskId_OrganizationId",
                table: "ProductionPackageInstallations",
                columns: new[] { "KioskId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageInstallations_OrganizationId_IdempotencyKey",
                table: "ProductionPackageInstallations",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageInstallations_OrganizationId_Status_Starte~",
                table: "ProductionPackageInstallations",
                columns: new[] { "OrganizationId", "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageInstallations_PackageVersionId",
                table: "ProductionPackageInstallations",
                column: "PackageVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageInstallations_StoreId_OrganizationId",
                table: "ProductionPackageInstallations",
                columns: new[] { "StoreId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageMaterializations_InstallationId_ResourceKi~",
                table: "ProductionPackageMaterializations",
                columns: new[] { "InstallationId", "ResourceKind", "SourceKey" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageProductDefinitions_PackageVersionId_Source~",
                table: "ProductionPackageProductDefinitions",
                columns: new[] { "PackageVersionId", "SourceKey" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageProductDefinitions_SourceProductId",
                table: "ProductionPackageProductDefinitions",
                column: "SourceProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageProgramBlueprints_PackageVersionId_Bluepri~",
                table: "ProductionPackageProgramBlueprints",
                columns: new[] { "PackageVersionId", "BlueprintCode" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageProgramSlots_ProgramBlueprintId_SlotCode",
                table: "ProductionPackageProgramSlots",
                columns: new[] { "ProgramBlueprintId", "SlotCode" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageRouteBlueprints_PackageVersionId_RouteCode",
                table: "ProductionPackageRouteBlueprints",
                columns: new[] { "PackageVersionId", "RouteCode" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackages_Code",
                table: "ProductionPackages",
                column: "Code",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageVersions_ManifestChecksum",
                table: "ProductionPackageVersions",
                column: "ManifestChecksum",
                unique: true,
                filter: "\"ManifestChecksum\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPackageVersions_ProductionPackageId_Version",
                table: "ProductionPackageVersions",
                columns: new[] { "ProductionPackageId", "Version" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionIngredientRequirements_IngredientId",
                table: "ProductOptionIngredientRequirements",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionIngredientRequirements_ProductOptionId_Ingredi~",
                table: "ProductOptionIngredientRequirements",
                columns: new[] { "ProductOptionId", "IngredientId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifactDeclaredEffects_TechnicalContractId_EffectCode",
                table: "RobotArtifactDeclaredEffects",
                columns: new[] { "TechnicalContractId", "EffectCode" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifactOrderingConstraints_TechnicalContractId_Constr~",
                table: "RobotArtifactOrderingConstraints",
                columns: new[] { "TechnicalContractId", "ConstraintType", "Value", "SortHint" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifactTechnicalContracts_ContractChecksum",
                table: "RobotArtifactTechnicalContracts",
                column: "ContractChecksum",
                unique: true,
                filter: "\"ContractChecksum\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifactTechnicalContracts_ContractCode_ContractVersion",
                table: "RobotArtifactTechnicalContracts",
                columns: new[] { "ContractCode", "ContractVersion" },
                unique: true,
                filter: "\"OrganizationId\" IS NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifactTechnicalContracts_OrganizationId_ContractCode~",
                table: "RobotArtifactTechnicalContracts",
                columns: new[] { "OrganizationId", "ContractCode", "ContractVersion" },
                unique: true,
                filter: "\"OrganizationId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifactTechnicalContracts_SourceContractId",
                table: "RobotArtifactTechnicalContracts",
                column: "SourceContractId");

            migrationBuilder.AddForeignKey(
                name: "FK_ControllerArtifactSetDeployments_Accounts_RequestedByAccoun~",
                table: "ControllerArtifactSetDeployments",
                column: "RequestedByAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ControllerArtifactSetDeployments_Accounts_RiskAcknowledgedB~",
                table: "ControllerArtifactSetDeployments",
                column: "RiskAcknowledgedByAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KioskConfigurationDeployments_Accounts_RiskAcknowledgedByAc~",
                table: "KioskConfigurationDeployments",
                column: "RiskAcknowledgedByAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionExecutionRecords_OrderItems_OrderItemId",
                table: "ProductionExecutionRecords",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RobotArtifacts_RobotArtifactTechnicalContracts_TechnicalCon~",
                table: "RobotArtifacts",
                column: "TechnicalContractId",
                principalTable: "RobotArtifactTechnicalContracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RobotArtifactTemplates_RobotArtifactTechnicalContracts_Tech~",
                table: "RobotArtifactTemplates",
                column: "TechnicalContractId",
                principalTable: "RobotArtifactTechnicalContracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ControllerArtifactSetDeployments_Accounts_RequestedByAccoun~",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_ControllerArtifactSetDeployments_Accounts_RiskAcknowledgedB~",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_KioskConfigurationDeployments_Accounts_RiskAcknowledgedByAc~",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionExecutionRecords_OrderItems_OrderItemId",
                table: "ProductionExecutionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_RobotArtifacts_RobotArtifactTechnicalContracts_TechnicalCon~",
                table: "RobotArtifacts");

            migrationBuilder.DropForeignKey(
                name: "FK_RobotArtifactTemplates_RobotArtifactTechnicalContracts_Tech~",
                table: "RobotArtifactTemplates");

            migrationBuilder.DropTable(
                name: "KioskConnectivityProjections");

            migrationBuilder.DropTable(
                name: "OrderItemOptionIngredientRequirements");

            migrationBuilder.DropTable(
                name: "OrderItemStatusHistories");

            migrationBuilder.DropTable(
                name: "ProductionCompositions");

            migrationBuilder.DropTable(
                name: "ProductionPackageArtifactDefinitions");

            migrationBuilder.DropTable(
                name: "ProductionPackageMaterializations");

            migrationBuilder.DropTable(
                name: "ProductionPackageProductDefinitions");

            migrationBuilder.DropTable(
                name: "ProductionPackageProgramSlots");

            migrationBuilder.DropTable(
                name: "ProductionPackageRouteBlueprints");

            migrationBuilder.DropTable(
                name: "ProductOptionIngredientRequirements");

            migrationBuilder.DropTable(
                name: "RobotArtifactDeclaredEffects");

            migrationBuilder.DropTable(
                name: "RobotArtifactOrderingConstraints");

            migrationBuilder.DropTable(
                name: "ProductionPackageInstallations");

            migrationBuilder.DropTable(
                name: "ProductionPackageProgramBlueprints");

            migrationBuilder.DropTable(
                name: "RobotArtifactTechnicalContracts");

            migrationBuilder.DropTable(
                name: "ProductionPackageVersions");

            migrationBuilder.DropTable(
                name: "ProductionPackages");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Stores_Id_OrganizationId",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_RobotArtifactTemplates_TechnicalContractId",
                table: "RobotArtifactTemplates");

            migrationBuilder.DropIndex(
                name: "IX_RobotArtifacts_TechnicalContractId",
                table: "RobotArtifacts");

            migrationBuilder.DropIndex(
                name: "IX_ProductionExecutionRecords_OrderItemId",
                table: "ProductionExecutionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ProductionExecutionRecords_SourceCommandId_OrderItemId_Prod~",
                table: "ProductionExecutionRecords");

            migrationBuilder.DropIndex(
                name: "IX_KioskConfigurationDeployments_RiskAcknowledgedByAccountId",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionRoutes_ProductionDefinitionChecksum",
                table: "ExecutionRoutes");

            migrationBuilder.DropIndex(
                name: "IX_ControllerArtifactSetDeployments_RequestedByAccountId",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropIndex(
                name: "IX_ControllerArtifactSetDeployments_RiskAcknowledgedByAccountId",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropColumn(
                name: "RequiredOptionCode",
                table: "RobotProgramArtifacts");

            migrationBuilder.DropColumn(
                name: "TechnicalContractChecksum",
                table: "RobotArtifactTemplates");

            migrationBuilder.DropColumn(
                name: "TechnicalContractId",
                table: "RobotArtifactTemplates");

            migrationBuilder.DropColumn(
                name: "TechnicalContractChecksum",
                table: "RobotArtifacts");

            migrationBuilder.DropColumn(
                name: "TechnicalContractId",
                table: "RobotArtifacts");

            migrationBuilder.DropColumn(
                name: "ExecutionImpact",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "ProductionExecutionRecords");

            migrationBuilder.DropColumn(
                name: "ProductionUnitNo",
                table: "ProductionExecutionRecords");

            migrationBuilder.DropColumn(
                name: "ProductionUnitQuantity",
                table: "ProductionExecutionRecords");

            migrationBuilder.DropColumn(
                name: "FulfillmentType",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ExecutionImpact",
                table: "OrderItemOptions");

            migrationBuilder.DropColumn(
                name: "RiskAcknowledgedAt",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropColumn(
                name: "RiskAcknowledgedByAccountId",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropColumn(
                name: "ValidationReportChecksum",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropColumn(
                name: "WarningCodesJson",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropColumn(
                name: "ProductionDefinitionChecksum",
                table: "ExecutionRoutes");

            migrationBuilder.DropColumn(
                name: "ProductionDefinitionJson",
                table: "ExecutionRoutes");

            migrationBuilder.DropColumn(
                name: "ProductionDefinitionSchemaVersion",
                table: "ExecutionRoutes");

            migrationBuilder.DropColumn(
                name: "SupportedOptionCodesJson",
                table: "ExecutionRoutes");

            migrationBuilder.DropColumn(
                name: "RequiredOptionCode",
                table: "ControllerArtifactSetItems");

            migrationBuilder.DropColumn(
                name: "RiskAcknowledgedAt",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropColumn(
                name: "RiskAcknowledgedByAccountId",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropColumn(
                name: "ValidationReportChecksum",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropColumn(
                name: "WarningCodesJson",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.AddColumn<long>(
                name: "ParentCategoryId",
                table: "ProductCategories",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfigJson",
                table: "PaymentMethods",
                type: "jsonb",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfigSchemaVersion",
                table: "PaymentMethods",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsOfflineMode",
                table: "Kiosks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_ParentCategoryId",
                table: "ProductCategories",
                column: "ParentCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_ProductCategories_ParentCategoryId",
                table: "ProductCategories",
                column: "ParentCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

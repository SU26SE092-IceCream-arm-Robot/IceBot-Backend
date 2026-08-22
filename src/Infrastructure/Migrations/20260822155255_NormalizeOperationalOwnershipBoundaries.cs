using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeOperationalOwnershipBoundaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountRoles_AccountId_RoleId_OrganizationId_StoreId_KioskId",
                table: "AccountRoles");

            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    invalid_bindings text;
                BEGIN
                    SELECT string_agg(erb."Id"::text, ', ' ORDER BY erb."Id"::text)
                    INTO invalid_bindings
                    FROM "ExecutionRouteRobotBindings" erb
                    JOIN "RobotPrograms" program ON program."Id" = erb."RobotProgramId"
                    WHERE erb."ProductionProgramBindingId" IS NULL
                      AND (program."ProgramManifestChecksum" IS NULL OR length(program."ProgramManifestChecksum") <> 64);

                    IF invalid_bindings IS NOT NULL THEN
                        RAISE EXCEPTION 'Cannot migrate release bindings whose robot programs have no published manifest checksum. ExecutionRouteRobotBinding ids: %', invalid_bindings;
                    END IF;
                END $$;

                UPDATE "ExecutionRouteRobotBindings" erb
                SET "ProductionProgramBindingChecksum" = binding."BindingChecksum"
                FROM "ProductionProgramBindings" binding
                WHERE binding."Id" = erb."ProductionProgramBindingId"
                  AND erb."ProductionProgramBindingChecksum" IS DISTINCT FROM binding."BindingChecksum";

                CREATE TEMP TABLE "_RouteBindingRepair" ON COMMIT DROP AS
                WITH source AS (
                    SELECT
                        erb."Id" AS "RouteBindingId",
                        release."OrganizationId",
                        route."ProductVariantId",
                        route."RecipeId",
                        recipe."Version" AS "RecipeVersion",
                        erb."RobotProgramId",
                        lower(program."ProgramManifestChecksum") AS "ProgramManifestChecksum",
                        COALESCE((
                            SELECT array_to_json(array_agg(value ORDER BY value))::text
                            FROM jsonb_array_elements_text(erb."RequiredCapabilityCodesJson") value
                        ), '[]') AS "RequiredCapabilitiesJson",
                        COALESCE((
                            SELECT array_to_json(array_agg(value ORDER BY value))::text
                            FROM jsonb_array_elements_text(route."SupportedOptionCodesJson") value
                        ), '[]') AS "SupportedOptionsJson"
                    FROM "ExecutionRouteRobotBindings" erb
                    JOIN "ExecutionRoutes" route ON route."Id" = erb."ExecutionRouteId"
                    JOIN "ConfigurationReleases" release ON release."Id" = route."ConfigurationReleaseId"
                    JOIN "Recipes" recipe ON recipe."Id" = route."RecipeId"
                    JOIN "RobotPrograms" program ON program."Id" = erb."RobotProgramId"
                    WHERE erb."ProductionProgramBindingId" IS NULL
                ), checksummed AS (
                    SELECT source.*,
                        lower(encode(sha256(convert_to(
                            source."OrganizationId"::text || '|' || source."ProductVariantId"::text || '|' ||
                            source."RecipeId"::text || '|' || source."RecipeVersion"::text || '|' ||
                            source."RobotProgramId"::text || '|' || source."ProgramManifestChecksum" || '|' ||
                            source."RequiredCapabilitiesJson" || '|Declared|OperatorDeclared|' || source."SupportedOptionsJson",
                            'UTF8')), 'hex')) AS "BindingChecksum"
                    FROM source
                )
                SELECT checksummed.*,
                    (substr("BindingChecksum", 1, 8) || '-' || substr("BindingChecksum", 9, 4) || '-' ||
                     substr("BindingChecksum", 13, 4) || '-' || substr("BindingChecksum", 17, 4) || '-' ||
                     substr("BindingChecksum", 21, 12))::uuid AS "BindingId"
                FROM checksummed;

                INSERT INTO "ProductionProgramBindings" (
                    "Id", "OrganizationId", "ProductVariantId", "RecipeId", "RecipeVersion", "RobotProgramId",
                    "ProgramManifestChecksum", "RequiredCapabilityCodesJson", "CapabilityEvidenceStatus", "Assurance",
                    "SupportedOptionCodesJson", "BindingChecksum", "Status", "CreatedAt")
                SELECT DISTINCT ON (repair."BindingChecksum")
                    repair."BindingId", repair."OrganizationId", repair."ProductVariantId", repair."RecipeId",
                    repair."RecipeVersion", repair."RobotProgramId", repair."ProgramManifestChecksum",
                    repair."RequiredCapabilitiesJson"::jsonb, 0, 0, repair."SupportedOptionsJson"::jsonb,
                    repair."BindingChecksum", 0, NOW()
                FROM "_RouteBindingRepair" repair
                ORDER BY repair."BindingChecksum", repair."RouteBindingId"
                ON CONFLICT ("BindingChecksum") DO NOTHING;

                UPDATE "ExecutionRouteRobotBindings" erb
                SET "ProductionProgramBindingId" = binding."Id",
                    "ProductionProgramBindingChecksum" = binding."BindingChecksum"
                FROM "_RouteBindingRepair" repair
                JOIN "ProductionProgramBindings" binding ON binding."BindingChecksum" = repair."BindingChecksum"
                WHERE erb."Id" = repair."RouteBindingId";

                DO $$
                DECLARE
                    invalid_bindings text;
                BEGIN
                    SELECT string_agg("Id"::text, ', ' ORDER BY "Id"::text)
                    INTO invalid_bindings
                    FROM "ExecutionRouteRobotBindings"
                    WHERE "ProductionProgramBindingId" IS NULL
                       OR "ProductionProgramBindingChecksum" IS NULL
                       OR length("ProductionProgramBindingChecksum") <> 64;
                    IF invalid_bindings IS NOT NULL THEN
                        RAISE EXCEPTION 'Release bindings could not be normalized. ExecutionRouteRobotBinding ids: %', invalid_bindings;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "KioskIngredientInventories" (
                    "Id", "OrganizationId", "StoreId", "KioskId", "IngredientId", "Unit",
                    "EstimatedQuantity", "TrackingMode", "LastMeasuredAt", "IsActive", "CreatedAt",
                    "OriginNodeId", "Version")
                SELECT
                    (substr(md5('inventory:' || source."KioskId"::text || ':' || source."IngredientId"::text || ':' || source."Unit"), 1, 8) || '-' ||
                     substr(md5('inventory:' || source."KioskId"::text || ':' || source."IngredientId"::text || ':' || source."Unit"), 9, 4) || '-' ||
                     substr(md5('inventory:' || source."KioskId"::text || ':' || source."IngredientId"::text || ':' || source."Unit"), 13, 4) || '-' ||
                     substr(md5('inventory:' || source."KioskId"::text || ':' || source."IngredientId"::text || ':' || source."Unit"), 17, 4) || '-' ||
                     substr(md5('inventory:' || source."KioskId"::text || ':' || source."IngredientId"::text || ':' || source."Unit"), 21, 12))::uuid,
                    source."OrganizationId", source."StoreId", source."KioskId", source."IngredientId", source."Unit",
                    source."EstimatedQuantity", source."TrackingMode", source."LastMeasuredAt", source."IsActive", NOW(),
                    '00000000-0000-0000-0000-000000000000'::uuid, 1
                FROM (
                    SELECT state."KioskId", state."IngredientId", lower(state."Unit") AS "Unit",
                        kiosk."OrganizationId", kiosk."StoreId",
                        CASE WHEN bool_and(state."EstimatedQuantity" IS NULL) THEN NULL ELSE sum(COALESCE(state."EstimatedQuantity", 0)) END AS "EstimatedQuantity",
                        max(state."TrackingMode") AS "TrackingMode",
                        max(COALESCE(state."LastMeasuredAt", state."UpdatedAt", state."CreatedAt", NOW())) AS "LastMeasuredAt",
                        bool_or(state."IsActive" AND state."DeletedAt" IS NULL) AS "IsActive"
                    FROM "IngredientDispenserStates" state
                    JOIN "Kiosks" kiosk ON kiosk."Id" = state."KioskId"
                    WHERE state."KioskIngredientInventoryId" IS NULL
                    GROUP BY state."KioskId", state."IngredientId", lower(state."Unit"), kiosk."OrganizationId", kiosk."StoreId"
                ) source
                ON CONFLICT ("KioskId", "IngredientId", "Unit") DO NOTHING;

                UPDATE "IngredientDispenserStates" state
                SET "KioskIngredientInventoryId" = inventory."Id"
                FROM "KioskIngredientInventories" inventory
                WHERE state."KioskIngredientInventoryId" IS NULL
                  AND inventory."KioskId" = state."KioskId"
                  AND inventory."IngredientId" = state."IngredientId"
                  AND inventory."Unit" = lower(state."Unit");

                UPDATE "StockMovements" movement
                SET "KioskIngredientInventoryId" = state."KioskIngredientInventoryId"
                FROM "IngredientDispenserStates" state
                WHERE movement."KioskIngredientInventoryId" IS NULL
                  AND movement."IngredientDispenserStateId" = state."Id";

                UPDATE "StockMovements" movement
                SET "KioskIngredientInventoryId" = inventory."Id"
                FROM "KioskIngredientInventories" inventory
                WHERE movement."KioskIngredientInventoryId" IS NULL
                  AND movement."KioskId" = inventory."KioskId"
                  AND movement."IngredientId" = inventory."IngredientId"
                  AND lower(movement."Unit") = inventory."Unit";

                DO $$
                DECLARE
                    invalid_states text;
                    invalid_movements text;
                BEGIN
                    SELECT string_agg("Id"::text, ', ' ORDER BY "Id"::text) INTO invalid_states
                    FROM "IngredientDispenserStates" WHERE "KioskIngredientInventoryId" IS NULL;
                    SELECT string_agg("Id"::text, ', ' ORDER BY "Id"::text) INTO invalid_movements
                    FROM "StockMovements" WHERE "KioskIngredientInventoryId" IS NULL;
                    IF invalid_states IS NOT NULL THEN
                        RAISE EXCEPTION 'Dispenser states could not be linked to canonical kiosk inventory. Ids: %', invalid_states;
                    END IF;
                    IF invalid_movements IS NOT NULL THEN
                        RAISE EXCEPTION 'Stock movements could not be linked to canonical kiosk inventory. Ids: %', invalid_movements;
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "KioskIngredientInventoryId",
                table: "StockMovements",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "KioskIngredientInventoryId",
                table: "IngredientDispenserStates",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductionProgramBindingId",
                table: "ExecutionRouteRobotBindings",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductionProgramBindingChecksum",
                table: "ExecutionRouteRobotBindings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "RequiredWorkcellCapabilityCode",
                table: "ExecutionRouteRobotBindings");

            migrationBuilder.CreateTable(
                name: "TechnicianSupportGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AssignedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianSupportGrants", x => x.Id);
                    table.CheckConstraint("CK_TechnicianSupportGrants_ConcreteScope", "\"OrganizationId\" IS NOT NULL AND ((\"StoreId\" IS NOT NULL AND \"KioskId\" IS NULL) OR (\"StoreId\" IS NULL AND \"KioskId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_TechnicianSupportGrants_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicianSupportGrants_Accounts_AssignedByAccountId",
                        column: x => x.AssignedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicianSupportGrants_Accounts_RevokedByAccountId",
                        column: x => x.RevokedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicianSupportGrants_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicianSupportGrants_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TechnicianSupportGrants_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                UPDATE "AccountRoles" ar
                SET "OrganizationId" = kiosk."OrganizationId",
                    "StoreId" = NULL
                FROM "Roles" role, "Kiosks" kiosk
                WHERE ar."RoleId" = role."Id"
                  AND role."Code" = 'Technician'
                  AND ar."KioskId" = kiosk."Id";

                UPDATE "AccountRoles" ar
                SET "OrganizationId" = store."OrganizationId"
                FROM "Roles" role, "Stores" store
                WHERE ar."RoleId" = role."Id"
                  AND role."Code" = 'Technician'
                  AND ar."KioskId" IS NULL
                  AND ar."StoreId" = store."Id";

                INSERT INTO "TechnicianSupportGrants" (
                    "Id", "AccountId", "OrganizationId", "StoreId", "KioskId", "IsActive",
                    "AssignedAt", "AssignedByAccountId", "CreatedAt", "CreatedByAccountId")
                SELECT ar."Id", ar."AccountId", ar."OrganizationId", ar."StoreId", ar."KioskId", TRUE,
                    ar."AssignedAt", ar."AssignedByAccountId", ar."AssignedAt", ar."AssignedByAccountId"
                FROM "AccountRoles" ar
                JOIN "Roles" role ON role."Id" = ar."RoleId"
                WHERE role."Code" = 'Technician'
                  AND ar."IsActive" = TRUE
                  AND ar."OrganizationId" IS NOT NULL
                  AND ((ar."StoreId" IS NOT NULL AND ar."KioskId" IS NULL) OR
                       (ar."StoreId" IS NULL AND ar."KioskId" IS NOT NULL))
                ON CONFLICT DO NOTHING;

                DELETE FROM "AccountRoles" ar
                USING "Roles" role
                WHERE ar."RoleId" = role."Id" AND role."Code" = 'Technician';

                UPDATE "AccountRoles" ar
                SET "OrganizationId" = kiosk."OrganizationId",
                    "StoreId" = kiosk."StoreId"
                FROM "Kiosks" kiosk
                WHERE ar."KioskId" = kiosk."Id";

                UPDATE "AccountRoles" ar
                SET "OrganizationId" = store."OrganizationId"
                FROM "Stores" store
                WHERE ar."KioskId" IS NULL AND ar."StoreId" = store."Id";

                DO $$
                DECLARE
                    invalid_roles text;
                BEGIN
                    SELECT string_agg("Id"::text, ', ' ORDER BY "Id"::text)
                    INTO invalid_roles
                    FROM "AccountRoles"
                    WHERE NOT (
                        ("OrganizationId" IS NULL AND "StoreId" IS NULL AND "KioskId" IS NULL) OR
                        ("OrganizationId" IS NOT NULL AND "StoreId" IS NULL AND "KioskId" IS NULL) OR
                        ("OrganizationId" IS NOT NULL AND "StoreId" IS NOT NULL AND "KioskId" IS NULL) OR
                        ("OrganizationId" IS NOT NULL AND "StoreId" IS NOT NULL AND "KioskId" IS NOT NULL));
                    IF invalid_roles IS NOT NULL THEN
                        RAISE EXCEPTION 'Account role scopes could not be normalized. AccountRole ids: %', invalid_roles;
                    END IF;
                END $$;

                WITH duplicates AS (
                    SELECT "Id", row_number() OVER (
                        PARTITION BY "AccountId", "RoleId", "OrganizationId", "StoreId", "KioskId"
                        ORDER BY "AssignedAt", "Id") AS ordinal
                    FROM "AccountRoles"
                    WHERE "IsActive" = TRUE
                )
                UPDATE "AccountRoles" ar
                SET "IsActive" = FALSE
                FROM duplicates
                WHERE ar."Id" = duplicates."Id" AND duplicates.ordinal > 1;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION reject_platform_technician_account_role()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "Roles" role
                        WHERE role."Id" = NEW."RoleId" AND role."Code" = 'Technician') THEN
                        RAISE EXCEPTION 'Platform Technician roles cannot be stored in AccountRoles.'
                            USING ERRCODE = '23514';
                    END IF;
                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER "TR_AccountRoles_RejectPlatformTechnician"
                BEFORE INSERT OR UPDATE OF "RoleId" ON "AccountRoles"
                FOR EACH ROW EXECUTE FUNCTION reject_platform_technician_account_role();
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianSupportGrantHistories_ActorAccountId",
                table: "TechnicianSupportGrantHistories",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoles_AccountId_RoleId",
                table: "AccountRoles",
                columns: new[] { "AccountId", "RoleId" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"OrganizationId\" IS NULL AND \"StoreId\" IS NULL AND \"KioskId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoles_AccountId_RoleId_OrganizationId",
                table: "AccountRoles",
                columns: new[] { "AccountId", "RoleId", "OrganizationId" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"OrganizationId\" IS NOT NULL AND \"StoreId\" IS NULL AND \"KioskId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoles_AccountId_RoleId_OrganizationId_StoreId",
                table: "AccountRoles",
                columns: new[] { "AccountId", "RoleId", "OrganizationId", "StoreId" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"OrganizationId\" IS NOT NULL AND \"StoreId\" IS NOT NULL AND \"KioskId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoles_AccountId_RoleId_OrganizationId_StoreId_KioskId",
                table: "AccountRoles",
                columns: new[] { "AccountId", "RoleId", "OrganizationId", "StoreId", "KioskId" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"OrganizationId\" IS NOT NULL AND \"StoreId\" IS NOT NULL AND \"KioskId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccountRoles_ScopeHierarchy",
                table: "AccountRoles",
                sql: "(\"OrganizationId\" IS NULL AND \"StoreId\" IS NULL AND \"KioskId\" IS NULL) OR (\"OrganizationId\" IS NOT NULL AND \"StoreId\" IS NULL AND \"KioskId\" IS NULL) OR (\"OrganizationId\" IS NOT NULL AND \"StoreId\" IS NOT NULL AND \"KioskId\" IS NULL) OR (\"OrganizationId\" IS NOT NULL AND \"StoreId\" IS NOT NULL AND \"KioskId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianSupportGrants_AccountId_OrganizationId_KioskId",
                table: "TechnicianSupportGrants",
                columns: new[] { "AccountId", "OrganizationId", "KioskId" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"StoreId\" IS NULL AND \"KioskId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianSupportGrants_AccountId_OrganizationId_StoreId",
                table: "TechnicianSupportGrants",
                columns: new[] { "AccountId", "OrganizationId", "StoreId" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"StoreId\" IS NOT NULL AND \"KioskId\" IS NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianSupportGrants_AssignedByAccountId",
                table: "TechnicianSupportGrants",
                column: "AssignedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianSupportGrants_KioskId",
                table: "TechnicianSupportGrants",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianSupportGrants_OrganizationId",
                table: "TechnicianSupportGrants",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianSupportGrants_RevokedByAccountId",
                table: "TechnicianSupportGrants",
                column: "RevokedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianSupportGrants_StoreId",
                table: "TechnicianSupportGrants",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_TechnicianSupportGrantHistories_Accounts_AccountId",
                table: "TechnicianSupportGrantHistories",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TechnicianSupportGrantHistories_Accounts_ActorAccountId",
                table: "TechnicianSupportGrantHistories",
                column: "ActorAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TechnicianSupportScopeReplays_Accounts_AccountId",
                table: "TechnicianSupportScopeReplays",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS "TR_AccountRoles_RejectPlatformTechnician" ON "AccountRoles";
                DROP FUNCTION IF EXISTS reject_platform_technician_account_role();
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_TechnicianSupportGrantHistories_Accounts_AccountId",
                table: "TechnicianSupportGrantHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TechnicianSupportGrantHistories_Accounts_ActorAccountId",
                table: "TechnicianSupportGrantHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TechnicianSupportScopeReplays_Accounts_AccountId",
                table: "TechnicianSupportScopeReplays");

            migrationBuilder.DropTable(
                name: "TechnicianSupportGrants");

            migrationBuilder.DropIndex(
                name: "IX_TechnicianSupportGrantHistories_ActorAccountId",
                table: "TechnicianSupportGrantHistories");

            migrationBuilder.DropIndex(
                name: "IX_AccountRoles_AccountId_RoleId",
                table: "AccountRoles");

            migrationBuilder.DropIndex(
                name: "IX_AccountRoles_AccountId_RoleId_OrganizationId",
                table: "AccountRoles");

            migrationBuilder.DropIndex(
                name: "IX_AccountRoles_AccountId_RoleId_OrganizationId_StoreId",
                table: "AccountRoles");

            migrationBuilder.DropIndex(
                name: "IX_AccountRoles_AccountId_RoleId_OrganizationId_StoreId_KioskId",
                table: "AccountRoles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccountRoles_ScopeHierarchy",
                table: "AccountRoles");

            migrationBuilder.AlterColumn<Guid>(
                name: "KioskIngredientInventoryId",
                table: "StockMovements",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "KioskIngredientInventoryId",
                table: "IngredientDispenserStates",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductionProgramBindingId",
                table: "ExecutionRouteRobotBindings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "ProductionProgramBindingChecksum",
                table: "ExecutionRouteRobotBindings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "RequiredWorkcellCapabilityCode",
                table: "ExecutionRouteRobotBindings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoles_AccountId_RoleId_OrganizationId_StoreId_KioskId",
                table: "AccountRoles",
                columns: new[] { "AccountId", "RoleId", "OrganizationId", "StoreId", "KioskId" },
                unique: true);
        }
    }
}

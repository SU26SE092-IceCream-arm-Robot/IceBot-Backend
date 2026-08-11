using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeriveProductionBindingCapabilitiesFromContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Assurance",
                table: "ProductionProgramBindings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CapabilityEvidenceStatus",
                table: "ProductionProgramBindings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "RequiredCapabilityCodesJson",
                table: "ProductionProgramBindings",
                type: "jsonb",
                maxLength: 10000,
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            // Existing bindings were created from a manually supplied capability. It is not
            // trustworthy technical-contract evidence, so it intentionally migrates to Missing.
            migrationBuilder.DropColumn(
                name: "RequiredWorkcellCapabilityCode",
                table: "ProductionProgramBindings");

            migrationBuilder.AddColumn<string>(
                name: "RequiredCapabilityCodesJson",
                table: "ExecutionRouteRobotBindings",
                type: "jsonb",
                maxLength: 10000,
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            // Release routes are historical execution snapshots. Preserve their prior scalar
            // requirement as a one-item capability set while new routes may carry many codes.
            migrationBuilder.Sql("""
                UPDATE "ExecutionRouteRobotBindings"
                SET "RequiredCapabilityCodesJson" = CASE
                    WHEN COALESCE(BTRIM("RequiredWorkcellCapabilityCode"), '') = '' THEN '[]'::jsonb
                    ELSE jsonb_build_array(UPPER(BTRIM("RequiredWorkcellCapabilityCode")))
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequiredWorkcellCapabilityCode",
                table: "ProductionProgramBindings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "ProductionProgramBindings"
                SET "RequiredWorkcellCapabilityCode" = COALESCE("RequiredCapabilityCodesJson" ->> 0, '');
                """);

            migrationBuilder.DropColumn(
                name: "CapabilityEvidenceStatus",
                table: "ProductionProgramBindings");

            // This migration was applied in local development before Assurance was added to
            // its model. Keep rollback tolerant so that the incomplete schema can be repaired
            // by reverting and reapplying this migration.
            migrationBuilder.Sql("""
                ALTER TABLE "ProductionProgramBindings"
                DROP COLUMN IF EXISTS "Assurance";
                """);

            migrationBuilder.DropColumn(
                name: "RequiredCapabilityCodesJson",
                table: "ProductionProgramBindings");

            migrationBuilder.DropColumn(
                name: "RequiredCapabilityCodesJson",
                table: "ExecutionRouteRobotBindings");
        }
    }
}

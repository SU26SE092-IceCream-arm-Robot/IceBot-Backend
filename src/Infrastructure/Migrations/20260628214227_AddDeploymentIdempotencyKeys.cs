using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeploymentIdempotencyKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "KioskConfigurationDeployments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "ControllerArtifactSetDeployments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "KioskConfigurationDeployments"
                SET "IdempotencyKey" = 'legacy:' || "Id"::text
                WHERE "IdempotencyKey" IS NULL;

                UPDATE "ControllerArtifactSetDeployments"
                SET "IdempotencyKey" = 'legacy:' || "Id"::text
                WHERE "IdempotencyKey" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "IdempotencyKey",
                table: "KioskConfigurationDeployments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IdempotencyKey",
                table: "ControllerArtifactSetDeployments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Idem~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskExecutionEndpointId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_KioskExecutionEndpointId_I~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "KioskExecutionEndpointId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Idem~",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropIndex(
                name: "IX_ControllerArtifactSetDeployments_KioskExecutionEndpointId_I~",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "ControllerArtifactSetDeployments");
        }
    }
}

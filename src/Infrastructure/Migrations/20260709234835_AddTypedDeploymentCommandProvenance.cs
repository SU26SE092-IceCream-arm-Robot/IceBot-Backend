using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTypedDeploymentCommandProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RequestedCommandExpiryAt",
                table: "EdgeCommands",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RollbackTargetDeploymentId",
                table: "EdgeCommands",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "EdgeCommands"
                SET
                    "RollbackTargetDeploymentId" = NULLIF("PayloadJson" ->> 'RollbackTargetDeploymentId', '')::uuid,
                    "RequestedCommandExpiryAt" = NULLIF("PayloadJson" ->> 'RequestedCommandExpiryAt', '')::timestamptz
                WHERE "PayloadJson" ? 'RollbackTargetDeploymentId'
                   OR "PayloadJson" ? 'RequestedCommandExpiryAt';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedCommandExpiryAt",
                table: "EdgeCommands");

            migrationBuilder.DropColumn(
                name: "RollbackTargetDeploymentId",
                table: "EdgeCommands");
        }
    }
}

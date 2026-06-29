using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixExecutionEndpointProvisioningForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KioskConfigurationDeployments_KioskExecutionEndpoints_Kiosk~",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_KioskExecutionEndpoints_Id_KioskId_FullEdgeRuntimeId",
                table: "KioskExecutionEndpoints");

            migrationBuilder.DropIndex(
                name: "IX_KioskExecutionEndpoints_Id_KioskId_FullEdgeRuntimeId",
                table: "KioskExecutionEndpoints");

            migrationBuilder.DropIndex(
                name: "IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Kios~",
                table: "KioskConfigurationDeployments");

            migrationBuilder.AlterColumn<Guid>(
                name: "FullEdgeRuntimeId",
                table: "KioskExecutionEndpoints",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_KioskConfigurationDeployments_KioskExecutionEndpoints_Kiosk~",
                table: "KioskConfigurationDeployments",
                column: "KioskExecutionEndpointId",
                principalTable: "KioskExecutionEndpoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KioskConfigurationDeployments_KioskExecutionEndpoints_Kiosk~",
                table: "KioskConfigurationDeployments");

            migrationBuilder.AlterColumn<Guid>(
                name: "FullEdgeRuntimeId",
                table: "KioskExecutionEndpoints",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_KioskExecutionEndpoints_Id_KioskId_FullEdgeRuntimeId",
                table: "KioskExecutionEndpoints",
                columns: new[] { "Id", "KioskId", "FullEdgeRuntimeId" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskExecutionEndpoints_Id_KioskId_FullEdgeRuntimeId",
                table: "KioskExecutionEndpoints",
                columns: new[] { "Id", "KioskId", "FullEdgeRuntimeId" },
                unique: true,
                filter: "\"FullEdgeRuntimeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Kios~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskExecutionEndpointId", "KioskId", "EdgeRuntimeId" });

            migrationBuilder.AddForeignKey(
                name: "FK_KioskConfigurationDeployments_KioskExecutionEndpoints_Kiosk~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskExecutionEndpointId", "KioskId", "EdgeRuntimeId" },
                principalTable: "KioskExecutionEndpoints",
                principalColumns: new[] { "Id", "KioskId", "FullEdgeRuntimeId" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}

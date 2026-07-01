using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceCompositeScopingConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_Devices_DeviceId",
                table: "Alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_ControllerArtifactSetDeployments_ConfigurationReleases_Sour~",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_ControllerArtifactSetDeployments_KioskExecutionEndpoints_Ki~",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceEvents_Devices_DeviceId",
                table: "DeviceEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionEndpointReadinessProjections_KioskExecutionEndpoin~",
                table: "ExecutionEndpointReadinessProjections");

            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionEndpointSupportedRobotTargets_Devices_DeviceId",
                table: "ExecutionEndpointSupportedRobotTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~",
                table: "ExecutionEndpointSupportedRobotTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_KioskConfigurationDeployments_ConfigurationReleases_Configu~",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_KioskConfigurationDeployments_KioskExecutionEndpoints_Kiosk~",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderExecutionRecords_EdgeCommands_SourceCommandId",
                table: "OrderExecutionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionExecutionRecords_EdgeCommands_SourceCommandId",
                table: "ProductionExecutionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ProductionExecutionRecords_SourceCommandId",
                table: "ProductionExecutionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ProductionExecutionRecords_SourceCommandId_SourceProduction~",
                table: "ProductionExecutionRecords");

            migrationBuilder.DropIndex(
                name: "IX_KioskConfigurationDeployments_ConfigurationReleaseId",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_DeviceId",
                table: "ExecutionEndpointSupportedRobotTargets");

            migrationBuilder.DropIndex(
                name: "IX_ControllerArtifactSetDeployments_SourceConfigurationRelease~",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_DeviceId",
                table: "Alerts");

            migrationBuilder.RenameIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~",
                table: "ExecutionEndpointSupportedRobotTargets",
                newName: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~1");

            migrationBuilder.RenameIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~1",
                table: "ExecutionEndpointSupportedRobotTargets",
                newName: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~2");

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceProductionJobId",
                table: "ProductionExecutionRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "KioskConfigurationDeployments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "KioskId",
                table: "ExecutionEndpointSupportedRobotTargets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "KioskId",
                table: "Devices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "ControllerArtifactSetDeployments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Kiosks_Id_OrganizationId",
                table: "Kiosks",
                columns: new[] { "Id", "OrganizationId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_EdgeCommands_Id_TargetExecutionEndpointId",
                table: "EdgeCommands",
                columns: new[] { "Id", "TargetExecutionEndpointId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Devices_Id_KioskId",
                table: "Devices",
                columns: new[] { "Id", "KioskId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ConfigurationReleases_Id_OrganizationId",
                table: "ConfigurationReleases",
                columns: new[] { "Id", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExecutionRecords_SourceCommandId_KioskExecutionEn~",
                table: "ProductionExecutionRecords",
                columns: new[] { "SourceCommandId", "KioskExecutionEndpointId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionExecutionRecords_SourceCommandId_SourceProduction~",
                table: "ProductionExecutionRecords",
                columns: new[] { "SourceCommandId", "SourceProductionJobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderExecutionRecords_SourceCommandId_KioskExecutionEndpoin~",
                table: "OrderExecutionRecords",
                columns: new[] { "SourceCommandId", "KioskExecutionEndpointId" });

            migrationBuilder.CreateIndex(
                name: "IX_Kiosks_Id_OrganizationId",
                table: "Kiosks",
                columns: new[] { "Id", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_ConfigurationReleaseId_Organi~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "ConfigurationReleaseId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Kios~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskConfigurationDeployments_KioskId_OrganizationId",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_DeviceId_KioskId",
                table: "ExecutionEndpointSupportedRobotTargets",
                columns: new[] { "DeviceId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~",
                table: "ExecutionEndpointSupportedRobotTargets",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointReadinessProjections_KioskExecutionEndpoi~1",
                table: "ExecutionEndpointReadinessProjections",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Id_KioskId",
                table: "Devices",
                columns: new[] { "Id", "KioskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_DeviceId_KioskId",
                table: "DeviceEvents",
                columns: new[] { "DeviceId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_KioskExecutionEndpointId_K~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_KioskId_OrganizationId",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "KioskId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_SourceConfigurationRelease~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "SourceConfigurationReleaseId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationReleases_Id_OrganizationId",
                table: "ConfigurationReleases",
                columns: new[] { "Id", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_DeviceId_KioskId",
                table: "Alerts",
                columns: new[] { "DeviceId", "KioskId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_Devices_DeviceId_KioskId",
                table: "Alerts",
                columns: new[] { "DeviceId", "KioskId" },
                principalTable: "Devices",
                principalColumns: new[] { "Id", "KioskId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ControllerArtifactSetDeployments_ConfigurationReleases_Sour~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "SourceConfigurationReleaseId", "OrganizationId" },
                principalTable: "ConfigurationReleases",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ControllerArtifactSetDeployments_KioskExecutionEndpoints_Ki~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" },
                principalTable: "KioskExecutionEndpoints",
                principalColumns: new[] { "Id", "KioskId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ControllerArtifactSetDeployments_Kiosks_KioskId_Organizatio~",
                table: "ControllerArtifactSetDeployments",
                columns: new[] { "KioskId", "OrganizationId" },
                principalTable: "Kiosks",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceEvents_Devices_DeviceId_KioskId",
                table: "DeviceEvents",
                columns: new[] { "DeviceId", "KioskId" },
                principalTable: "Devices",
                principalColumns: new[] { "Id", "KioskId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionEndpointReadinessProjections_KioskExecutionEndpoin~",
                table: "ExecutionEndpointReadinessProjections",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" },
                principalTable: "KioskExecutionEndpoints",
                principalColumns: new[] { "Id", "KioskId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionEndpointSupportedRobotTargets_Devices_DeviceId_Kio~",
                table: "ExecutionEndpointSupportedRobotTargets",
                columns: new[] { "DeviceId", "KioskId" },
                principalTable: "Devices",
                principalColumns: new[] { "Id", "KioskId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~",
                table: "ExecutionEndpointSupportedRobotTargets",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" },
                principalTable: "KioskExecutionEndpoints",
                principalColumns: new[] { "Id", "KioskId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KioskConfigurationDeployments_ConfigurationReleases_Configu~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "ConfigurationReleaseId", "OrganizationId" },
                principalTable: "ConfigurationReleases",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KioskConfigurationDeployments_KioskExecutionEndpoints_Kiosk~",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" },
                principalTable: "KioskExecutionEndpoints",
                principalColumns: new[] { "Id", "KioskId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KioskConfigurationDeployments_Kiosks_KioskId_OrganizationId",
                table: "KioskConfigurationDeployments",
                columns: new[] { "KioskId", "OrganizationId" },
                principalTable: "Kiosks",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderExecutionRecords_EdgeCommands_SourceCommandId_KioskExe~",
                table: "OrderExecutionRecords",
                columns: new[] { "SourceCommandId", "KioskExecutionEndpointId" },
                principalTable: "EdgeCommands",
                principalColumns: new[] { "Id", "TargetExecutionEndpointId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionExecutionRecords_EdgeCommands_SourceCommandId_Kio~",
                table: "ProductionExecutionRecords",
                columns: new[] { "SourceCommandId", "KioskExecutionEndpointId" },
                principalTable: "EdgeCommands",
                principalColumns: new[] { "Id", "TargetExecutionEndpointId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_Devices_DeviceId_KioskId",
                table: "Alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_ControllerArtifactSetDeployments_ConfigurationReleases_Sour~",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_ControllerArtifactSetDeployments_KioskExecutionEndpoints_Ki~",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_ControllerArtifactSetDeployments_Kiosks_KioskId_Organizatio~",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceEvents_Devices_DeviceId_KioskId",
                table: "DeviceEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionEndpointReadinessProjections_KioskExecutionEndpoin~",
                table: "ExecutionEndpointReadinessProjections");

            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionEndpointSupportedRobotTargets_Devices_DeviceId_Kio~",
                table: "ExecutionEndpointSupportedRobotTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~",
                table: "ExecutionEndpointSupportedRobotTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_KioskConfigurationDeployments_ConfigurationReleases_Configu~",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_KioskConfigurationDeployments_KioskExecutionEndpoints_Kiosk~",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_KioskConfigurationDeployments_Kiosks_KioskId_OrganizationId",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderExecutionRecords_EdgeCommands_SourceCommandId_KioskExe~",
                table: "OrderExecutionRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionExecutionRecords_EdgeCommands_SourceCommandId_Kio~",
                table: "ProductionExecutionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ProductionExecutionRecords_SourceCommandId_KioskExecutionEn~",
                table: "ProductionExecutionRecords");

            migrationBuilder.DropIndex(
                name: "IX_ProductionExecutionRecords_SourceCommandId_SourceProduction~",
                table: "ProductionExecutionRecords");

            migrationBuilder.DropIndex(
                name: "IX_OrderExecutionRecords_SourceCommandId_KioskExecutionEndpoin~",
                table: "OrderExecutionRecords");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Kiosks_Id_OrganizationId",
                table: "Kiosks");

            migrationBuilder.DropIndex(
                name: "IX_Kiosks_Id_OrganizationId",
                table: "Kiosks");

            migrationBuilder.DropIndex(
                name: "IX_KioskConfigurationDeployments_ConfigurationReleaseId_Organi~",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropIndex(
                name: "IX_KioskConfigurationDeployments_KioskExecutionEndpointId_Kios~",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropIndex(
                name: "IX_KioskConfigurationDeployments_KioskId_OrganizationId",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_DeviceId_KioskId",
                table: "ExecutionEndpointSupportedRobotTargets");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~",
                table: "ExecutionEndpointSupportedRobotTargets");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionEndpointReadinessProjections_KioskExecutionEndpoi~1",
                table: "ExecutionEndpointReadinessProjections");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_EdgeCommands_Id_TargetExecutionEndpointId",
                table: "EdgeCommands");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Devices_Id_KioskId",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Devices_Id_KioskId",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_DeviceEvents_DeviceId_KioskId",
                table: "DeviceEvents");

            migrationBuilder.DropIndex(
                name: "IX_ControllerArtifactSetDeployments_KioskExecutionEndpointId_K~",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropIndex(
                name: "IX_ControllerArtifactSetDeployments_KioskId_OrganizationId",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropIndex(
                name: "IX_ControllerArtifactSetDeployments_SourceConfigurationRelease~",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ConfigurationReleases_Id_OrganizationId",
                table: "ConfigurationReleases");

            migrationBuilder.DropIndex(
                name: "IX_ConfigurationReleases_Id_OrganizationId",
                table: "ConfigurationReleases");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_DeviceId_KioskId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "KioskConfigurationDeployments");

            migrationBuilder.DropColumn(
                name: "KioskId",
                table: "ExecutionEndpointSupportedRobotTargets");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ControllerArtifactSetDeployments");

            migrationBuilder.RenameIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~2",
                table: "ExecutionEndpointSupportedRobotTargets",
                newName: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~1");

            migrationBuilder.RenameIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~1",
                table: "ExecutionEndpointSupportedRobotTargets",
                newName: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~");

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceProductionJobId",
                table: "ProductionExecutionRecords",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "KioskId",
                table: "Devices",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

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
                name: "IX_KioskConfigurationDeployments_ConfigurationReleaseId",
                table: "KioskConfigurationDeployments",
                column: "ConfigurationReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_DeviceId",
                table: "ExecutionEndpointSupportedRobotTargets",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ControllerArtifactSetDeployments_SourceConfigurationRelease~",
                table: "ControllerArtifactSetDeployments",
                column: "SourceConfigurationReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_DeviceId",
                table: "Alerts",
                column: "DeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_Devices_DeviceId",
                table: "Alerts",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ControllerArtifactSetDeployments_ConfigurationReleases_Sour~",
                table: "ControllerArtifactSetDeployments",
                column: "SourceConfigurationReleaseId",
                principalTable: "ConfigurationReleases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ControllerArtifactSetDeployments_KioskExecutionEndpoints_Ki~",
                table: "ControllerArtifactSetDeployments",
                column: "KioskExecutionEndpointId",
                principalTable: "KioskExecutionEndpoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceEvents_Devices_DeviceId",
                table: "DeviceEvents",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionEndpointReadinessProjections_KioskExecutionEndpoin~",
                table: "ExecutionEndpointReadinessProjections",
                column: "KioskExecutionEndpointId",
                principalTable: "KioskExecutionEndpoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionEndpointSupportedRobotTargets_Devices_DeviceId",
                table: "ExecutionEndpointSupportedRobotTargets",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~",
                table: "ExecutionEndpointSupportedRobotTargets",
                column: "KioskExecutionEndpointId",
                principalTable: "KioskExecutionEndpoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KioskConfigurationDeployments_ConfigurationReleases_Configu~",
                table: "KioskConfigurationDeployments",
                column: "ConfigurationReleaseId",
                principalTable: "ConfigurationReleases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KioskConfigurationDeployments_KioskExecutionEndpoints_Kiosk~",
                table: "KioskConfigurationDeployments",
                column: "KioskExecutionEndpointId",
                principalTable: "KioskExecutionEndpoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderExecutionRecords_EdgeCommands_SourceCommandId",
                table: "OrderExecutionRecords",
                column: "SourceCommandId",
                principalTable: "EdgeCommands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionExecutionRecords_EdgeCommands_SourceCommandId",
                table: "ProductionExecutionRecords",
                column: "SourceCommandId",
                principalTable: "EdgeCommands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

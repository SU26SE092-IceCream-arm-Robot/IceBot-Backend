using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceExecutionEndpointSupportedRobotTargetsWithReportedDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionEndpointSupportedRobotTargets");

            migrationBuilder.AddColumn<int>(
                name: "RuntimeProfileSource",
                table: "RobotAuthoringImports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RuntimeProfileSource",
                table: "RobotArtifacts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReportedDevicesObservedAt",
                table: "KioskExecutionEndpoints",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReportedDevicesReceivedAt",
                table: "KioskExecutionEndpoints",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReportedDevicesSnapshotRevision",
                table: "KioskExecutionEndpoints",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReportedDevicesSourceExecutorId",
                table: "KioskExecutionEndpoints",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExecutionEndpointReportedDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDeviceKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RuntimeTargetCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MachineModelCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionEndpointReportedDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointReportedDevices_Devices_DeviceId_KioskId",
                        columns: x => new { x.DeviceId, x.KioskId },
                        principalTable: "Devices",
                        principalColumns: new[] { "Id", "KioskId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointReportedDevices_KioskExecutionEndpoints_Ki~",
                        columns: x => new { x.KioskExecutionEndpointId, x.KioskId },
                        principalTable: "KioskExecutionEndpoints",
                        principalColumns: new[] { "Id", "KioskId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointReportedDevices_DeviceId_KioskId",
                table: "ExecutionEndpointReportedDevices",
                columns: new[] { "DeviceId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointReportedDevices_KioskExecutionEndpointId_D~",
                table: "ExecutionEndpointReportedDevices",
                columns: new[] { "KioskExecutionEndpointId", "DeviceId" },
                unique: true,
                filter: "\"DeviceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointReportedDevices_KioskExecutionEndpointId_K~",
                table: "ExecutionEndpointReportedDevices",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointReportedDevices_KioskExecutionEndpointId_S~",
                table: "ExecutionEndpointReportedDevices",
                columns: new[] { "KioskExecutionEndpointId", "SourceDeviceKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionEndpointReportedDevices");

            migrationBuilder.DropColumn(
                name: "ReportedDevicesObservedAt",
                table: "KioskExecutionEndpoints");

            migrationBuilder.DropColumn(
                name: "ReportedDevicesReceivedAt",
                table: "KioskExecutionEndpoints");

            migrationBuilder.DropColumn(
                name: "ReportedDevicesSnapshotRevision",
                table: "KioskExecutionEndpoints");

            migrationBuilder.DropColumn(
                name: "ReportedDevicesSourceExecutorId",
                table: "KioskExecutionEndpoints");

            migrationBuilder.DropColumn(
                name: "RuntimeProfileSource",
                table: "RobotAuthoringImports");

            migrationBuilder.DropColumn(
                name: "RuntimeProfileSource",
                table: "RobotArtifacts");

            migrationBuilder.CreateTable(
                name: "ExecutionEndpointSupportedRobotTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    MachineModelCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RuntimeTargetCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionEndpointSupportedRobotTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointSupportedRobotTargets_Devices_DeviceId_Kio~",
                        columns: x => new { x.DeviceId, x.KioskId },
                        principalTable: "Devices",
                        principalColumns: new[] { "Id", "KioskId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~",
                        columns: x => new { x.KioskExecutionEndpointId, x.KioskId },
                        principalTable: "KioskExecutionEndpoints",
                        principalColumns: new[] { "Id", "KioskId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_DeviceId_KioskId",
                table: "ExecutionEndpointSupportedRobotTargets",
                columns: new[] { "DeviceId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~1",
                table: "ExecutionEndpointSupportedRobotTargets",
                columns: new[] { "KioskExecutionEndpointId", "RuntimeTargetCode", "MachineModelCode" },
                unique: true,
                filter: "\"DeviceId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpo~2",
                table: "ExecutionEndpointSupportedRobotTargets",
                columns: new[] { "KioskExecutionEndpointId", "RuntimeTargetCode", "MachineModelCode", "DeviceId" },
                unique: true,
                filter: "\"DeviceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointSupportedRobotTargets_KioskExecutionEndpoi~",
                table: "ExecutionEndpointSupportedRobotTargets",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" });
        }
    }
}

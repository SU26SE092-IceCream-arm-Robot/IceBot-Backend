using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceTicketFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_KioskId_Status_ReportedAt",
                table: "MaintenanceTickets");

            migrationBuilder.DropIndex(
                name: "IX_DeviceEvents_KioskId",
                table: "DeviceEvents");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AssignedAt",
                table: "MaintenanceTickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "MaintenanceTickets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                table: "MaintenanceTickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceEventId",
                table: "MaintenanceTickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "MaintenanceTickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "MaintenanceTickets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "MaintenanceTickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MaintenanceTickets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_DeviceEventId",
                table: "MaintenanceTickets",
                column: "DeviceEventId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_KioskId",
                table: "MaintenanceTickets",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_OrderId",
                table: "MaintenanceTickets",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_OrganizationId",
                table: "MaintenanceTickets",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_OrganizationId_StoreId_KioskId_Status_Re~",
                table: "MaintenanceTickets",
                columns: new[] { "OrganizationId", "StoreId", "KioskId", "Status", "ReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_StoreId",
                table: "MaintenanceTickets",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_KioskId_OccurredAt",
                table: "DeviceEvents",
                columns: new[] { "KioskId", "OccurredAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceTickets_DeviceEvents_DeviceEventId",
                table: "MaintenanceTickets",
                column: "DeviceEventId",
                principalTable: "DeviceEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceTickets_Orders_OrderId",
                table: "MaintenanceTickets",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceTickets_Organizations_OrganizationId",
                table: "MaintenanceTickets",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceTickets_Stores_StoreId",
                table: "MaintenanceTickets",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceTickets_DeviceEvents_DeviceEventId",
                table: "MaintenanceTickets");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceTickets_Orders_OrderId",
                table: "MaintenanceTickets");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceTickets_Organizations_OrganizationId",
                table: "MaintenanceTickets");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceTickets_Stores_StoreId",
                table: "MaintenanceTickets");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_DeviceEventId",
                table: "MaintenanceTickets");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_KioskId",
                table: "MaintenanceTickets");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_OrderId",
                table: "MaintenanceTickets");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_OrganizationId",
                table: "MaintenanceTickets");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_OrganizationId_StoreId_KioskId_Status_Re~",
                table: "MaintenanceTickets");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_StoreId",
                table: "MaintenanceTickets");

            migrationBuilder.DropIndex(
                name: "IX_DeviceEvents_KioskId_OccurredAt",
                table: "DeviceEvents");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "DeviceEventId",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MaintenanceTickets");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_KioskId_Status_ReportedAt",
                table: "MaintenanceTickets",
                columns: new[] { "KioskId", "Status", "ReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceEvents_KioskId",
                table: "DeviceEvents",
                column: "KioskId");
        }
    }
}

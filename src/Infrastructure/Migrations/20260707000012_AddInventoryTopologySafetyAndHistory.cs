using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryTopologySafetyAndHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IngredientDispenserStates_DeviceId_ContainerCode",
                table: "IngredientDispenserStates");

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceBefore",
                table: "StockMovements",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryTopologyChangeRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispenserStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangeType = table.Column<int>(type: "integer", nullable: false),
                    BeforeIsActive = table.Column<bool>(type: "boolean", nullable: true),
                    AfterIsActive = table.Column<bool>(type: "boolean", nullable: true),
                    BeforeCapacityQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    AfterCapacityQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    BeforeUnit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    AfterUnit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTopologyChangeRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTopologyRebindRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDispenserStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplacementDispenserStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplacementDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceIngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplacementIngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceContainerCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReplacementContainerCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EstimateDisposition = table.Column<int>(type: "integer", nullable: false),
                    PreviousEstimatedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    TransferredQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SourceUnit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReplacementUnit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTopologyRebindRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTopologyRebindRecords_IngredientDispenserStates_Re~",
                        column: x => x.ReplacementDispenserStateId,
                        principalTable: "IngredientDispenserStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTopologyRebindRecords_IngredientDispenserStates_So~",
                        column: x => x.SourceDispenserStateId,
                        principalTable: "IngredientDispenserStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IngredientDispenserStates_DeviceId_ContainerCode",
                table: "IngredientDispenserStates",
                columns: new[] { "DeviceId", "ContainerCode" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTopologyChangeRecords_DispenserStateId_CreatedAt",
                table: "InventoryTopologyChangeRecords",
                columns: new[] { "DispenserStateId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTopologyChangeRecords_KioskId_CreatedAt",
                table: "InventoryTopologyChangeRecords",
                columns: new[] { "KioskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTopologyRebindRecords_KioskId_CreatedAt",
                table: "InventoryTopologyRebindRecords",
                columns: new[] { "KioskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTopologyRebindRecords_ReplacementDispenserStateId",
                table: "InventoryTopologyRebindRecords",
                column: "ReplacementDispenserStateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTopologyRebindRecords_SourceDispenserStateId",
                table: "InventoryTopologyRebindRecords",
                column: "SourceDispenserStateId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryTopologyChangeRecords");

            migrationBuilder.DropTable(
                name: "InventoryTopologyRebindRecords");

            migrationBuilder.DropIndex(
                name: "IX_IngredientDispenserStates_DeviceId_ContainerCode",
                table: "IngredientDispenserStates");

            migrationBuilder.DropColumn(
                name: "BalanceBefore",
                table: "StockMovements");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientDispenserStates_DeviceId_ContainerCode",
                table: "IngredientDispenserStates",
                columns: new[] { "DeviceId", "ContainerCode" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }
    }
}

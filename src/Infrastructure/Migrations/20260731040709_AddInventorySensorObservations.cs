using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventorySensorObservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventorySensorObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceExecutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientDispenserStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservationSequence = table.Column<long>(type: "bigint", nullable: false),
                    ObservedLevelStatus = table.Column<int>(type: "integer", nullable: false),
                    DerivedEstimatedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Disposition = table.Column<int>(type: "integer", nullable: false),
                    SensorPayloadJson = table.Column<string>(type: "jsonb", maxLength: 16384, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySensorObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventorySensorObservations_IngredientDispenserStates_Ingre~",
                        column: x => x.IngredientDispenserStateId,
                        principalTable: "IngredientDispenserStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventorySensorObservations_KioskExecutionEndpoints_KioskEx~",
                        column: x => x.KioskExecutionEndpointId,
                        principalTable: "KioskExecutionEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySensorObservations_IngredientDispenserStateId_Clou~",
                table: "InventorySensorObservations",
                columns: new[] { "IngredientDispenserStateId", "CloudReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySensorObservations_KioskExecutionEndpointId",
                table: "InventorySensorObservations",
                column: "KioskExecutionEndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySensorObservations_OriginNodeId_Version",
                table: "InventorySensorObservations",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySensorObservations_SourceExecutorId_IngredientDisp~",
                table: "InventorySensorObservations",
                columns: new[] { "SourceExecutorId", "IngredientDispenserStateId", "ObservationSequence" });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySensorObservations_SourceExecutorId_SourceEventId",
                table: "InventorySensorObservations",
                columns: new[] { "SourceExecutorId", "SourceEventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventorySensorObservations");
        }
    }
}

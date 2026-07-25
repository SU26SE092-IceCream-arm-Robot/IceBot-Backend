using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceProductionJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumberSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProductVariantNameSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProductionUnitNo = table.Column<int>(type: "integer", nullable: false),
                    ProductionUnitQuantity = table.Column<int>(type: "integer", nullable: false),
                    Trigger = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PhysicalOutputState = table.Column<int>(type: "integer", nullable: false),
                    InspectionOutcome = table.Column<int>(type: "integer", nullable: true),
                    Resolution = table.Column<int>(type: "integer", nullable: true),
                    ErrorCodeSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ErrorMessageSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OpenedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    InspectedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    InspectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolutionRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionRequestFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RelatedEdgeCommandId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedRefundId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionIncidents_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionIncidents_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionIncidentHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionIncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: true),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionIncidentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionIncidentHistories_ProductionIncidents_ProductionI~",
                        column: x => x.ProductionIncidentId,
                        principalTable: "ProductionIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionIncidentHistories_ProductionIncidentId_OccurredAt",
                table: "ProductionIncidentHistories",
                columns: new[] { "ProductionIncidentId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionIncidents_OrderId_OrderItemId_ProductionUnitNo",
                table: "ProductionIncidents",
                columns: new[] { "OrderId", "OrderItemId", "ProductionUnitNo" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionIncidents_OrderItemId",
                table: "ProductionIncidents",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionIncidents_OrganizationId_StoreId_KioskId_Status_C~",
                table: "ProductionIncidents",
                columns: new[] { "OrganizationId", "StoreId", "KioskId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionIncidents_ResolutionRequestId",
                table: "ProductionIncidents",
                column: "ResolutionRequestId",
                unique: true,
                filter: "\"ResolutionRequestId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionIncidents_SourceCommandId_SourceProductionJobId",
                table: "ProductionIncidents",
                columns: new[] { "SourceCommandId", "SourceProductionJobId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionIncidentHistories");

            migrationBuilder.DropTable(
                name: "ProductionIncidents");
        }
    }
}

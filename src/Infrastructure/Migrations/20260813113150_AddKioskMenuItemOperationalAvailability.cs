using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKioskMenuItemOperationalAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KioskMenuItemAvailabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    ReasonCode = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KioskMenuItemAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KioskMenuItemAvailabilities_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskMenuItemAvailabilities_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskMenuItemAvailabilities_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KioskMenuItemAvailabilityTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailabilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromState = table.Column<int>(type: "integer", nullable: false),
                    ToState = table.Column<int>(type: "integer", nullable: false),
                    ReasonCode = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCodeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AvailabilityRevision = table.Column<long>(type: "bigint", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KioskMenuItemAvailabilityTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KioskMenuItemAvailabilityTransitions_KioskMenuItemAvailabil~",
                        column: x => x.AvailabilityId,
                        principalTable: "KioskMenuItemAvailabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KioskMenuItemAvailabilities_KioskId_MenuItemId",
                table: "KioskMenuItemAvailabilities",
                columns: new[] { "KioskId", "MenuItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KioskMenuItemAvailabilities_KioskId_State",
                table: "KioskMenuItemAvailabilities",
                columns: new[] { "KioskId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskMenuItemAvailabilities_MenuId",
                table: "KioskMenuItemAvailabilities",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskMenuItemAvailabilities_MenuItemId",
                table: "KioskMenuItemAvailabilities",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskMenuItemAvailabilities_OrganizationId",
                table: "KioskMenuItemAvailabilities",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskMenuItemAvailabilityTransitions_AvailabilityId_Occurre~",
                table: "KioskMenuItemAvailabilityTransitions",
                columns: new[] { "AvailabilityId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskMenuItemAvailabilityTransitions_AvailabilityId_Request~",
                table: "KioskMenuItemAvailabilityTransitions",
                columns: new[] { "AvailabilityId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KioskMenuItemAvailabilityTransitions_OrganizationId",
                table: "KioskMenuItemAvailabilityTransitions",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KioskMenuItemAvailabilityTransitions");

            migrationBuilder.DropTable(
                name: "KioskMenuItemAvailabilities");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogImageCleanupAndReplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogImageCleanups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogImageAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicIdSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogImageCleanups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogImageCleanups_CatalogImageAssets_CatalogImageAssetId",
                        column: x => x.CatalogImageAssetId,
                        principalTable: "CatalogImageAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CatalogImageOperationReplays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OwnerType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogImageOperationReplays", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImageCleanups_CatalogImageAssetId",
                table: "CatalogImageCleanups",
                column: "CatalogImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImageCleanups_CompletedAt_NextAttemptAt",
                table: "CatalogImageCleanups",
                columns: new[] { "CompletedAt", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImageOperationReplays_ScopeKey_OwnerType_OwnerId_Ope~",
                table: "CatalogImageOperationReplays",
                columns: new[] { "ScopeKey", "OwnerType", "OwnerId", "Operation", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogImageCleanups");

            migrationBuilder.DropTable(
                name: "CatalogImageOperationReplays");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceClientDeviceId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClientDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InstallationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AppVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Platform = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CredentialVersion = table.Column<int>(type: "integer", nullable: false),
                    SessionVersion = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DisabledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientDevices", x => x.Id);
                    table.CheckConstraint("CK_ClientDevices_PositiveVersions", "\"CredentialVersion\" > 0 AND \"SessionVersion\" > 0 AND \"Revision\" > 0");
                    table.ForeignKey(
                        name: "FK_ClientDevices_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientDevices_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientDevices_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientDeviceCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    SecretHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    HashKeyVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RotatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientDeviceCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientDeviceCredentials_ClientDevices_ClientDeviceId",
                        column: x => x.ClientDeviceId,
                        principalTable: "ClientDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientDeviceOperationReplays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientDeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Operation = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResultClientDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientDeviceOperationReplays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientDeviceOperationReplays_ClientDevices_ClientDeviceId",
                        column: x => x.ClientDeviceId,
                        principalTable: "ClientDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientDeviceOperationReplays_ClientDevices_ResultClientDevi~",
                        column: x => x.ResultClientDeviceId,
                        principalTable: "ClientDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientDeviceOperationReplays_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SourceClientDeviceId",
                table: "Orders",
                column: "SourceClientDeviceId");

            migrationBuilder.Sql("""
                INSERT INTO "ClientDevices" (
                    "Id", "OrganizationId", "StoreId", "KioskId", "Type", "Status", "InstallationId",
                    "DisplayName", "CredentialVersion", "SessionVersion", "Revision", "RetiredAt", "CreatedAt")
                SELECT DISTINCT
                    (
                        substring(md5('historical-client-device:' || kiosk."Id"::text), 1, 8) || '-' ||
                        substring(md5('historical-client-device:' || kiosk."Id"::text), 9, 4) || '-' ||
                        substring(md5('historical-client-device:' || kiosk."Id"::text), 13, 4) || '-' ||
                        substring(md5('historical-client-device:' || kiosk."Id"::text), 17, 4) || '-' ||
                        substring(md5('historical-client-device:' || kiosk."Id"::text), 21, 12)
                    )::uuid,
                    kiosk."OrganizationId", kiosk."StoreId", kiosk."Id", 1, 3,
                    (
                        substring(md5('historical-client-installation:' || kiosk."Id"::text), 1, 8) || '-' ||
                        substring(md5('historical-client-installation:' || kiosk."Id"::text), 9, 4) || '-' ||
                        substring(md5('historical-client-installation:' || kiosk."Id"::text), 13, 4) || '-' ||
                        substring(md5('historical-client-installation:' || kiosk."Id"::text), 17, 4) || '-' ||
                        substring(md5('historical-client-installation:' || kiosk."Id"::text), 21, 12)
                    )::uuid,
                    'Historical tablet order source', 1, 1, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                FROM "Orders" AS "order"
                INNER JOIN "Kiosks" AS kiosk ON kiosk."Id" = "order"."KioskId"
                WHERE "order"."Channel" = 1
                  AND "order"."SourceClientDeviceId" IS NULL
                ON CONFLICT ("Id") DO NOTHING;

                UPDATE "Orders" AS "order"
                SET "SourceClientDeviceId" = (
                    substring(md5('historical-client-device:' || "order"."KioskId"::text), 1, 8) || '-' ||
                    substring(md5('historical-client-device:' || "order"."KioskId"::text), 9, 4) || '-' ||
                    substring(md5('historical-client-device:' || "order"."KioskId"::text), 13, 4) || '-' ||
                    substring(md5('historical-client-device:' || "order"."KioskId"::text), 17, 4) || '-' ||
                    substring(md5('historical-client-device:' || "order"."KioskId"::text), 21, 12)
                )::uuid
                WHERE "order"."Channel" = 1
                  AND "order"."SourceClientDeviceId" IS NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_TabletRequiresClientDevice",
                table: "Orders",
                sql: "\"Channel\" <> 1 OR \"SourceClientDeviceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClientDeviceCredentials_ClientDeviceId",
                table: "ClientDeviceCredentials",
                column: "ClientDeviceId",
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ClientDeviceCredentials_ClientDeviceId_Version",
                table: "ClientDeviceCredentials",
                columns: new[] { "ClientDeviceId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientDeviceOperationReplays_ClientDeviceId_Operation_Idemp~",
                table: "ClientDeviceOperationReplays",
                columns: new[] { "ClientDeviceId", "Operation", "IdempotencyKey" },
                unique: true,
                filter: "\"ClientDeviceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClientDeviceOperationReplays_KioskId_Operation_IdempotencyK~",
                table: "ClientDeviceOperationReplays",
                columns: new[] { "KioskId", "Operation", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientDeviceOperationReplays_ResultClientDeviceId",
                table: "ClientDeviceOperationReplays",
                column: "ResultClientDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientDevices_InstallationId",
                table: "ClientDevices",
                column: "InstallationId",
                unique: true,
                filter: "\"Status\" <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_ClientDevices_KioskId_Status",
                table: "ClientDevices",
                columns: new[] { "KioskId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientDevices_KioskId_Type",
                table: "ClientDevices",
                columns: new[] { "KioskId", "Type" },
                unique: true,
                filter: "\"Type\" = 1 AND \"Status\" <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_ClientDevices_OrganizationId",
                table: "ClientDevices",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientDevices_StoreId",
                table: "ClientDevices",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ClientDevices_SourceClientDeviceId",
                table: "Orders",
                column: "SourceClientDeviceId",
                principalTable: "ClientDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ClientDevices_SourceClientDeviceId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "ClientDeviceCredentials");

            migrationBuilder.DropTable(
                name: "ClientDeviceOperationReplays");

            migrationBuilder.DropTable(
                name: "ClientDevices");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SourceClientDeviceId",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_TabletRequiresClientDevice",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SourceClientDeviceId",
                table: "Orders");
        }
    }
}

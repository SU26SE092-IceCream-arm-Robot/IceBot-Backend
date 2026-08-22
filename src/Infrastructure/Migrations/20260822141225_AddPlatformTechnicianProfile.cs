using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformTechnicianProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformTechnicianProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformTechnicianProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformTechnicianProfiles_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformTechnicianProfiles_AccountId",
                table: "PlatformTechnicianProfiles",
                column: "AccountId",
                unique: true);

            // Kiosk-scoped grants historically carried the parent StoreId as denormalized
            // context. The final support-scope model stores exactly one concrete target.
            migrationBuilder.Sql("""
                UPDATE "AccountRoles" ar
                SET "StoreId" = NULL
                FROM "Roles" r, "Kiosks" k
                WHERE ar."RoleId" = r."Id"
                  AND r."Code" = 'Technician'
                  AND ar."IsActive" = TRUE
                  AND ar."StoreId" IS NOT NULL
                  AND ar."KioskId" = k."Id"
                  AND k."StoreId" = ar."StoreId"
                  AND k."OrganizationId" = ar."OrganizationId";
                """);

            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    conflicting_accounts text;
                BEGIN
                    SELECT string_agg(a."Id"::text, ', ' ORDER BY a."Id"::text)
                    INTO conflicting_accounts
                    FROM "Accounts" a
                    WHERE a."DeletedAt" IS NULL
                      AND EXISTS (
                        SELECT 1 FROM "AccountRoles" ar JOIN "Roles" r ON r."Id" = ar."RoleId"
                        WHERE ar."AccountId" = a."Id" AND ar."IsActive" = TRUE AND r."Code" = 'Technician')
                      AND EXISTS (
                        SELECT 1 FROM "AccountRoles" ar JOIN "Roles" r ON r."Id" = ar."RoleId"
                        WHERE ar."AccountId" = a."Id" AND ar."IsActive" = TRUE AND r."Code" <> 'Technician');

                    IF conflicting_accounts IS NOT NULL THEN
                        RAISE EXCEPTION 'Technician accounts contain mixed active roles: %', conflicting_accounts;
                    END IF;

                    SELECT string_agg(ar."AccountId"::text, ', ' ORDER BY ar."AccountId"::text)
                    INTO conflicting_accounts
                    FROM "AccountRoles" ar
                    JOIN "Roles" r ON r."Id" = ar."RoleId"
                    WHERE ar."IsActive" = TRUE
                      AND r."Code" = 'Technician'
                      AND (ar."OrganizationId" IS NULL OR
                           ((ar."StoreId" IS NOT NULL)::int + (ar."KioskId" IS NOT NULL)::int) <> 1);

                    IF conflicting_accounts IS NOT NULL THEN
                        RAISE EXCEPTION 'Technician grants must contain OrganizationId and exactly one StoreId or KioskId. Accounts: %', conflicting_accounts;
                    END IF;
                END $$;
                """);

            // Convert valid pre-profile Technician identities to the final model.
            migrationBuilder.Sql("""
                INSERT INTO "PlatformTechnicianProfiles" ("Id", "AccountId", "CreatedAt")
                SELECT (
                    substr(md5(a."Id"::text), 1, 8) || '-' ||
                    substr(md5(a."Id"::text), 9, 4) || '-' ||
                    substr(md5(a."Id"::text), 13, 4) || '-' ||
                    substr(md5(a."Id"::text), 17, 4) || '-' ||
                    substr(md5(a."Id"::text), 21, 12)
                )::uuid, a."Id", NOW()
                FROM "Accounts" a
                WHERE a."DeletedAt" IS NULL
                  AND EXISTS (
                    SELECT 1 FROM "AccountRoles" ar JOIN "Roles" r ON r."Id" = ar."RoleId"
                    WHERE ar."AccountId" = a."Id" AND ar."IsActive" = TRUE AND r."Code" = 'Technician')
                  AND NOT EXISTS (
                    SELECT 1 FROM "AccountRoles" ar JOIN "Roles" r ON r."Id" = ar."RoleId"
                    WHERE ar."AccountId" = a."Id" AND ar."IsActive" = TRUE AND r."Code" <> 'Technician');
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformTechnicianProfiles");
        }
    }
}

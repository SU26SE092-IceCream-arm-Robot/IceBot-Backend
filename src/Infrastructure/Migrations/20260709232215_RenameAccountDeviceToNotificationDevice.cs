using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameAccountDeviceToNotificationDevice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_AccountDevices_AccountDeviceId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_AccountDeviceId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "AccountDeviceId",
                table: "RefreshTokens");

            migrationBuilder.RenameTable(
                name: "AccountDevices",
                newName: "AccountNotificationDevices");

            migrationBuilder.RenameColumn(
                name: "DeviceTokenHash",
                table: "AccountNotificationDevices",
                newName: "PushTokenHash");

            migrationBuilder.DropIndex(
                name: "IX_AccountDevices_AccountId_DeviceTokenHash",
                table: "AccountNotificationDevices");

            migrationBuilder.DropColumn(
                name: "IsTrusted",
                table: "AccountNotificationDevices");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "AccountNotificationDevices");

            migrationBuilder.AddColumn<Guid>(
                name: "InstallationId",
                table: "AccountNotificationDevices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InvalidatedAt",
                table: "AccountNotificationDevices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvalidationReason",
                table: "AccountNotificationDevices",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PushToken",
                table: "AccountNotificationDevices",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.Sql("UPDATE \"AccountNotificationDevices\" SET \"InstallationId\" = \"Id\" WHERE \"InstallationId\" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "InstallationId",
                table: "AccountNotificationDevices",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountNotificationDevices_AccountId_InstallationId",
                table: "AccountNotificationDevices",
                columns: new[] { "AccountId", "InstallationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountNotificationDevices_AccountId_InvalidatedAt",
                table: "AccountNotificationDevices",
                columns: new[] { "AccountId", "InvalidatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountNotificationDevices_PushTokenHash",
                table: "AccountNotificationDevices",
                column: "PushTokenHash",
                unique: true,
                filter: "\"PushTokenHash\" IS NOT NULL AND \"InvalidatedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountNotificationDevices_AccountId_InstallationId",
                table: "AccountNotificationDevices");

            migrationBuilder.DropIndex(
                name: "IX_AccountNotificationDevices_AccountId_InvalidatedAt",
                table: "AccountNotificationDevices");

            migrationBuilder.DropIndex(
                name: "IX_AccountNotificationDevices_PushTokenHash",
                table: "AccountNotificationDevices");

            migrationBuilder.DropColumn(
                name: "InstallationId",
                table: "AccountNotificationDevices");

            migrationBuilder.DropColumn(
                name: "InvalidatedAt",
                table: "AccountNotificationDevices");

            migrationBuilder.DropColumn(
                name: "InvalidationReason",
                table: "AccountNotificationDevices");

            migrationBuilder.AlterColumn<string>(
                name: "PushToken",
                table: "AccountNotificationDevices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4096)",
                oldMaxLength: 4096,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTrusted",
                table: "AccountNotificationDevices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevokedAt",
                table: "AccountNotificationDevices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.RenameColumn(
                name: "PushTokenHash",
                table: "AccountNotificationDevices",
                newName: "DeviceTokenHash");

            migrationBuilder.RenameTable(
                name: "AccountNotificationDevices",
                newName: "AccountDevices");

            migrationBuilder.AddColumn<Guid>(
                name: "AccountDeviceId",
                table: "RefreshTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AccountDeviceId",
                table: "RefreshTokens",
                column: "AccountDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountDevices_AccountId_DeviceTokenHash",
                table: "AccountDevices",
                columns: new[] { "AccountId", "DeviceTokenHash" });

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_AccountDevices_AccountDeviceId",
                table: "RefreshTokens",
                column: "AccountDeviceId",
                principalTable: "AccountDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

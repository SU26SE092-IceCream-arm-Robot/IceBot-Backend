using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertCorrelationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationKey",
                table: "Alerts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastOccurredAt",
                table: "Alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OccurrenceCount",
                table: "Alerts",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Alerts"
                SET "CorrelationKey" = UPPER(BTRIM("AlertCode")),
                    "LastOccurredAt" = "RaisedAt",
                    "OccurrenceCount" = 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationKey",
                table: "Alerts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastOccurredAt",
                table: "Alerts",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OccurrenceCount",
                table: "Alerts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_KioskId_DeviceId_CorrelationKey_Status_LastOccurredAt",
                table: "Alerts",
                columns: new[] { "KioskId", "DeviceId", "CorrelationKey", "Status", "LastOccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Alerts_KioskId_DeviceId_CorrelationKey_Status_LastOccurredAt",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "CorrelationKey",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "LastOccurredAt",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "OccurrenceCount",
                table: "Alerts");
        }
    }
}

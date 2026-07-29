using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteLocalOperationalChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SalesPauseReason",
                table: "Stores",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SalesPausedAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalesPausedByAccountId",
                table: "Stores",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SalesPausedUntil",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SalesResumedAt",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalesResumedByAccountId",
                table: "Stores",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SettlementDisposition",
                table: "PaymentTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaymentDeadlineAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusBeforeDuplicatePaymentIntervention",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OperationalImpact",
                table: "MaintenanceTickets",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OperationalState",
                table: "Kiosks",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OperationalStateChangedAt",
                table: "Kiosks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperationalStateChangedByAccountId",
                table: "Kiosks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationalStateReason",
                table: "Kiosks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KioskOperationalStateTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromState = table.Column<int>(type: "integer", nullable: false),
                    ToState = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceMaintenanceTicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KioskOperationalStateTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KioskOperationalStateTransitions_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskOperationalStateTransitions_MaintenanceTickets_SourceM~",
                        column: x => x.SourceMaintenanceTicketId,
                        principalTable: "MaintenanceTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            CompleteLocalOperationalChangesManualSteps.BackfillPaymentSettlementAndOrderDeadline(migrationBuilder);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "PaymentDeadlineAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_OrderId_PrimarySettlement",
                table: "PaymentTransactions",
                column: "OrderId",
                unique: true,
                filter: "\"SettlementDisposition\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_KioskOperationalStateTransitions_KioskId_ChangedAt",
                table: "KioskOperationalStateTransitions",
                columns: new[] { "KioskId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskOperationalStateTransitions_SourceMaintenanceTicketId",
                table: "KioskOperationalStateTransitions",
                column: "SourceMaintenanceTicketId");

            CompleteLocalOperationalChangesManualSteps.MigrateLegacyKioskMaintenanceState(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CompleteLocalOperationalChangesManualSteps.RestoreLegacyKioskMaintenanceState(migrationBuilder);

            migrationBuilder.DropTable(
                name: "KioskOperationalStateTransitions");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_OrderId_PrimarySettlement",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "SalesPauseReason",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "SalesPausedAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "SalesPausedByAccountId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "SalesPausedUntil",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "SalesResumedAt",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "SalesResumedByAccountId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "SettlementDisposition",
                table: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentDeadlineAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StatusBeforeDuplicatePaymentIntervention",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OperationalImpact",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "OperationalState",
                table: "Kiosks");

            migrationBuilder.DropColumn(
                name: "OperationalStateChangedAt",
                table: "Kiosks");

            migrationBuilder.DropColumn(
                name: "OperationalStateChangedByAccountId",
                table: "Kiosks");

            migrationBuilder.DropColumn(
                name: "OperationalStateReason",
                table: "Kiosks");
        }
    }
}

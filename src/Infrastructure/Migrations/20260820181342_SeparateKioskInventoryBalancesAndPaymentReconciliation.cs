using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeparateKioskInventoryBalancesAndPaymentReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "IngredientDispenserStateId",
                table: "StockMovements",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "KioskIngredientInventoryId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompensationMethod",
                table: "Refunds",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("""
                UPDATE "Refunds"
                SET "CompensationMethod" = CASE
                WHEN "Reason" ~ '"Method"\s*:\s*"Voucher"' THEN 2
                    ELSE 1
                END;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "KioskIngredientInventoryId",
                table: "IngredientDispenserStates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LastObservedEstimatedQuantity",
                table: "IngredientDispenserStates",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SensorRebaselineRefillTaskId",
                table: "IngredientDispenserStates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SensorRebaselineRequestedAt",
                table: "IngredientDispenserStates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SensorRebaselineRequired",
                table: "IngredientDispenserStates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "KioskIngredientInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EstimatedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    LowStockThreshold = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TrackingMode = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastMeasuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSensorReconciledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KioskIngredientInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KioskIngredientInventories_Ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "Ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KioskIngredientInventories_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentProviderObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderOrderCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    ObservedStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ObservedAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ObservedPaidAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ProviderTransactionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProviderObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentProviderObservations_PaymentTransactions_PaymentTran~",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryReconciliationCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskIngredientInventoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AppliedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryReconciliationCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryReconciliationCases_KioskIngredientInventories_Kio~",
                        column: x => x.KioskIngredientInventoryId,
                        principalTable: "KioskIngredientInventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryRefillTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskIngredientInventoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientDispenserStateId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceAlertId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestSource = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ActualQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExternalLotReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestIdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryRefillTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryRefillTasks_Accounts_CancelledByAccountId",
                        column: x => x.CancelledByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryRefillTasks_Accounts_CompletedByAccountId",
                        column: x => x.CompletedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryRefillTasks_Accounts_RequestedByAccountId",
                        column: x => x.RequestedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryRefillTasks_Accounts_StartedByAccountId",
                        column: x => x.StartedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryRefillTasks_Alerts_SourceAlertId",
                        column: x => x.SourceAlertId,
                        principalTable: "Alerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryRefillTasks_IngredientDispenserStates_IngredientDi~",
                        column: x => x.IngredientDispenserStateId,
                        principalTable: "IngredientDispenserStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryRefillTasks_KioskIngredientInventories_KioskIngred~",
                        column: x => x.KioskIngredientInventoryId,
                        principalTable: "KioskIngredientInventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryRefillTaskTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryRefillTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: true),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorRoleCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ActorOrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorStoreId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorKioskId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActualQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    RequestIdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_InventoryRefillTaskTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryRefillTaskTransitions_Accounts_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryRefillTaskTransitions_InventoryRefillTasks_Invento~",
                        column: x => x.InventoryRefillTaskId,
                        principalTable: "InventoryRefillTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_KioskIngredientInventoryId",
                table: "StockMovements",
                column: "KioskIngredientInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientDispenserStates_KioskIngredientInventoryId",
                table: "IngredientDispenserStates",
                column: "KioskIngredientInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_KioskId_SourceType_SourceId_CorrelationKey",
                table: "Alerts",
                columns: new[] { "KioskId", "SourceType", "SourceId", "CorrelationKey" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"Status\" IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReconciliationCases_KioskIngredientInventoryId",
                table: "InventoryReconciliationCases",
                column: "KioskIngredientInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReconciliationCases_OriginNodeId_Version",
                table: "InventoryReconciliationCases",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReconciliationCases_SourceEventId_IngredientId_Uni~",
                table: "InventoryReconciliationCases",
                columns: new[] { "SourceEventId", "IngredientId", "Unit", "ReasonCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRefillTasks_CancelledByAccountId",
                table: "InventoryRefillTasks",
                column: "CancelledByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRefillTasks_CompletedByAccountId",
                table: "InventoryRefillTasks",
                column: "CompletedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRefillTasks_IngredientDispenserStateId",
                table: "InventoryRefillTasks",
                column: "IngredientDispenserStateId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRefillTasks_KioskId_RequestIdempotencyKey",
                table: "InventoryRefillTasks",
                columns: new[] { "KioskId", "RequestIdempotencyKey" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRefillTasks_KioskId_Status_RequestedAt",
                table: "InventoryRefillTasks",
                columns: new[] { "KioskId", "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRefillTasks_KioskIngredientInventoryId",
                table: "InventoryRefillTasks",
                column: "KioskIngredientInventoryId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"Status\" IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRefillTasks_OriginNodeId_Version",
                table: "InventoryRefillTasks",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRefillTasks_RequestedByAccountId",
                table: "InventoryRefillTasks",
                column: "RequestedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRefillTasks_SourceAlertId",
                table: "InventoryRefillTasks",
                column: "SourceAlertId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRefillTasks_StartedByAccountId",
                table: "InventoryRefillTasks",
                column: "StartedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRefillTaskTransitions_ActorAccountId",
                table: "InventoryRefillTaskTransitions",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRefillTaskTransitions_InventoryRefillTaskId_Reques~",
                table: "InventoryRefillTaskTransitions",
                columns: new[] { "InventoryRefillTaskId", "RequestIdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRefillTaskTransitions_OriginNodeId_Version",
                table: "InventoryRefillTaskTransitions",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_KioskIngredientInventories_IngredientId",
                table: "KioskIngredientInventories",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_KioskIngredientInventories_KioskId_IngredientId_Unit",
                table: "KioskIngredientInventories",
                columns: new[] { "KioskId", "IngredientId", "Unit" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KioskIngredientInventories_OriginNodeId_Version",
                table: "KioskIngredientInventories",
                columns: new[] { "OriginNodeId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderObservations_PaymentTransactionId_CloudRecei~",
                table: "PaymentProviderObservations",
                columns: new[] { "PaymentTransactionId", "CloudReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderObservations_Provider_ProviderOrderCode_Clou~",
                table: "PaymentProviderObservations",
                columns: new[] { "Provider", "ProviderOrderCode", "CloudReceivedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_IngredientDispenserStates_KioskIngredientInventories_KioskI~",
                table: "IngredientDispenserStates",
                column: "KioskIngredientInventoryId",
                principalTable: "KioskIngredientInventories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_KioskIngredientInventories_KioskIngredientIn~",
                table: "StockMovements",
                column: "KioskIngredientInventoryId",
                principalTable: "KioskIngredientInventories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IngredientDispenserStates_KioskIngredientInventories_KioskI~",
                table: "IngredientDispenserStates");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_KioskIngredientInventories_KioskIngredientIn~",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "InventoryReconciliationCases");

            migrationBuilder.DropTable(
                name: "InventoryRefillTaskTransitions");

            migrationBuilder.DropTable(
                name: "PaymentProviderObservations");

            migrationBuilder.DropTable(
                name: "InventoryRefillTasks");

            migrationBuilder.DropTable(
                name: "KioskIngredientInventories");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_KioskIngredientInventoryId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_IngredientDispenserStates_KioskIngredientInventoryId",
                table: "IngredientDispenserStates");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_KioskId_SourceType_SourceId_CorrelationKey",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "KioskIngredientInventoryId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "CompensationMethod",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "KioskIngredientInventoryId",
                table: "IngredientDispenserStates");

            migrationBuilder.DropColumn(
                name: "LastObservedEstimatedQuantity",
                table: "IngredientDispenserStates");

            migrationBuilder.DropColumn(
                name: "SensorRebaselineRefillTaskId",
                table: "IngredientDispenserStates");

            migrationBuilder.DropColumn(
                name: "SensorRebaselineRequestedAt",
                table: "IngredientDispenserStates");

            migrationBuilder.DropColumn(
                name: "SensorRebaselineRequired",
                table: "IngredientDispenserStates");

            migrationBuilder.AlterColumn<Guid>(
                name: "IngredientDispenserStateId",
                table: "StockMovements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}

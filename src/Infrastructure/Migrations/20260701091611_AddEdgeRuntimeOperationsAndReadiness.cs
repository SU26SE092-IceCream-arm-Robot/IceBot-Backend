using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgeRuntimeOperationsAndReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncEventInbox_EventId",
                table: "SyncEventInbox");

            migrationBuilder.AddColumn<long>(
                name: "SequenceNumber",
                table: "SyncEventInbox",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EdgeStateSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceExecutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SummaryKind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StateRevision = table.Column<long>(type: "bigint", nullable: false),
                    SummarySchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    EdgeCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EdgeStateSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EdgeStateSummaries_KioskExecutionEndpoints_KioskExecutionEn~",
                        columns: x => new { x.KioskExecutionEndpointId, x.KioskId },
                        principalTable: "KioskExecutionEndpoints",
                        principalColumns: new[] { "Id", "KioskId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EdgeStateSummaries_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionEndpointMqttCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BrokerProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CredentialVersion = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionEndpointMqttCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointMqttCredentials_KioskExecutionEndpoints_Ki~",
                        column: x => x.KioskExecutionEndpointId,
                        principalTable: "KioskExecutionEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionEndpointReadinessProjections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceExecutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StateRevision = table.Column<long>(type: "bigint", nullable: false),
                    Readiness = table.Column<int>(type: "integer", nullable: false),
                    Activity = table.Column<int>(type: "integer", nullable: false),
                    Safety = table.Column<int>(type: "integer", nullable: false),
                    CurrentCommandId = table.Column<Guid>(type: "uuid", nullable: true),
                    PhysicalOutputState = table.Column<int>(type: "integer", nullable: false),
                    FaultCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExecutorReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CloudReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionEndpointReadinessProjections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointReadinessProjections_KioskExecutionEndpoin~",
                        column: x => x.KioskExecutionEndpointId,
                        principalTable: "KioskExecutionEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointReadinessProjections_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionEventCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskId = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceExecutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastContiguousSequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    LastContiguousEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionEventCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionEventCheckpoints_KioskExecutionEndpoints_KioskExe~",
                        columns: x => new { x.KioskExecutionEndpointId, x.KioskId },
                        principalTable: "KioskExecutionEndpoints",
                        principalColumns: new[] { "Id", "KioskId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionEventCheckpoints_Kiosks_KioskId",
                        column: x => x.KioskId,
                        principalTable: "Kiosks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyncDeadLetterRetryAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SyncDeadLetterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    RequestedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: true),
                    ResultMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncDeadLetterRetryAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncDeadLetterRetryAttempts_Accounts_RequestedByAccountId",
                        column: x => x.RequestedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncDeadLetterRetryAttempts_SyncDeadLetters_SyncDeadLetterId",
                        column: x => x.SyncDeadLetterId,
                        principalTable: "SyncDeadLetters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionEndpointCapabilityProjections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionEndpointReadinessProjectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapabilityCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WorkcellCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    UnavailableReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionEndpointCapabilityProjections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointCapabilityProjections_ExecutionEndpointRea~",
                        column: x => x.ExecutionEndpointReadinessProjectionId,
                        principalTable: "ExecutionEndpointReadinessProjections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncEventInbox_SourceNodeId_EventId",
                table: "SyncEventInbox",
                columns: new[] { "SourceNodeId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncEventInbox_SourceNodeId_SequenceNumber",
                table: "SyncEventInbox",
                columns: new[] { "SourceNodeId", "SequenceNumber" },
                unique: true,
                filter: "\"SequenceNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EdgeStateSummaries_KioskExecutionEndpointId_KioskId",
                table: "EdgeStateSummaries",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_EdgeStateSummaries_KioskId",
                table: "EdgeStateSummaries",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_EdgeStateSummaries_SourceExecutorId_SummaryKind",
                table: "EdgeStateSummaries",
                columns: new[] { "SourceExecutorId", "SummaryKind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointCapabilityProjections_ExecutionEndpointRea~",
                table: "ExecutionEndpointCapabilityProjections",
                columns: new[] { "ExecutionEndpointReadinessProjectionId", "CapabilityCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointMqttCredentials_KioskExecutionEndpointId",
                table: "ExecutionEndpointMqttCredentials",
                column: "KioskExecutionEndpointId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointMqttCredentials_Username",
                table: "ExecutionEndpointMqttCredentials",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointReadinessProjections_KioskExecutionEndpoin~",
                table: "ExecutionEndpointReadinessProjections",
                column: "KioskExecutionEndpointId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointReadinessProjections_KioskId_Readiness_Act~",
                table: "ExecutionEndpointReadinessProjections",
                columns: new[] { "KioskId", "Readiness", "Activity" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEventCheckpoints_KioskExecutionEndpointId_KioskId",
                table: "ProductionEventCheckpoints",
                columns: new[] { "KioskExecutionEndpointId", "KioskId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEventCheckpoints_KioskId",
                table: "ProductionEventCheckpoints",
                column: "KioskId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEventCheckpoints_SourceExecutorId",
                table: "ProductionEventCheckpoints",
                column: "SourceExecutorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncDeadLetterRetryAttempts_RequestedByAccountId",
                table: "SyncDeadLetterRetryAttempts",
                column: "RequestedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncDeadLetterRetryAttempts_SyncDeadLetterId_AttemptNumber",
                table: "SyncDeadLetterRetryAttempts",
                columns: new[] { "SyncDeadLetterId", "AttemptNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EdgeStateSummaries");

            migrationBuilder.DropTable(
                name: "ExecutionEndpointCapabilityProjections");

            migrationBuilder.DropTable(
                name: "ExecutionEndpointMqttCredentials");

            migrationBuilder.DropTable(
                name: "ProductionEventCheckpoints");

            migrationBuilder.DropTable(
                name: "SyncDeadLetterRetryAttempts");

            migrationBuilder.DropTable(
                name: "ExecutionEndpointReadinessProjections");

            migrationBuilder.DropIndex(
                name: "IX_SyncEventInbox_SourceNodeId_EventId",
                table: "SyncEventInbox");

            migrationBuilder.DropIndex(
                name: "IX_SyncEventInbox_SourceNodeId_SequenceNumber",
                table: "SyncEventInbox");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "SyncEventInbox");

            migrationBuilder.CreateIndex(
                name: "IX_SyncEventInbox_EventId",
                table: "SyncEventInbox",
                column: "EventId",
                unique: true);
        }
    }
}

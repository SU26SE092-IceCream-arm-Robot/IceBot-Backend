using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRegistrationAndOrganizationLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeactivatedAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeactivatedByAccountId",
                table: "Organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "Organizations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReactivatedAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReactivatedByAccountId",
                table: "Organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StatusRevision",
                table: "Organizations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SuspendedAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SuspendedByAccountId",
                table: "Organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspensionReason",
                table: "Organizations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspensionReasonCode",
                table: "Organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrganizationStatusTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: false),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ChangedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestIdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OrganizationStatusRevision = table.Column<long>(type: "bigint", nullable: false),
                    ReadinessConfirmed = table.Column<bool>(type: "boolean", nullable: true),
                    SessionRevocationStatus = table.Column<int>(type: "integer", nullable: false),
                    SessionRevocationAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextSessionRevocationAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SessionRevocationCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SessionRevocationLastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationStatusTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationStatusTransitions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NormalizedPhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BusinessName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpectedLocationCount = table.Column<int>(type: "integer", nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PrivacyPolicyRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrivacyPolicyAcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedProvisioningJson = table.Column<string>(type: "jsonb", maxLength: 8000, nullable: true),
                    ProvisionedOrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProvisionedOrgAdminAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProvisionedInvitationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProvisioningFailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProvisioningFailureMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentPageRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentPageId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    BodyHtml = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: false),
                    PublishedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPageRevisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DraftTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DraftBodyHtml = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: false),
                    PublishedRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentPages_ContentPageRevisions_PublishedRevisionId",
                        column: x => x.PublishedRevisionId,
                        principalTable: "ContentPageRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentPageRevisions_ContentPageId_RevisionNumber",
                table: "ContentPageRevisions",
                columns: new[] { "ContentPageId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_Key",
                table: "ContentPages",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_PublishedRevisionId",
                table: "ContentPages",
                column: "PublishedRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_Slug",
                table: "ContentPages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationStatusTransitions_OrganizationId_ChangedAt",
                table: "OrganizationStatusTransitions",
                columns: new[] { "OrganizationId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationStatusTransitions_OrganizationId_RequestIdempot~",
                table: "OrganizationStatusTransitions",
                columns: new[] { "OrganizationId", "RequestIdempotencyKey" },
                unique: true,
                filter: "\"RequestIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationStatusTransitions_SessionRevocationStatus_NextS~",
                table: "OrganizationStatusTransitions",
                columns: new[] { "SessionRevocationStatus", "NextSessionRevocationAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistrations_IdempotencyKey",
                table: "ServiceRegistrations",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistrations_NormalizedEmail_CreatedAt",
                table: "ServiceRegistrations",
                columns: new[] { "NormalizedEmail", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistrations_ProvisionedOrgAdminAccountId",
                table: "ServiceRegistrations",
                column: "ProvisionedOrgAdminAccountId",
                unique: true,
                filter: "\"ProvisionedOrgAdminAccountId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistrations_ProvisionedOrganizationId",
                table: "ServiceRegistrations",
                column: "ProvisionedOrganizationId",
                unique: true,
                filter: "\"ProvisionedOrganizationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistrations_ReferenceCode",
                table: "ServiceRegistrations",
                column: "ReferenceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistrations_Status_CreatedAt",
                table: "ServiceRegistrations",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ContentPageRevisions_ContentPages_ContentPageId",
                table: "ContentPageRevisions",
                column: "ContentPageId",
                principalTable: "ContentPages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContentPageRevisions_ContentPages_ContentPageId",
                table: "ContentPageRevisions");

            migrationBuilder.DropTable(
                name: "OrganizationStatusTransitions");

            migrationBuilder.DropTable(
                name: "ServiceRegistrations");

            migrationBuilder.DropTable(
                name: "ContentPages");

            migrationBuilder.DropTable(
                name: "ContentPageRevisions");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "DeactivatedByAccountId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ReactivatedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ReactivatedByAccountId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "StatusRevision",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SuspendedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SuspendedByAccountId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SuspensionReason",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SuspensionReasonCode",
                table: "Organizations");
        }
    }
}

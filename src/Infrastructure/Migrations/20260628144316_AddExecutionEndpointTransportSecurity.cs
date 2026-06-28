using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExecutionEndpointTransportSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicKeyPem",
                table: "ExecutionEndpointCredentialBindings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExecutionEndpointRequestNonces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KioskExecutionEndpointId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nonce = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionEndpointRequestNonces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionEndpointRequestNonces_KioskExecutionEndpoints_Kios~",
                        column: x => x.KioskExecutionEndpointId,
                        principalTable: "KioskExecutionEndpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointRequestNonces_ExpiresAt",
                table: "ExecutionEndpointRequestNonces",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionEndpointRequestNonces_KioskExecutionEndpointId_Non~",
                table: "ExecutionEndpointRequestNonces",
                columns: new[] { "KioskExecutionEndpointId", "Nonce" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionEndpointRequestNonces");

            migrationBuilder.DropColumn(
                name: "PublicKeyPem",
                table: "ExecutionEndpointCredentialBindings");
        }
    }
}

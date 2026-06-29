using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRobotArtifactTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceRobotArtifactTemplateId",
                table: "RobotArtifacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RobotArtifactTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TemplateName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Checksum = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RuntimeTargetCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MachineModelCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentLengthBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotArtifactTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifacts_SourceRobotArtifactTemplateId",
                table: "RobotArtifacts",
                column: "SourceRobotArtifactTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifactTemplates_RuntimeTargetCode_MachineModelCode_S~",
                table: "RobotArtifactTemplates",
                columns: new[] { "RuntimeTargetCode", "MachineModelCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifactTemplates_StorageKey",
                table: "RobotArtifactTemplates",
                column: "StorageKey",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RobotArtifactTemplates_TemplateCode_Checksum",
                table: "RobotArtifactTemplates",
                columns: new[] { "TemplateCode", "Checksum" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_RobotArtifacts_RobotArtifactTemplates_SourceRobotArtifactTe~",
                table: "RobotArtifacts",
                column: "SourceRobotArtifactTemplateId",
                principalTable: "RobotArtifactTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RobotArtifacts_RobotArtifactTemplates_SourceRobotArtifactTe~",
                table: "RobotArtifacts");

            migrationBuilder.DropTable(
                name: "RobotArtifactTemplates");

            migrationBuilder.DropIndex(
                name: "IX_RobotArtifacts_SourceRobotArtifactTemplateId",
                table: "RobotArtifacts");

            migrationBuilder.DropColumn(
                name: "SourceRobotArtifactTemplateId",
                table: "RobotArtifacts");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionProgramBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductionProgramBindingChecksum",
                table: "ExecutionRouteRobotBindings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionProgramBindingId",
                table: "ExecutionRouteRobotBindings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductionProgramBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeVersion = table.Column<int>(type: "integer", nullable: false),
                    RobotProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramManifestChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequiredWorkcellCapabilityCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SupportedOptionCodesJson = table.Column<string>(type: "jsonb", maxLength: 10000, nullable: false, defaultValueSql: "'[]'::jsonb"),
                    BindingChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ProductionProgramBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionProgramBindings_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionProgramBindings_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionProgramBindings_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionProgramBindings_RobotPrograms_RobotProgramId",
                        column: x => x.RobotProgramId,
                        principalTable: "RobotPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRouteRobotBindings_ProductionProgramBindingId",
                table: "ExecutionRouteRobotBindings",
                column: "ProductionProgramBindingId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionProgramBindings_BindingChecksum",
                table: "ProductionProgramBindings",
                column: "BindingChecksum",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionProgramBindings_OrganizationId_RecipeId_RobotProg~",
                table: "ProductionProgramBindings",
                columns: new[] { "OrganizationId", "RecipeId", "RobotProgramId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionProgramBindings_ProductVariantId",
                table: "ProductionProgramBindings",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionProgramBindings_RecipeId",
                table: "ProductionProgramBindings",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionProgramBindings_RobotProgramId",
                table: "ProductionProgramBindings",
                column: "RobotProgramId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionRouteRobotBindings_ProductionProgramBindings_Produ~",
                table: "ExecutionRouteRobotBindings",
                column: "ProductionProgramBindingId",
                principalTable: "ProductionProgramBindings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionRouteRobotBindings_ProductionProgramBindings_Produ~",
                table: "ExecutionRouteRobotBindings");

            migrationBuilder.DropTable(
                name: "ProductionProgramBindings");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionRouteRobotBindings_ProductionProgramBindingId",
                table: "ExecutionRouteRobotBindings");

            migrationBuilder.DropColumn(
                name: "ProductionProgramBindingChecksum",
                table: "ExecutionRouteRobotBindings");

            migrationBuilder.DropColumn(
                name: "ProductionProgramBindingId",
                table: "ExecutionRouteRobotBindings");
        }
    }
}

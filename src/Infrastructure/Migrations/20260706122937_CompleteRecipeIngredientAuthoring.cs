using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteRecipeIngredientAuthoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "Recipes"
                        WHERE "IsDefault" = TRUE
                          AND "Status" <> 4
                          AND "DeletedAt" IS NULL
                        GROUP BY "ProductVariantId"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot enforce one default recipe per product variant: duplicate non-retired default recipes exist.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Ingredients",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_ProductVariantId_Default",
                table: "Recipes",
                column: "ProductVariantId",
                unique: true,
                filter: "\"IsDefault\" = TRUE AND \"Status\" <> 4 AND \"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Recipes_ProductVariantId_Default",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Ingredients");
        }
    }
}

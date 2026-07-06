using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleDefaultProductOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_OptionGroupId",
                table: "ProductOptions",
                column: "OptionGroupId",
                unique: true,
                filter: "\"IsDefault\" = TRUE AND \"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_OptionGroupId",
                table: "ProductOptions");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_OptionGroupId",
                table: "ProductOptions",
                column: "OptionGroupId");
        }
    }
}

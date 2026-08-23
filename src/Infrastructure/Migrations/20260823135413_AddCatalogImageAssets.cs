using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogImageAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "ImageAltText",
                table: "ProductVariants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageAltText",
                table: "Products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImageAssetId",
                table: "ProductVariants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "ProductVariants",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "ImageAssetId",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "CatalogImageAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProviderAssetId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PublicId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeliveryUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Format = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    Bytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogImageAssets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ImageAssetId",
                table: "ProductVariants",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ImageAssetId",
                table: "Products",
                column: "ImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImageAssets_Provider_ProviderAssetId",
                table: "CatalogImageAssets",
                columns: new[] { "Provider", "ProviderAssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogImageAssets_Provider_PublicId",
                table: "CatalogImageAssets",
                columns: new[] { "Provider", "PublicId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_CatalogImageAssets_ImageAssetId",
                table: "Products",
                column: "ImageAssetId",
                principalTable: "CatalogImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_CatalogImageAssets_ImageAssetId",
                table: "ProductVariants",
                column: "ImageAssetId",
                principalTable: "CatalogImageAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_CatalogImageAssets_ImageAssetId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_CatalogImageAssets_ImageAssetId",
                table: "ProductVariants");

            migrationBuilder.DropTable(
                name: "CatalogImageAssets");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ImageAssetId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_Products_ImageAssetId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImageAssetId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ImageAssetId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Products");

            migrationBuilder.DropColumn(name: "ImageAltText", table: "ProductVariants");

            migrationBuilder.DropColumn(name: "ImageAltText", table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ProductVariants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "MenuItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}

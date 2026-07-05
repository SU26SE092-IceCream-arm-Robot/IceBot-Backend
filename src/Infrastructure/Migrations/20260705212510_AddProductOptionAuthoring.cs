using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductOptionAuthoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "OptionGroups")
                       OR EXISTS (SELECT 1 FROM "ProductOptions")
                       OR EXISTS (SELECT 1 FROM "ProductProductOptions")
                       OR EXISTS (SELECT 1 FROM "OrderItemProductOptions")
                       OR EXISTS (SELECT 1 FROM "OrderItems" WHERE "OptionsJson" IS NOT NULL AND btrim("OptionsJson"::text) <> '') THEN
                        RAISE EXCEPTION 'Legacy product-option data exists. Export or migrate OptionGroups, ProductOptions, ProductProductOptions, OrderItemProductOptions, and OrderItems.OptionsJson before applying AddProductOptionAuthoring.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ProductOptions_Organizations_OrganizationId",
                table: "ProductOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductOptions_ProductOptions_TemplateProductOptionId",
                table: "ProductOptions");

            migrationBuilder.DropTable(
                name: "OrderItemProductOptions");

            migrationBuilder.DropTable(
                name: "ProductProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_OptionGroupId",
                table: "ProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_OrganizationId",
                table: "ProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_OrganizationId_OptionGroupId_Code",
                table: "ProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_TemplateProductOptionId",
                table: "ProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_OptionGroups_Code",
                table: "OptionGroups");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "ScopeType",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "OptionsJson",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "OptionsSchemaVersion",
                table: "OrderItems");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "OptionGroups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "MenuItemProductOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemProductOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemProductOptions_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuItemProductOptions_ProductOptions_ProductOptionId",
                        column: x => x.ProductOptionId,
                        principalTable: "ProductOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionGroupId = table.Column<long>(type: "bigint", nullable: false),
                    OptionGroupCodeSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CodeSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NameSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UnitPriceDelta = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    TotalPriceDelta = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedByAccountId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemOptions_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_OptionGroupId_Code",
                table: "ProductOptions",
                columns: new[] { "OptionGroupId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OptionGroups_ProductId_Code",
                table: "OptionGroups",
                columns: new[] { "ProductId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemProductOptions_MenuItemId_ProductOptionId",
                table: "MenuItemProductOptions",
                columns: new[] { "MenuItemId", "ProductOptionId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemProductOptions_ProductOptionId",
                table: "MenuItemProductOptions",
                column: "ProductOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemOptions_OrderItemId_OptionGroupId",
                table: "OrderItemOptions",
                columns: new[] { "OrderItemId", "OptionGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemOptions_OrderItemId_ProductOptionId",
                table: "OrderItemOptions",
                columns: new[] { "OrderItemId", "ProductOptionId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_OptionGroups_Products_ProductId",
                table: "OptionGroups",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OptionGroups_Products_ProductId",
                table: "OptionGroups");

            migrationBuilder.DropTable(
                name: "MenuItemProductOptions");

            migrationBuilder.DropTable(
                name: "OrderItemOptions");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_OptionGroupId_Code",
                table: "ProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_OptionGroups_ProductId_Code",
                table: "OptionGroups");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "OptionGroups");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "ProductOptions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "ProductOptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScopeType",
                table: "ProductOptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OptionsJson",
                table: "OrderItems",
                type: "jsonb",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OptionsSchemaVersion",
                table: "OrderItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OrderItemProductOptions",
                columns: table => new
                {
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductOptionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemProductOptions", x => new { x.OrderItemId, x.ProductOptionId });
                    table.ForeignKey(
                        name: "FK_OrderItemProductOptions_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemProductOptions_ProductOptions_ProductOptionId",
                        column: x => x.ProductOptionId,
                        principalTable: "ProductOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductProductOptions",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductOptionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductProductOptions", x => new { x.ProductId, x.ProductOptionId });
                    table.ForeignKey(
                        name: "FK_ProductProductOptions_ProductOptions_ProductOptionId",
                        column: x => x.ProductOptionId,
                        principalTable: "ProductOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductProductOptions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_OptionGroupId",
                table: "ProductOptions",
                column: "OptionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_OrganizationId",
                table: "ProductOptions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_OrganizationId_OptionGroupId_Code",
                table: "ProductOptions",
                columns: new[] { "OrganizationId", "OptionGroupId", "Code" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_TemplateProductOptionId",
                table: "ProductOptions",
                column: "TemplateProductOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_OptionGroups_Code",
                table: "OptionGroups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemProductOptions_ProductOptionId",
                table: "OrderItemProductOptions",
                column: "ProductOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductProductOptions_ProductOptionId",
                table: "ProductProductOptions",
                column: "ProductOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductOptions_Organizations_OrganizationId",
                table: "ProductOptions",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductOptions_ProductOptions_TemplateProductOptionId",
                table: "ProductOptions",
                column: "TemplateProductOptionId",
                principalTable: "ProductOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

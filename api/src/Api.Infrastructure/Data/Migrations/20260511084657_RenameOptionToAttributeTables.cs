using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameOptionToAttributeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ProductOptionGroups",
                schema: "business",
                newName: "ProductAttributeGroups",
                newSchema: "business");

            migrationBuilder.RenameTable(
                name: "ProductOptionValues",
                schema: "business",
                newName: "ProductAttributeValues",
                newSchema: "business");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                schema: "business",
                table: "Products",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "CostPrice",
                schema: "business",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPrice",
                schema: "business",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                schema: "business",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                schema: "business",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CostPrice",     schema: "business", table: "Products");
            migrationBuilder.DropColumn(name: "DiscountPrice", schema: "business", table: "Products");
            migrationBuilder.DropColumn(name: "Sku",           schema: "business", table: "Products");
            migrationBuilder.DropColumn(name: "Barcode",       schema: "business", table: "Products");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                schema: "business",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.RenameTable(
                name: "ProductAttributeGroups",
                schema: "business",
                newName: "ProductOptionGroups",
                newSchema: "business");

            migrationBuilder.RenameTable(
                name: "ProductAttributeValues",
                schema: "business",
                newName: "ProductOptionValues",
                newSchema: "business");
        }
    }
}

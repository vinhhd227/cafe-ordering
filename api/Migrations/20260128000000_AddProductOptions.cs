using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasIceLevelOption",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasSugarLevelOption",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasTemperatureOption",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "IceLevel",
                table: "OrderItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SugarLevel",
                table: "OrderItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Temperature",
                table: "OrderItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasIceLevelOption",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HasSugarLevelOption",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HasTemperatureOption",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IceLevel",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SugarLevel",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "OrderItems");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemCustomizationOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IceLevel",
                schema: "business",
                table: "OrderItem",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SugarLevel",
                schema: "business",
                table: "OrderItem",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Temperature",
                schema: "business",
                table: "OrderItem",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IceLevel",
                schema: "business",
                table: "OrderItem");

            migrationBuilder.DropColumn(
                name: "SugarLevel",
                schema: "business",
                table: "OrderItem");

            migrationBuilder.DropColumn(
                name: "Temperature",
                schema: "business",
                table: "OrderItem");
        }
    }
}

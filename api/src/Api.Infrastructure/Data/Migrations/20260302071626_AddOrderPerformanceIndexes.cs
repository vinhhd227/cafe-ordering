using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderDate",
                schema: "business",
                table: "Orders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber_Unique",
                schema: "business",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                schema: "business",
                table: "Orders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderDate",
                schema: "business",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderNumber_Unique",
                schema: "business",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status",
                schema: "business",
                table: "Orders");
        }
    }
}

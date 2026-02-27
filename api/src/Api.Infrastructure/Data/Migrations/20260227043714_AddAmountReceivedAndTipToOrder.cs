using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAmountReceivedAndTipToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountReceived",
                schema: "business",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TipAmount",
                schema: "business",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountReceived",
                schema: "business",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TipAmount",
                schema: "business",
                table: "Orders");
        }
    }
}

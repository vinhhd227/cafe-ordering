using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTypeAndCustomerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                schema: "business",
                table: "Orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                schema: "business",
                table: "Orders",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                schema: "business",
                table: "Orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                schema: "business",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryNote",
                schema: "business",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "business",
                table: "Orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'DINE_IN'");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Type",
                schema: "business",
                table: "Orders",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_Type",
                schema: "business",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                schema: "business",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                schema: "business",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                schema: "business",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryNote",
                schema: "business",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "business",
                table: "Orders");

            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                schema: "business",
                table: "Orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}

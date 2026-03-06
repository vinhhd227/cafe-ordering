using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PaymentEnumsToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_Orders_OrderId",
                schema: "business",
                table: "OrderItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderItem",
                schema: "business",
                table: "OrderItem");

            migrationBuilder.RenameTable(
                name: "OrderItem",
                schema: "business",
                newName: "OrderItems",
                newSchema: "business");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItem_OrderId",
                schema: "business",
                table: "OrderItems",
                newName: "IX_OrderItems_OrderId");

            // PaymentStatus: int → varchar(20) with USING CASE for data preservation
            migrationBuilder.Sql("""
                ALTER TABLE business."Orders"
                  ALTER COLUMN "PaymentStatus" TYPE character varying(20)
                  USING CASE "PaymentStatus"
                    WHEN 1 THEN 'UNPAID'
                    WHEN 2 THEN 'PAID'
                    WHEN 3 THEN 'REFUNDED'
                    WHEN 4 THEN 'VOIDED'
                  END;
                ALTER TABLE business."Orders"
                  ALTER COLUMN "PaymentStatus" SET DEFAULT 'UNPAID';
                """);

            // PaymentMethod: int → varchar(20) with USING CASE for data preservation
            migrationBuilder.Sql("""
                ALTER TABLE business."Orders"
                  ALTER COLUMN "PaymentMethod" TYPE character varying(20)
                  USING CASE "PaymentMethod"
                    WHEN 0 THEN 'UNKNOWN'
                    WHEN 1 THEN 'CASH'
                    WHEN 2 THEN 'BANK_TRANSFER'
                  END;
                ALTER TABLE business."Orders"
                  ALTER COLUMN "PaymentMethod" SET DEFAULT 'UNKNOWN';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Temperature",
                schema: "business",
                table: "OrderItems",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SugarLevel",
                schema: "business",
                table: "OrderItems",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IceLevel",
                schema: "business",
                table: "OrderItems",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderItems",
                schema: "business",
                table: "OrderItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                schema: "business",
                table: "OrderItems",
                column: "OrderId",
                principalSchema: "business",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                schema: "business",
                table: "OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderItems",
                schema: "business",
                table: "OrderItems");

            migrationBuilder.RenameTable(
                name: "OrderItems",
                schema: "business",
                newName: "OrderItem",
                newSchema: "business");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_OrderId",
                schema: "business",
                table: "OrderItem",
                newName: "IX_OrderItem_OrderId");

            migrationBuilder.Sql("""
                ALTER TABLE business."Orders"
                  ALTER COLUMN "PaymentStatus" DROP DEFAULT;
                ALTER TABLE business."Orders"
                  ALTER COLUMN "PaymentStatus" TYPE integer
                  USING CASE "PaymentStatus"
                    WHEN 'UNPAID'   THEN 1
                    WHEN 'PAID'     THEN 2
                    WHEN 'REFUNDED' THEN 3
                    WHEN 'VOIDED'   THEN 4
                  END;
                ALTER TABLE business."Orders"
                  ALTER COLUMN "PaymentStatus" SET DEFAULT 1;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE business."Orders"
                  ALTER COLUMN "PaymentMethod" DROP DEFAULT;
                ALTER TABLE business."Orders"
                  ALTER COLUMN "PaymentMethod" TYPE integer
                  USING CASE "PaymentMethod"
                    WHEN 'UNKNOWN'       THEN 0
                    WHEN 'CASH'          THEN 1
                    WHEN 'BANK_TRANSFER' THEN 2
                  END;
                ALTER TABLE business."Orders"
                  ALTER COLUMN "PaymentMethod" SET DEFAULT 0;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Temperature",
                schema: "business",
                table: "OrderItem",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SugarLevel",
                schema: "business",
                table: "OrderItem",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IceLevel",
                schema: "business",
                table: "OrderItem",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderItem",
                schema: "business",
                table: "OrderItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_Orders_OrderId",
                schema: "business",
                table: "OrderItem",
                column: "OrderId",
                principalSchema: "business",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

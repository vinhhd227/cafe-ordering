using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OrderStatusToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the int-based index before altering column type
            migrationBuilder.DropIndex(
                name: "IX_Orders_Status",
                schema: "business",
                table: "Orders");

            // Convert int → varchar(20) UPPERCASE with USING CASE.
            // No ELSE clause: unknown int values produce NULL, violating NOT NULL
            // and causing an explicit failure rather than silently corrupting data.
            migrationBuilder.Sql("""
                ALTER TABLE business."Orders"
                  ALTER COLUMN "Status" TYPE character varying(20)
                  USING CASE "Status"
                    WHEN 1 THEN 'PENDING'
                    WHEN 2 THEN 'PROCESSING'
                    WHEN 3 THEN 'COMPLETED'
                    WHEN 4 THEN 'CANCELLED'
                  END;
                """);

            // Recreate index on the new varchar column
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
                name: "IX_Orders_Status",
                schema: "business",
                table: "Orders");

            migrationBuilder.Sql("""
                ALTER TABLE business."Orders"
                  ALTER COLUMN "Status" TYPE integer
                  USING CASE "Status"
                    WHEN 'PENDING' THEN 1
                    WHEN 'PROCESSING' THEN 2
                    WHEN 'COMPLETED' THEN 3
                    WHEN 'CANCELLED' THEN 4
                  END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                schema: "business",
                table: "Orders",
                column: "Status");
        }
    }
}

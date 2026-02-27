using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeToTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "business",
                table: "Tables",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Backfill Code for existing rows using T{Number:D2} format
            migrationBuilder.Sql("""
                UPDATE business."Tables"
                SET "Code" = 'T' || LPAD("Number"::text, 2, '0')
                WHERE "Code" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Tables_Code",
                schema: "business",
                table: "Tables",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tables_Code",
                schema: "business",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "business",
                table: "Tables");
        }
    }
}

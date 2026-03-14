using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPromotionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Promotions_Code_Unique",
                schema: "business",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "StackPolicy",
                schema: "business",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "StackPolicy",
                schema: "business",
                table: "OrderPromotions");

            // Assign unique codes to any promotions that had no code (auto-apply promotions)
            migrationBuilder.Sql("""
                UPDATE business."Promotions"
                SET "Code" = 'CAFE-' || upper(substring(md5(random()::text || "Id"::text), 1, 6))
                WHERE "Code" IS NULL OR "Code" = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "business",
                table: "Promotions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeVisibility",
                schema: "business",
                table: "Promotions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'PUBLIC'");

            migrationBuilder.AddColumn<string>(
                name: "GetFromCategoryIds",
                schema: "business",
                table: "Promotions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GetFromProductIds",
                schema: "business",
                table: "Promotions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountAmount",
                schema: "business",
                table: "Promotions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Code_Unique",
                schema: "business",
                table: "Promotions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_CodeVisibility",
                schema: "business",
                table: "Promotions",
                column: "CodeVisibility");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Promotions_Code_Unique",
                schema: "business",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_CodeVisibility",
                schema: "business",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "CodeVisibility",
                schema: "business",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "GetFromCategoryIds",
                schema: "business",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "GetFromProductIds",
                schema: "business",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "MaxDiscountAmount",
                schema: "business",
                table: "Promotions");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "business",
                table: "Promotions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "StackPolicy",
                schema: "business",
                table: "Promotions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'EXCLUSIVE'");

            migrationBuilder.AddColumn<string>(
                name: "StackPolicy",
                schema: "business",
                table: "OrderPromotions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Code_Unique",
                schema: "business",
                table: "Promotions",
                column: "Code",
                unique: true,
                filter: "\"Code\" IS NOT NULL");
        }
    }
}

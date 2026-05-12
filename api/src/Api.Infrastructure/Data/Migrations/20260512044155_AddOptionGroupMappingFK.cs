using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOptionGroupMappingFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_ProductOptionGroupMappings_ProductOptionGroups_GroupId",
                schema: "business",
                table: "ProductOptionGroupMappings",
                column: "GroupId",
                principalSchema: "business",
                principalTable: "ProductOptionGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductOptionGroupMappings_ProductOptionGroups_GroupId",
                schema: "business",
                table: "ProductOptionGroupMappings");
        }
    }
}

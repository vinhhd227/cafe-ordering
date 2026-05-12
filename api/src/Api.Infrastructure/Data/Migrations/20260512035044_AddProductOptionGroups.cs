using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductOptionGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // RenameOptionToAttributeTables renamed the tables but left old constraint/index names.
            // Rename them now so the new ProductOptionGroups table can reuse those names cleanly.
            migrationBuilder.Sql(@"
                ALTER TABLE business.""ProductAttributeGroups"" RENAME CONSTRAINT ""PK_ProductOptionGroups"" TO ""PK_ProductAttributeGroups"";
                ALTER TABLE business.""ProductAttributeGroups"" RENAME CONSTRAINT ""FK_ProductOptionGroups_Products_ProductId"" TO ""FK_ProductAttributeGroups_Products_ProductId"";
                ALTER INDEX  business.""IX_ProductOptionGroups_ProductId""                  RENAME TO ""IX_ProductAttributeGroups_ProductId"";
                ALTER INDEX  business.""IX_ProductOptionGroups_ProductId_DisplayOrder""     RENAME TO ""IX_ProductAttributeGroups_ProductId_DisplayOrder"";
                ALTER TABLE business.""ProductAttributeValues"" RENAME CONSTRAINT ""PK_ProductOptionValues"" TO ""PK_ProductAttributeValues"";
                ALTER TABLE business.""ProductAttributeValues"" RENAME CONSTRAINT ""FK_ProductOptionValues_ProductOptionGroups_GroupId"" TO ""FK_ProductAttributeValues_ProductAttributeGroups_GroupId"";
                ALTER INDEX  business.""IX_ProductOptionValues_GroupId""                    RENAME TO ""IX_ProductAttributeValues_GroupId"";
            ");

            migrationBuilder.AddColumn<decimal>(
                name: "OptionValueTotal",
                schema: "business",
                table: "OrderItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "OrderItemSelectedOptions",
                schema: "business",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderItemId = table.Column<int>(type: "integer", nullable: false),
                    OptionValueId = table.Column<int>(type: "integer", nullable: false),
                    GroupName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ValueName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemSelectedOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemSelectedOptions_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalSchema: "business",
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductOptionGroupMappings",
                schema: "business",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOptionGroupMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductOptionGroupMappings_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "business",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductOptionGroups",
                schema: "business",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    AllowMultiple = table.Column<bool>(type: "boolean", nullable: false),
                    AllowQuantity = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOptionGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductOptionValues",
                schema: "business",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    IsInStock = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOptionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductOptionValues_ProductOptionGroups_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "business",
                        principalTable: "ProductOptionGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemSelectedOptions_OrderItemId",
                schema: "business",
                table: "OrderItemSelectedOptions",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionGroupMappings_GroupId",
                schema: "business",
                table: "ProductOptionGroupMappings",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionGroupMappings_ProductId",
                schema: "business",
                table: "ProductOptionGroupMappings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionGroupMappings_ProductId_GroupId",
                schema: "business",
                table: "ProductOptionGroupMappings",
                columns: new[] { "ProductId", "GroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionGroups_DisplayOrder",
                schema: "business",
                table: "ProductOptionGroups",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionGroups_IsActive",
                schema: "business",
                table: "ProductOptionGroups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionValues_GroupId",
                schema: "business",
                table: "ProductOptionValues",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionValues_GroupId_DisplayOrder",
                schema: "business",
                table: "ProductOptionValues",
                columns: new[] { "GroupId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemSelectedOptions",
                schema: "business");

            migrationBuilder.DropTable(
                name: "ProductOptionGroupMappings",
                schema: "business");

            migrationBuilder.DropTable(
                name: "ProductOptionValues",
                schema: "business");

            migrationBuilder.DropTable(
                name: "ProductOptionGroups",
                schema: "business");

            migrationBuilder.DropColumn(
                name: "OptionValueTotal",
                schema: "business",
                table: "OrderItems");
        }
    }
}

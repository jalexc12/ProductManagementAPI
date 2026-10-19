using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class EnforceProductIdRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterSequence(
                name: "ProductIdSequence",
                maxValue: 999999L);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_Id_Range",
                table: "Products",
                sql: "[Id] BETWEEN 100000 AND 999999");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_Id_Range",
                table: "Products");

            migrationBuilder.AlterSequence(
                name: "ProductIdSequence",
                oldMaxValue: 999999L);
        }
    }
}

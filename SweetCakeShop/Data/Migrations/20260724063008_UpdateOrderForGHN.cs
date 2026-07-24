using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SweetCakeShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderForGHN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DistrictId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProvinceId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingStatus",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WardCode",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProvinceId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "WardCode",
                table: "Orders");
        }
    }
}

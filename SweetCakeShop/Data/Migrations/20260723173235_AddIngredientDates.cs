using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SweetCakeShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "Ingredients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ImportDate",
                table: "Ingredients",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "ImportDate",
                table: "Ingredients");
        }
    }
}

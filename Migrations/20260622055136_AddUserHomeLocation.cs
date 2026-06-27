using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ascent_app.Migrations
{
    /// <inheritdoc />
    public partial class AddUserHomeLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "HomeLat",
                table: "AspNetUsers",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HomeLng",
                table: "AspNetUsers",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeLocationName",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HomeLat",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HomeLng",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HomeLocationName",
                table: "AspNetUsers");
        }
    }
}

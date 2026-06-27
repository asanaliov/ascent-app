using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ascent_app.Migrations
{
    /// <inheritdoc />
    public partial class AddTrailRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RouteGeoJson",
                table: "Trails",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RouteGeoJson",
                table: "Trails");
        }
    }
}

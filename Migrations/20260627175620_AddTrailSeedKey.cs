using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ascent_app.Migrations
{
    /// <inheritdoc />
    public partial class AddTrailSeedKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SeedKey",
                table: "Trails",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trails_SeedKey",
                table: "Trails",
                column: "SeedKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trails_SeedKey",
                table: "Trails");

            migrationBuilder.DropColumn(
                name: "SeedKey",
                table: "Trails");
        }
    }
}

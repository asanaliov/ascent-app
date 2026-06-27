using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ascent_app.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalSeedDataModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "EstimatedTimeHours",
                table: "Trails",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "IsSeedData",
                table: "Trails",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Trails",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsCoverImage",
                table: "TrailPhotos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemoData",
                table: "Reviews",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemoData",
                table: "HikeLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemoData",
                table: "Favorites",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExperienceLevel",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemoUser",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 400,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HikeEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    StartsAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MaxParticipants = table.Column<int>(type: "INTEGER", nullable: false),
                    MeetingPoint = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsDemoData = table.Column<bool>(type: "INTEGER", nullable: false),
                    TrailId = table.Column<int>(type: "INTEGER", nullable: false),
                    GuideId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HikeEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HikeEvents_AspNetUsers_GuideId",
                        column: x => x.GuideId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HikeEvents_Trails_TrailId",
                        column: x => x.TrailId,
                        principalTable: "Trails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HikeEvents_GuideId",
                table: "HikeEvents",
                column: "GuideId");

            migrationBuilder.CreateIndex(
                name: "IX_HikeEvents_Title_StartsAt",
                table: "HikeEvents",
                columns: new[] { "Title", "StartsAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HikeEvents_TrailId",
                table: "HikeEvents",
                column: "TrailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HikeEvents");

            migrationBuilder.DropColumn(
                name: "EstimatedTimeHours",
                table: "Trails");

            migrationBuilder.DropColumn(
                name: "IsSeedData",
                table: "Trails");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Trails");

            migrationBuilder.DropColumn(
                name: "IsCoverImage",
                table: "TrailPhotos");

            migrationBuilder.DropColumn(
                name: "IsDemoData",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "IsDemoData",
                table: "HikeLogs");

            migrationBuilder.DropColumn(
                name: "IsDemoData",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "ExperienceLevel",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsDemoUser",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "AspNetUsers");
        }
    }
}

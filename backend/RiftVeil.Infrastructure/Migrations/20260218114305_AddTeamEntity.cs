using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiftVeil.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Team1Name",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Team1ShortName",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Team2Name",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Team2ShortName",
                table: "Matches");

            migrationBuilder.AddColumn<int>(
                name: "Team1Id",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Team2Id",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Team1Id",
                table: "Matches",
                column: "Team1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Team2Id",
                table: "Matches",
                column: "Team2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ShortName",
                table: "Teams",
                column: "ShortName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_Team1Id",
                table: "Matches",
                column: "Team1Id",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Teams_Team2Id",
                table: "Matches",
                column: "Team2Id",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_Team1Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Teams_Team2Id",
                table: "Matches");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Matches_Team1Id",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_Team2Id",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Team1Id",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Team2Id",
                table: "Matches");

            migrationBuilder.AddColumn<string>(
                name: "Team1Name",
                table: "Matches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Team1ShortName",
                table: "Matches",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Team2Name",
                table: "Matches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Team2ShortName",
                table: "Matches",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}

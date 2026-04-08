using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiftVeil.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamShortNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Team1ShortName",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Team2ShortName",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Team1ShortName",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Team2ShortName",
                table: "Matches");
        }
    }
}

using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiftVeil.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentStage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbSmokeTest");

            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "Tournaments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stage",
                table: "Tournaments");

            migrationBuilder.CreateTable(
                name: "DbSmokeTest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbSmokeTest", x => x.Id);
                });
        }
    }
}

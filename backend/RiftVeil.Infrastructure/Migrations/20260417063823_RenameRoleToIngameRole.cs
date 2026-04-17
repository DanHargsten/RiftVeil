using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiftVeil.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameRoleToIngameRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Role",
                table: "GamePlayerStats",
                newName: "IngameRole");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IngameRole",
                table: "GamePlayerStats",
                newName: "Role");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiftVeil.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameVodSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameVods_GameId_Provider_Locale",
                table: "GameVods");

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "GameVods",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE [GameVods]
                SET [Source] = 1,
                    [Locale] = NULL
                WHERE [Locale] = 'manual';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_GameVods_GameId_Provider_Locale_Source",
                table: "GameVods",
                columns: new[] { "GameId", "Provider", "Locale", "Source" },
                unique: true,
                filter: "[Locale] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameVods_GameId_Provider_Locale_Source",
                table: "GameVods");

            migrationBuilder.Sql("""
                UPDATE [GameVods]
                SET [Locale] = 'manual'
                WHERE [Source] = 1 AND [Locale] IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "Source",
                table: "GameVods");

            migrationBuilder.CreateIndex(
                name: "IX_GameVods_GameId_Provider_Locale",
                table: "GameVods",
                columns: new[] { "GameId", "Provider", "Locale" },
                unique: true,
                filter: "[Locale] IS NOT NULL");
        }
    }
}

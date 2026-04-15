using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiftVeil.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefineGameDetailStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameTeamStats_GameId",
                table: "GameTeamStats");

            migrationBuilder.DropIndex(
                name: "IX_GamePlayerStats_GameId",
                table: "GamePlayerStats");

            migrationBuilder.DropIndex(
                name: "IX_GameDraftEntries_GameId",
                table: "GameDraftEntries");

            migrationBuilder.AlterColumn<string>(
                name: "TrinketId",
                table: "GamePlayerStats",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SummonerSpell2Id",
                table: "GamePlayerStats",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SummonerSpell1Id",
                table: "GamePlayerStats",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "GamePlayerStats",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PlayerName",
                table: "GamePlayerStats",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ItemIds",
                table: "GamePlayerStats",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Champion",
                table: "GamePlayerStats",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Phase",
                table: "GameDraftEntries",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Champion",
                table: "GameDraftEntries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_GameTeamStats_GameId_TeamNumber",
                table: "GameTeamStats",
                columns: new[] { "GameId", "TeamNumber" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_GameTeamStats_TeamNumber",
                table: "GameTeamStats",
                sql: "[TeamNumber] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayerStats_GameId_PlayerName",
                table: "GamePlayerStats",
                columns: new[] { "GameId", "PlayerName" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_GamePlayerStats_TeamNumber",
                table: "GamePlayerStats",
                sql: "[TeamNumber] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_GameDraftEntries_GameId_SequenceNumber",
                table: "GameDraftEntries",
                columns: new[] { "GameId", "SequenceNumber" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_GameDraftEntries_Phase",
                table: "GameDraftEntries",
                sql: "[Phase] IN ('Ban', 'Pick')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GameDraftEntries_TeamNumber",
                table: "GameDraftEntries",
                sql: "[TeamNumber] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameTeamStats_GameId_TeamNumber",
                table: "GameTeamStats");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GameTeamStats_TeamNumber",
                table: "GameTeamStats");

            migrationBuilder.DropIndex(
                name: "IX_GamePlayerStats_GameId_PlayerName",
                table: "GamePlayerStats");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GamePlayerStats_TeamNumber",
                table: "GamePlayerStats");

            migrationBuilder.DropIndex(
                name: "IX_GameDraftEntries_GameId_SequenceNumber",
                table: "GameDraftEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GameDraftEntries_Phase",
                table: "GameDraftEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GameDraftEntries_TeamNumber",
                table: "GameDraftEntries");

            migrationBuilder.AlterColumn<string>(
                name: "TrinketId",
                table: "GamePlayerStats",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SummonerSpell2Id",
                table: "GamePlayerStats",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SummonerSpell1Id",
                table: "GamePlayerStats",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "GamePlayerStats",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PlayerName",
                table: "GamePlayerStats",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "ItemIds",
                table: "GamePlayerStats",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Champion",
                table: "GamePlayerStats",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Phase",
                table: "GameDraftEntries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Champion",
                table: "GameDraftEntries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_GameTeamStats_GameId",
                table: "GameTeamStats",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayerStats_GameId",
                table: "GamePlayerStats",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameDraftEntries_GameId",
                table: "GameDraftEntries",
                column: "GameId");
        }
    }
}

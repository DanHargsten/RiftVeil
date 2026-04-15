using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiftVeil.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameDetailStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameDraftEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    TeamNumber = table.Column<int>(type: "int", nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Champion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameDraftEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameDraftEntries_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GamePlayerStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    TeamNumber = table.Column<int>(type: "int", nullable: false),
                    PlayerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Champion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kills = table.Column<int>(type: "int", nullable: false),
                    Deaths = table.Column<int>(type: "int", nullable: false),
                    Assists = table.Column<int>(type: "int", nullable: false),
                    GoldEarned = table.Column<int>(type: "int", nullable: false),
                    CreepScore = table.Column<int>(type: "int", nullable: false),
                    DamageDealtToChampions = table.Column<int>(type: "int", nullable: false),
                    VisionScore = table.Column<int>(type: "int", nullable: false),
                    ItemIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrinketId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SummonerSpell1Id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SummonerSpell2Id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePlayerStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GamePlayerStats_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameTeamStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    TeamNumber = table.Column<int>(type: "int", nullable: false),
                    TotalKills = table.Column<int>(type: "int", nullable: false),
                    TotalDeaths = table.Column<int>(type: "int", nullable: false),
                    TotalAssists = table.Column<int>(type: "int", nullable: false),
                    TotalGoldEarned = table.Column<int>(type: "int", nullable: false),
                    TowersDestroyed = table.Column<int>(type: "int", nullable: false),
                    InhibitorsDestroyed = table.Column<int>(type: "int", nullable: false),
                    BaronsSlain = table.Column<int>(type: "int", nullable: false),
                    RiftHeraldsSlain = table.Column<int>(type: "int", nullable: false),
                    VoidGrubsSlain = table.Column<int>(type: "int", nullable: false),
                    TotalDragonsSlain = table.Column<int>(type: "int", nullable: false),
                    InfernalDragonsSlain = table.Column<int>(type: "int", nullable: false),
                    MountainDragonsSlain = table.Column<int>(type: "int", nullable: false),
                    CloudDragonsSlain = table.Column<int>(type: "int", nullable: false),
                    OceanDragonsSlain = table.Column<int>(type: "int", nullable: false),
                    HextechDragonsSlain = table.Column<int>(type: "int", nullable: false),
                    ChemtechDragonsSlain = table.Column<int>(type: "int", nullable: false),
                    ElderDragonsSlain = table.Column<int>(type: "int", nullable: false),
                    GameDurationSeconds = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameTeamStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameTeamStats_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameDraftEntries_GameId",
                table: "GameDraftEntries",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayerStats_GameId",
                table: "GamePlayerStats",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameTeamStats_GameId",
                table: "GameTeamStats",
                column: "GameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameDraftEntries");

            migrationBuilder.DropTable(
                name: "GamePlayerStats");

            migrationBuilder.DropTable(
                name: "GameTeamStats");
        }
    }
}

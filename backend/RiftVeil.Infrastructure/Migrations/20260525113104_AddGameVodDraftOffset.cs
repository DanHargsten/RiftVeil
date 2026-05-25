using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiftVeil.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameVodDraftOffset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DraftOffsetSeconds",
                table: "GameVods",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OffsetSeconds",
                table: "GameVods",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DraftOffsetSeconds",
                table: "GameVods");

            migrationBuilder.AlterColumn<int>(
                name: "OffsetSeconds",
                table: "GameVods",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}

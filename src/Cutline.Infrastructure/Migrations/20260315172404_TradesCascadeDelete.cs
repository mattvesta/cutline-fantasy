using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cutline.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TradesCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trades_Teams_InitiatorTeamId",
                table: "Trades");

            migrationBuilder.DropForeignKey(
                name: "FK_Trades_Teams_ReceiverTeamId",
                table: "Trades");

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Teams_InitiatorTeamId",
                table: "Trades",
                column: "InitiatorTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Teams_ReceiverTeamId",
                table: "Trades",
                column: "ReceiverTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trades_Teams_InitiatorTeamId",
                table: "Trades");

            migrationBuilder.DropForeignKey(
                name: "FK_Trades_Teams_ReceiverTeamId",
                table: "Trades");

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Teams_InitiatorTeamId",
                table: "Trades",
                column: "InitiatorTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Teams_ReceiverTeamId",
                table: "Trades",
                column: "ReceiverTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

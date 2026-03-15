using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cutline.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerGameStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerGameStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Season = table.Column<int>(type: "integer", nullable: false),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    GameStatus = table.Column<string>(type: "text", nullable: false),
                    Opponent = table.Column<string>(type: "text", nullable: true),
                    PassingYards = table.Column<int>(type: "integer", nullable: true),
                    PassingTDs = table.Column<int>(type: "integer", nullable: true),
                    Interceptions = table.Column<int>(type: "integer", nullable: true),
                    PassingAttempts = table.Column<int>(type: "integer", nullable: true),
                    PassingCompletions = table.Column<int>(type: "integer", nullable: true),
                    RushingYards = table.Column<int>(type: "integer", nullable: true),
                    RushingTDs = table.Column<int>(type: "integer", nullable: true),
                    RushingAttempts = table.Column<int>(type: "integer", nullable: true),
                    Fumbles = table.Column<int>(type: "integer", nullable: true),
                    Receptions = table.Column<int>(type: "integer", nullable: true),
                    Targets = table.Column<int>(type: "integer", nullable: true),
                    ReceivingYards = table.Column<int>(type: "integer", nullable: true),
                    ReceivingTDs = table.Column<int>(type: "integer", nullable: true),
                    FieldGoalsMade = table.Column<int>(type: "integer", nullable: true),
                    FieldGoalsAttempted = table.Column<int>(type: "integer", nullable: true),
                    LongFieldGoal = table.Column<int>(type: "integer", nullable: true),
                    ExtraPointsMade = table.Column<int>(type: "integer", nullable: true),
                    ExtraPointsAttempted = table.Column<int>(type: "integer", nullable: true),
                    Sacks = table.Column<int>(type: "integer", nullable: true),
                    DefensiveInterceptions = table.Column<int>(type: "integer", nullable: true),
                    FumblesRecovered = table.Column<int>(type: "integer", nullable: true),
                    DefensiveTDs = table.Column<int>(type: "integer", nullable: true),
                    PointsAllowed = table.Column<int>(type: "integer", nullable: true),
                    Safeties = table.Column<int>(type: "integer", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerGameStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerGameStats_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGameStats_PlayerId_Season_WeekNumber",
                table: "PlayerGameStats",
                columns: new[] { "PlayerId", "Season", "WeekNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerGameStats");
        }
    }
}

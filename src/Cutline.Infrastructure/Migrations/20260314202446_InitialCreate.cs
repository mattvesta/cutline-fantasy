using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cutline.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leagues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Season = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RosterSettings_BenchSlots = table.Column<int>(type: "integer", nullable: false),
                    RosterSettings_DefSlots = table.Column<int>(type: "integer", nullable: false),
                    RosterSettings_FlexSlots = table.Column<int>(type: "integer", nullable: false),
                    RosterSettings_IrSlots = table.Column<int>(type: "integer", nullable: false),
                    RosterSettings_KSlots = table.Column<int>(type: "integer", nullable: false),
                    RosterSettings_QbSlots = table.Column<int>(type: "integer", nullable: false),
                    RosterSettings_RbSlots = table.Column<int>(type: "integer", nullable: false),
                    RosterSettings_SuperFlexSlots = table.Column<int>(type: "integer", nullable: false),
                    RosterSettings_TePremium = table.Column<bool>(type: "boolean", nullable: false),
                    RosterSettings_TeSlots = table.Column<int>(type: "integer", nullable: false),
                    RosterSettings_WrSlots = table.Column<int>(type: "integer", nullable: false),
                    ScoringSettings_FumbleLostPoints = table.Column<decimal>(type: "numeric", nullable: false),
                    ScoringSettings_InterceptionPoints = table.Column<decimal>(type: "numeric", nullable: false),
                    ScoringSettings_PassingTdPoints = table.Column<decimal>(type: "numeric", nullable: false),
                    ScoringSettings_PassingYardsPerPoint = table.Column<decimal>(type: "numeric", nullable: false),
                    ScoringSettings_ReceivingTdPoints = table.Column<decimal>(type: "numeric", nullable: false),
                    ScoringSettings_ReceivingYardsPerPoint = table.Column<decimal>(type: "numeric", nullable: false),
                    ScoringSettings_ReceptionPoints = table.Column<decimal>(type: "numeric", nullable: false),
                    ScoringSettings_RushingTdPoints = table.Column<decimal>(type: "numeric", nullable: false),
                    ScoringSettings_RushingYardsPerPoint = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leagues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GsisId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SleeperId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EspnId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Position = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NflTeam = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ByeWeek = table.Column<int>(type: "integer", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEliminated = table.Column<bool>(type: "boolean", nullable: false),
                    EliminatedWeek = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RosterSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotType = table.Column<int>(type: "integer", nullable: false),
                    IsStarter = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RosterSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RosterSlots_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RosterSlots_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Weeks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeagueId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EliminatedTeamId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Weeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Weeks_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Weeks_Teams_EliminatedTeamId",
                        column: x => x.EliminatedTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TeamScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Points = table.Column<decimal>(type: "numeric", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamScores_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamScores_Weeks_WeekId",
                        column: x => x.WeekId,
                        principalTable: "Weeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaiverClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DropPlayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    FaabBid = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaiverClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaiverClaims_Players_AddPlayerId",
                        column: x => x.AddPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaiverClaims_Players_DropPlayerId",
                        column: x => x.DropPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WaiverClaims_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WaiverClaims_Weeks_WeekId",
                        column: x => x.WeekId,
                        principalTable: "Weeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Players_EspnId",
                table: "Players",
                column: "EspnId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_GsisId",
                table: "Players",
                column: "GsisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_SleeperId",
                table: "Players",
                column: "SleeperId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterSlots_PlayerId",
                table: "RosterSlots",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterSlots_TeamId",
                table: "RosterSlots",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_LeagueId_OwnerUserId",
                table: "Teams",
                columns: new[] { "LeagueId", "OwnerUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamScores_TeamId",
                table: "TeamScores",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamScores_WeekId_TeamId",
                table: "TeamScores",
                columns: new[] { "WeekId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaiverClaims_AddPlayerId",
                table: "WaiverClaims",
                column: "AddPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_WaiverClaims_DropPlayerId",
                table: "WaiverClaims",
                column: "DropPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_WaiverClaims_TeamId",
                table: "WaiverClaims",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_WaiverClaims_WeekId",
                table: "WaiverClaims",
                column: "WeekId");

            migrationBuilder.CreateIndex(
                name: "IX_Weeks_EliminatedTeamId",
                table: "Weeks",
                column: "EliminatedTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Weeks_LeagueId_WeekNumber",
                table: "Weeks",
                columns: new[] { "LeagueId", "WeekNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RosterSlots");

            migrationBuilder.DropTable(
                name: "TeamScores");

            migrationBuilder.DropTable(
                name: "WaiverClaims");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Weeks");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Leagues");
        }
    }
}

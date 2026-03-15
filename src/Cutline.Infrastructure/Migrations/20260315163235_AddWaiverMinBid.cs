using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cutline.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWaiverMinBid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RosterSettings_MinFaabBid",
                table: "Leagues",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RosterSettings_MinFaabBid",
                table: "Leagues");
        }
    }
}

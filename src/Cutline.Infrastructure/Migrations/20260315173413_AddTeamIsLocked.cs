using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cutline.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamIsLocked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Teams",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "Teams");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WildBunch.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleActivePlaythrough : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_GameSessions_OneActive",
                table: "GameSessions",
                column: "Status",
                unique: true,
                filter: "\"Status\" = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_GameSessions_OneActive",
                table: "GameSessions");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WildBunch.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelDiaryProjectionWatermark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TravelDiaryProjectionDayCount",
                table: "GameSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TravelDiaryProjectionStreamVersion",
                table: "GameSessions",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TravelDiaryProjectionDayCount",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "TravelDiaryProjectionStreamVersion",
                table: "GameSessions");
        }
    }
}

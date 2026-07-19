using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WildBunch.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiaryDaySchemaVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SchemaVersion",
                table: "GameSessionTravelDiaryDays",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SchemaVersion",
                table: "GameSessionTravelDiaryDays");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WildBunch.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RetireUnusedSessionSchemaArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameSessionStoredEvents_StreamId_Sequence",
                table: "GameSessionStoredEvents");

            migrationBuilder.DropColumn(
                name: "SchemaVersion",
                table: "GameSessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SchemaVersion",
                table: "GameSessions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_GameSessionStoredEvents_StreamId_Sequence",
                table: "GameSessionStoredEvents",
                columns: new[] { "StreamId", "Sequence" },
                unique: true);
        }
    }
}

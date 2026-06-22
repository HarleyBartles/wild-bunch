using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WildBunch.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EventStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SnapshotVersion",
                table: "GameSessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StreamVersion",
                table: "GameSessions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "GameSessionStoredEvents",
                columns: table => new
                {
                    StreamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessionStoredEvents", x => new { x.StreamId, x.Sequence });
                    table.ForeignKey(
                        name: "FK_GameSessionStoredEvents_GameSessions_StreamId",
                        column: x => x.StreamId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameSessionStoredEvents_EventId",
                table: "GameSessionStoredEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameSessionStoredEvents_StreamId_Sequence",
                table: "GameSessionStoredEvents",
                columns: new[] { "StreamId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameSessionStoredEvents");

            migrationBuilder.DropColumn(
                name: "SnapshotVersion",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "StreamVersion",
                table: "GameSessions");
        }
    }
}

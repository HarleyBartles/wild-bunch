using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WildBunch.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ComposedSessionPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StateJson",
                table: "GameSessions");

            migrationBuilder.AddColumn<int>(
                name: "SchemaVersion",
                table: "GameSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TravelDifficulty",
                table: "GameSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GameSessionComponents",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: ColumnTypeForGuid(migrationBuilder), nullable: false),
                    ComponentName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ComponentVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: ColumnTypeForDateTime(migrationBuilder), nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessionComponents", x => new { x.SessionId, x.ComponentName });
                    table.ForeignKey(
                        name: "FK_GameSessionComponents_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameSessionLogEntries",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: ColumnTypeForGuid(migrationBuilder), nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false),
                    Turn = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessionLogEntries", x => new { x.SessionId, x.Sequence });
                    table.ForeignKey(
                        name: "FK_GameSessionLogEntries_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameSessionTravelDiaryDays",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: ColumnTypeForGuid(migrationBuilder), nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: ColumnTypeForDateTime(migrationBuilder), nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessionTravelDiaryDays", x => new { x.SessionId, x.Sequence });
                    table.ForeignKey(
                        name: "FK_GameSessionTravelDiaryDays_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameSessionComponents");

            migrationBuilder.DropTable(
                name: "GameSessionLogEntries");

            migrationBuilder.DropTable(
                name: "GameSessionTravelDiaryDays");

            migrationBuilder.DropColumn(
                name: "SchemaVersion",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "TravelDifficulty",
                table: "GameSessions");

            migrationBuilder.AddColumn<string>(
                name: "StateJson",
                table: "GameSessions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        private static string ColumnTypeForGuid(MigrationBuilder migrationBuilder)
        {
            return migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL"
                ? "uuid"
                : "TEXT";
        }

        private static string ColumnTypeForDateTime(MigrationBuilder migrationBuilder)
        {
            return migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL"
                ? "timestamp with time zone"
                : "TEXT";
        }
    }
}

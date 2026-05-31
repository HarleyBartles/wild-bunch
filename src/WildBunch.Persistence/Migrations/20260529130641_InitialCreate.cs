using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WildBunch.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: ColumnTypeForGuid(migrationBuilder), nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: ColumnTypeForDateTime(migrationBuilder), nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: ColumnTypeForDateTime(migrationBuilder), nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    StateJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameSessions");
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

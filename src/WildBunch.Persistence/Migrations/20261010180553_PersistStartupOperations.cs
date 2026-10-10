using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WildBunch.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistStartupOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StartupOperations",
                columns: table => new
                {
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StartupOperations", x => x.OperationId);
                    table.ForeignKey(
                        name: "FK_StartupOperations_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StartupOperations_SessionId_Kind",
                table: "StartupOperations",
                columns: new[] { "SessionId", "Kind" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StartupOperations");
        }
    }
}

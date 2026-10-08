using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WildBunch.Persistence;

namespace WildBunch.Persistence.Migrations;

[DbContext(typeof(WildBunchDbContext))]
[Migration("20261008180000_InvalidatePreAlphaPlaythroughs")]
public partial class InvalidatePreAlphaPlaythroughs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM \"GameSessions\";");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Pre-alpha playthrough data discarded by this migration cannot be restored.");
    }
}

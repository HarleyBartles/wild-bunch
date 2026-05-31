using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WildBunch.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class JsonbPayloadStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "GameSessionTravelDiaryDays"
                ALTER COLUMN "PayloadJson" TYPE jsonb USING "PayloadJson"::jsonb;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "GameSessionComponents"
                ALTER COLUMN "PayloadJson" TYPE jsonb USING "PayloadJson"::jsonb;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "GameSessionTravelDiaryDays"
                ALTER COLUMN "PayloadJson" TYPE text USING "PayloadJson"::text;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "GameSessionComponents"
                ALTER COLUMN "PayloadJson" TYPE text USING "PayloadJson"::text;
                """);
        }
    }
}

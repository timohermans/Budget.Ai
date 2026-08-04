using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.Web.Migrations
{
    /// <inheritdoc />
    public partial class BackfillNameOtherPartyNormalized : Migration
    {
        private const string NormalizeSql = """
            btrim(regexp_replace(
                regexp_replace(
                    regexp_replace(lower(btrim(name_other_party)), '\s+', ' ', 'g'),
                    '\s*-\s*', ' - ', 'g'),
                '\s+', ' ', 'g'))
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                UPDATE transactions
                SET name_other_party_normalized = {NormalizeSql}
                WHERE name_other_party_normalized = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

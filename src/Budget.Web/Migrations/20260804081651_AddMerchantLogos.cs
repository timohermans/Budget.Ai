using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Budget.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantLogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name_other_party_normalized",
                table: "transactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "merchants",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name_normalized = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_merchants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "merchant_aliases",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name_normalized = table.Column<string>(type: "text", nullable: false),
                    merchant_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_merchant_aliases", x => x.id);
                    table.ForeignKey(
                        name: "fk_merchant_aliases_merchants_merchant_id",
                        column: x => x.merchant_id,
                        principalTable: "merchants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_name_other_party_normalized",
                table: "transactions",
                column: "name_other_party_normalized");

            migrationBuilder.CreateIndex(
                name: "ix_merchant_aliases_merchant_id",
                table: "merchant_aliases",
                column: "merchant_id");

            migrationBuilder.CreateIndex(
                name: "ix_merchant_aliases_name_normalized",
                table: "merchant_aliases",
                column: "name_normalized",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_merchants_name_normalized",
                table: "merchants",
                column: "name_normalized",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "merchant_aliases");

            migrationBuilder.DropTable(
                name: "merchants");

            migrationBuilder.DropIndex(
                name: "ix_transactions_name_other_party_normalized",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "name_other_party_normalized",
                table: "transactions");
        }
    }
}

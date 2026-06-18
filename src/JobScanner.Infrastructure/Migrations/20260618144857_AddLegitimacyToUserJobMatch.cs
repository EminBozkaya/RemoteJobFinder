using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLegitimacyToUserJobMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Legitimacy",
                table: "user_job_matches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "High");

            migrationBuilder.AddColumn<string>(
                name: "LegitimacySignalsJson",
                table: "user_job_matches",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<bool>(
                name: "IsLikelyGhost",
                table: "eligibility_facts_cache",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Legitimacy",
                table: "user_job_matches");

            migrationBuilder.DropColumn(
                name: "LegitimacySignalsJson",
                table: "user_job_matches");

            migrationBuilder.DropColumn(
                name: "IsLikelyGhost",
                table: "eligibility_facts_cache");
        }
    }
}

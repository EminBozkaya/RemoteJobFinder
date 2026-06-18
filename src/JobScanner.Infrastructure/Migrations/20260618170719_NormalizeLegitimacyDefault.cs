using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeLegitimacyDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Legitimacy",
                table: "user_job_matches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValueSql: "'High'",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldDefaultValue: "High");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Legitimacy",
                table: "user_job_matches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "High",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldDefaultValueSql: "'High'");
        }
    }
}

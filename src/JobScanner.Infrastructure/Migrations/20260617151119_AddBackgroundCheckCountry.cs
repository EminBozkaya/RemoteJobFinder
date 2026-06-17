using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBackgroundCheckCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackgroundCheckCountry",
                table: "eligibility_facts_cache",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackgroundCheckCountry",
                table: "eligibility_facts_cache");
        }
    }
}

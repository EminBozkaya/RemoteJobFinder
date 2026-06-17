using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequiresRelocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresRelocation",
                table: "eligibility_facts_cache",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresRelocation",
                table: "eligibility_facts_cache");
        }
    }
}

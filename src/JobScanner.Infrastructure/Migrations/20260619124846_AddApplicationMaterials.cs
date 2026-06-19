using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationMaterials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "application_materials",
                columns: table => new
                {
                    ProfileId = table.Column<long>(type: "bigint", nullable: false),
                    JobId = table.Column<long>(type: "bigint", nullable: false),
                    CoverLetter = table.Column<string>(type: "text", nullable: false),
                    TailoredCvMarkdown = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    SourceCvHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    JobVersionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_materials", x => new { x.ProfileId, x.JobId });
                    table.ForeignKey(
                        name: "FK_application_materials_job_postings_JobId",
                        column: x => x.JobId,
                        principalTable: "job_postings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_application_materials_JobId",
                table: "application_materials",
                column: "JobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_materials");
        }
    }
}

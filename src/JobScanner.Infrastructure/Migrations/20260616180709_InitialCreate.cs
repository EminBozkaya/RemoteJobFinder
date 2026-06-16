using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace JobScanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eligibility_facts_cache",
                columns: table => new
                {
                    JobId = table.Column<long>(type: "bigint", nullable: false),
                    PromptVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VersionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequiresWorkAuth = table.Column<bool>(type: "boolean", nullable: true),
                    allowed_countries = table.Column<string>(type: "jsonb", nullable: true),
                    RequiresCitizenship = table.Column<bool>(type: "boolean", nullable: true),
                    AllowsB2BContractor = table.Column<bool>(type: "boolean", nullable: true),
                    EngagementType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MentionsEor = table.Column<bool>(type: "boolean", nullable: true),
                    EorPlatform = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DataBoundary = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TimezoneRequirementRaw = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsRecruiterAgency = table.Column<bool>(type: "boolean", nullable: true),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    ExtractedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eligibility_facts_cache", x => new { x.JobId, x.PromptVersion, x.ModelVersion, x.VersionHash });
                });

            migrationBuilder.CreateTable(
                name: "job_postings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IdentityKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Company = table.Column<string>(type: "text", nullable: false),
                    DescriptionText = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    ApplyUrl = table.Column<string>(type: "text", nullable: true),
                    WorkMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VersionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceExtraJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_postings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_job_matches",
                columns: table => new
                {
                    ProfileId = table.Column<long>(type: "bigint", nullable: false),
                    JobId = table.Column<long>(type: "bigint", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    ScoreBreakdownJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    Decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DecisionReasonsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Feedback = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_job_matches", x => new { x.ProfileId, x.JobId });
                    table.ForeignKey(
                        name: "FK_user_job_matches_job_postings_JobId",
                        column: x => x.JobId,
                        principalTable: "job_postings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "criteria_profiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    WorkMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResidenceCountry = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    required_keywords = table.Column<string>(type: "jsonb", nullable: false),
                    forbidden_keywords = table.Column<string>(type: "jsonb", nullable: false),
                    nice_keywords = table.Column<string>(type: "jsonb", nullable: false),
                    contract_types = table.Column<string>(type: "jsonb", nullable: false),
                    TimezoneToleranceHours = table.Column<int>(type: "integer", nullable: false),
                    SalaryMin = table.Column<decimal>(type: "numeric", nullable: true),
                    SalaryCurrency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    MinScoreToShow = table.Column<double>(type: "double precision", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_criteria_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_criteria_profiles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_criteria_profiles_IsActive",
                table: "criteria_profiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_criteria_profiles_UserId",
                table: "criteria_profiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_job_postings_IdentityKey",
                table: "job_postings",
                column: "IdentityKey");

            migrationBuilder.CreateIndex(
                name: "IX_job_postings_SourceName_ExternalId",
                table: "job_postings",
                columns: new[] { "SourceName", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_job_matches_JobId",
                table: "user_job_matches",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_user_job_matches_State",
                table: "user_job_matches",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            // Postgres FTS: Title (agirlik A) + DescriptionText (agirlik B) uzerinde
            // STORED generated tsvector + GIN index (ucuz keyword elemesi / arama icin).
            migrationBuilder.Sql(@"
                ALTER TABLE job_postings
                    ADD COLUMN search_vector tsvector
                    GENERATED ALWAYS AS (
                        setweight(to_tsvector('english', coalesce(""Title"", '')), 'A') ||
                        setweight(to_tsvector('english', coalesce(""DescriptionText"", '')), 'B')
                    ) STORED;");
            migrationBuilder.Sql(
                @"CREATE INDEX ix_job_postings_search_vector ON job_postings USING GIN (search_vector);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "criteria_profiles");

            migrationBuilder.DropTable(
                name: "eligibility_facts_cache");

            migrationBuilder.DropTable(
                name: "user_job_matches");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "job_postings");
        }
    }
}

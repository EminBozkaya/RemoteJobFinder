using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase5bSkillsLanguagesExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Faz 5b: keyword listeleri (string[]) yerine puanlı/yıllı yetkinlikler (obje[]) geldi.
            // Veri şekli uyumsuz olduğundan rename DEĞİL drop+add yapılır (eski string verisi obje
            // kolonuna taşınırsa okuma patlar). Mevcut profil boş skills/languages ile başlar; kullanıcı
            // Kriterler sayfasından doldurur.
            migrationBuilder.DropColumn(name: "required_keywords", table: "criteria_profiles");
            migrationBuilder.DropColumn(name: "nice_keywords", table: "criteria_profiles");

            migrationBuilder.AddColumn<string>(
                name: "skills", table: "criteria_profiles", type: "jsonb", nullable: false, defaultValue: "[]");
            migrationBuilder.AddColumn<string>(
                name: "languages", table: "criteria_profiles", type: "jsonb", nullable: false, defaultValue: "[]");
            migrationBuilder.AddColumn<string>(
                name: "soft_skills", table: "criteria_profiles", type: "jsonb", nullable: false, defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "required_experience", table: "eligibility_facts_cache", type: "jsonb", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "required_experience", table: "eligibility_facts_cache");
            migrationBuilder.DropColumn(name: "skills", table: "criteria_profiles");
            migrationBuilder.DropColumn(name: "languages", table: "criteria_profiles");
            migrationBuilder.DropColumn(name: "soft_skills", table: "criteria_profiles");

            migrationBuilder.AddColumn<string>(
                name: "required_keywords", table: "criteria_profiles", type: "jsonb", nullable: false, defaultValue: "[]");
            migrationBuilder.AddColumn<string>(
                name: "nice_keywords", table: "criteria_profiles", type: "jsonb", nullable: false, defaultValue: "[]");
        }
    }
}

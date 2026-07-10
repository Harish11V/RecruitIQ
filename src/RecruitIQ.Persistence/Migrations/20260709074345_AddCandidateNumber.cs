using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitIQ.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "Resumes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ParserVersion",
                table: "Resumes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CandidateNumber",
                table: "Candidates",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Candidates",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Candidates",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "Candidates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_CompanyId_CandidateNumber",
                table: "Candidates",
                columns: new[] { "CompanyId", "CandidateNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Candidates_CompanyId_CandidateNumber",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "ParserVersion",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "CandidateNumber",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "Candidates");
        }
    }
}

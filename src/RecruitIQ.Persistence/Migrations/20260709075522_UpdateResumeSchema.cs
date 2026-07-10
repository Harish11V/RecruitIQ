using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitIQ.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateResumeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "Resumes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "Resumes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "Resumes",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "MimeType",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "Resumes");
        }
    }
}

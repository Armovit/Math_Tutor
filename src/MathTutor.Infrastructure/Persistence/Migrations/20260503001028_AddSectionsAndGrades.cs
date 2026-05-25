using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathTutor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionsAndGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Grade",
                table: "TestAttempts",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "EducationalMaterials",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TestAttempts_Grade",
                table: "TestAttempts",
                sql: "[Grade] BETWEEN 2 AND 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TestAttempts_Grade",
                table: "TestAttempts");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "TestAttempts");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "EducationalMaterials");
        }
    }
}

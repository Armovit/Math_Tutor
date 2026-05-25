using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathTutor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "EducationalMaterials",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Topic",
                table: "EducationalMaterials");
        }
    }
}

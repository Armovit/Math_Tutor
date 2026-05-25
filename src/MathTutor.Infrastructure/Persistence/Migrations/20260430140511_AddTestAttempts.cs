using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathTutor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTestAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TestAttemptId",
                table: "TaskSubmissions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TestAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EducationalMaterialId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    Percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestAttempts", x => x.Id);
                    table.CheckConstraint("CK_TestAttempts_MaxScore", "[MaxScore] > 0");
                    table.CheckConstraint("CK_TestAttempts_Score", "[Score] >= 0");
                    table.ForeignKey(
                        name: "FK_TestAttempts_EducationalMaterials_EducationalMaterialId",
                        column: x => x.EducationalMaterialId,
                        principalTable: "EducationalMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestAttempts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskSubmissions_TestAttemptId",
                table: "TaskSubmissions",
                column: "TestAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_EducationalMaterialId",
                table: "TestAttempts",
                column: "EducationalMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_UserId",
                table: "TestAttempts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskSubmissions_TestAttempts_TestAttemptId",
                table: "TaskSubmissions",
                column: "TestAttemptId",
                principalTable: "TestAttempts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskSubmissions_TestAttempts_TestAttemptId",
                table: "TaskSubmissions");

            migrationBuilder.DropTable(
                name: "TestAttempts");

            migrationBuilder.DropIndex(
                name: "IX_TaskSubmissions_TestAttemptId",
                table: "TaskSubmissions");

            migrationBuilder.DropColumn(
                name: "TestAttemptId",
                table: "TaskSubmissions");
        }
    }
}

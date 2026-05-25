using MathTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathTutor.Infrastructure.Persistence.Migrations;

[DbContext(typeof(MathTutorDbContext))]
[Migration("20260518120000_UpdateGradeScaleToTen")]
public partial class UpdateGradeScaleToTen : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_TestAttempts_Grade",
            table: "TestAttempts");

        migrationBuilder.Sql("""
            UPDATE TestAttempts
            SET Grade = CASE
                WHEN [Percent] <= 0 THEN 1
                WHEN ROUND([Percent] / 10.0, 0) < 1 THEN 1
                WHEN ROUND([Percent] / 10.0, 0) > 10 THEN 10
                ELSE CAST(ROUND([Percent] / 10.0, 0) AS int)
            END
            """);

        migrationBuilder.AddCheckConstraint(
            name: "CK_TestAttempts_Grade",
            table: "TestAttempts",
            sql: "[Grade] BETWEEN 1 AND 10");

        migrationBuilder.AlterColumn<int>(
            name: "Grade",
            table: "TestAttempts",
            type: "int",
            nullable: false,
            defaultValue: 1,
            oldClrType: typeof(int),
            oldType: "int",
            oldDefaultValue: 2);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_TestAttempts_Grade",
            table: "TestAttempts");

        migrationBuilder.Sql("""
            UPDATE TestAttempts
            SET Grade = CASE
                WHEN [Percent] >= 85 THEN 5
                WHEN [Percent] >= 70 THEN 4
                WHEN [Percent] >= 50 THEN 3
                ELSE 2
            END
            """);

        migrationBuilder.AddCheckConstraint(
            name: "CK_TestAttempts_Grade",
            table: "TestAttempts",
            sql: "[Grade] BETWEEN 2 AND 5");

        migrationBuilder.AlterColumn<int>(
            name: "Grade",
            table: "TestAttempts",
            type: "int",
            nullable: false,
            defaultValue: 2,
            oldClrType: typeof(int),
            oldType: "int",
            oldDefaultValue: 1);
    }
}

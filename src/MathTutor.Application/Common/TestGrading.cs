namespace MathTutor.Application.Common;

public static class TestGrading
{
    public const int MinGrade = 1;
    public const int MaxGrade = 10;

    public static int CalculateGrade(decimal percent)
    {
        if (percent <= 0) return MinGrade;
        var grade = (int)Math.Round(percent / 10m, MidpointRounding.AwayFromZero);
        return Math.Clamp(grade, MinGrade, MaxGrade);
    }

    public static string FormatGrade(int grade) => $"{grade} из {MaxGrade}";
}

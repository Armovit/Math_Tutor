using FluentAssertions;
using MathTutor.Application.Common;

namespace MathTutor.UnitTests.Common;

public sealed class TestGradingTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(9, 1)]
    [InlineData(10, 1)]
    [InlineData(15, 2)]
    [InlineData(50, 5)]
    [InlineData(84, 8)]
    [InlineData(85, 9)]
    [InlineData(100, 10)]
    public void CalculateGrade_maps_percent_to_ten_point_scale(decimal percent, int expectedGrade)
        => TestGrading.CalculateGrade(percent).Should().Be(expectedGrade);

    [Fact]
    public void FormatGrade_displays_scale()
        => TestGrading.FormatGrade(7).Should().Be("7 из 10");
}

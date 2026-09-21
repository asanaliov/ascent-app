using ascent_app.Services;
using Xunit;

namespace Ascent.Tests;

public class DifficultyServiceTests
{
    // Score/GetLabel don't touch the ctx, so null! is fine here.
    private static DifficultyService Sut() => new DifficultyService(null!);

    [Fact]
    public void Score_ReturnsSqrtOfTwoTimesGainTimesDistance()
    {
        // sqrt(2 * 500 * 10) = sqrt(10000) = 100
        var score = Sut().Score(10, 500);
        Assert.Equal(100.0, score, 6);
    }

    [Theory]
    [InlineData(2, 100, "Easy")]       // 20    -> <50
    [InlineData(10, 300, "Moderate")]  // ~77.5 -> <100
    [InlineData(12, 500, "Hard")]      // ~109.5 -> <170
    [InlineData(16, 800, "Hard")]      // 160    -> <170
    [InlineData(20, 900, "Strenuous")] // ~189.7 -> >=170
    public void GetLabel_BinsScoreIntoExpectedBucket(double distKm, int gainM, string expected)
    {
        Assert.Equal(expected, Sut().GetLabel(distKm, gainM));
    }
}

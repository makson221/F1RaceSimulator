using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Strategies;

namespace F1RaceSimulator.Tests;

/// <summary>Тести стратегій піт-стопів (патерн Strategy).</summary>
public class StrategyTests
{
    private readonly Track _track = TestHelpers.SquareTrack(laps: 50);

    [Fact]
    public void DoesNotPit_WhenTyresAreFresh()
    {
        var car = TestHelpers.TestCar(TyreCompound.Medium, condition: 90);
        Assert.False(car.Strategy.ShouldPit(car, lapsRemaining: 30, _track));
    }

    [Fact]
    public void Pits_WhenBelowThreshold_AndTyresWillNotLastToTheEnd()
    {
        var car = TestHelpers.TestCar(TyreCompound.Medium, condition: 20);
        Assert.True(car.Strategy.ShouldPit(car, lapsRemaining: 20, _track));
    }

    [Fact]
    public void DoesNotPit_OnLastLap_EvenWithWornTyres()
    {
        var car = TestHelpers.TestCar(TyreCompound.Soft, condition: 3);
        Assert.False(car.Strategy.ShouldPit(car, lapsRemaining: 1, _track));
    }

    [Fact]
    public void Pits_WhenTyresAreCritical()
    {
        var car = TestHelpers.TestCar(TyreCompound.Hard, condition: ThresholdPitStrategy.CriticalCondition - 1);
        Assert.True(car.Strategy.ShouldPit(car, lapsRemaining: 30, _track));
    }

    [Fact]
    public void Pits_NearTheEnd_IfSecondCompoundWasNotUsed()
    {
        // Шини ще добрі, але правило двох складів вимагає піт-стоп.
        var car = TestHelpers.TestCar(TyreCompound.Hard, condition: 80);
        Assert.True(car.Strategy.ShouldPit(car, lapsRemaining: ThresholdPitStrategy.MandatoryStopWindow, _track));
    }

    [Fact]
    public void ChoosesSofterTyres_WhenFewLapsRemain()
    {
        var car = TestHelpers.TestCar(TyreCompound.Hard, condition: 20);
        Assert.Equal(TyreCompound.Soft, car.Strategy.ChooseCompound(car, lapsRemaining: 8, _track));
    }

    [Fact]
    public void ChoosesHardTyres_ForLongStint()
    {
        var car = TestHelpers.TestCar(TyreCompound.Medium, condition: 20);
        Assert.Equal(TyreCompound.Hard, car.Strategy.ChooseCompound(car, lapsRemaining: 33, _track));
    }

    [Fact]
    public void DoesNotRepeatCompound_IfTwoCompoundsNotYetUsed()
    {
        var car = TestHelpers.TestCar(TyreCompound.Soft, condition: 10);
        TyreCompound choice = car.Strategy.ChooseCompound(car, lapsRemaining: 5, _track);
        Assert.NotEqual(TyreCompound.Soft, choice);
    }

    [Fact]
    public void AggressiveStrategy_HasHigherThreshold()
    {
        var aggressive = new AggressivePitStrategy(25);
        var careful = new ThresholdPitStrategy(25);
        Assert.Equal(35, aggressive.Threshold);
        Assert.True(aggressive.Threshold > careful.Threshold);
    }

    [Fact]
    public void AggressiveStrategy_PitsEarlierThanCareful()
    {
        var aggressive = TestHelpers.TestCar(TyreCompound.Medium, 30, new AggressivePitStrategy(25));
        var careful = TestHelpers.TestCar(TyreCompound.Medium, 30, new ThresholdPitStrategy(25));

        Assert.True(aggressive.Strategy.ShouldPit(aggressive, 20, _track));
        Assert.False(careful.Strategy.ShouldPit(careful, 20, _track));
    }

    [Fact]
    public void Factory_CreatesStrategyByName()
    {
        Assert.IsType<AggressivePitStrategy>(PitStrategyFactory.Create("aggressive", 25));
        Assert.IsType<ThresholdPitStrategy>(PitStrategyFactory.Create("threshold", 25));
        Assert.Throws<ArgumentException>(() => PitStrategyFactory.Create("random", 25));
    }

    [Fact]
    public void InvalidThreshold_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ThresholdPitStrategy(150));
    }
}

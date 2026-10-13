using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Tyres;

namespace F1RaceSimulator.Tests;

/// <summary>Тести ієрархії шин і фабрики шин.</summary>
public class TyreTests
{
    [Fact]
    public void NewTyre_HasFullCondition()
    {
        Tyre tyre = new MediumTyre();
        Assert.Equal(100, tyre.Condition);
        Assert.Equal(0, tyre.LapsUsed);
    }

    [Fact]
    public void SoftWearsFasterThanMedium_AndMediumFasterThanHard()
    {
        Tyre soft = new SoftTyre(), medium = new MediumTyre(), hard = new HardTyre();
        foreach (var t in new[] { soft, medium, hard })
            t.Wear(10, trackFactor: 1.0, driverFactor: 1.0);

        Assert.True(soft.Condition < medium.Condition);
        Assert.True(medium.Condition < hard.Condition);
    }

    [Fact]
    public void Wear_UsesTrackAndDriverFactors()
    {
        var tyre = new MediumTyre();
        tyre.Wear(laps: 10, trackFactor: 1.5, driverFactor: 1.0);

        // 3.1 % за коло × 10 кіл × 1.5 = 46.5 %
        Assert.Equal(100 - 46.5, tyre.Condition, precision: 6);
        Assert.Equal(10, tyre.LapsUsed, precision: 6);
    }

    [Fact]
    public void Condition_NeverGoesBelowZero()
    {
        var tyre = new SoftTyre();
        tyre.Wear(1000, 1, 1);
        Assert.Equal(0, tyre.Condition);
    }

    [Fact]
    public void Constructor_ClampsConditionToValidRange()
    {
        Assert.Equal(100, new HardTyre(150).Condition);
        Assert.Equal(0, new HardTyre(-5).Condition);
    }

    [Fact]
    public void WornTyre_IsSlowerThanNewTyre()
    {
        var fresh = new MediumTyre(100);
        var worn = new MediumTyre(20);
        Assert.True(worn.PaceMultiplier < fresh.PaceMultiplier);
    }

    [Fact]
    public void NewSoft_IsFasterThanNewHard()
    {
        Assert.True(new SoftTyre().PaceMultiplier > new HardTyre().PaceMultiplier);
    }

    [Fact]
    public void SoftTyre_HasPaceCliffBelowTwentyPercent()
    {
        // Поліморфізм: SoftTyre перевизначає PaceMultiplier.
        var soft = new SoftTyre(15);
        double withoutCliff = 1 + soft.PaceBonus - Tyre.PaceLossWhenWorn * (1 - 15 / 100.0) - soft.TemperaturePenalty;
        Assert.Equal(withoutCliff - 0.01, soft.PaceMultiplier, precision: 9);
    }

    [Theory]
    [InlineData("S", TyreCompound.Soft)]
    [InlineData("medium", TyreCompound.Medium)]
    [InlineData(" H ", TyreCompound.Hard)]
    public void Factory_CreatesTyreByCode(string code, TyreCompound expected)
    {
        Tyre tyre = TyreFactory.Create(code);
        Assert.Equal(expected, tyre.Compound);
    }

    [Fact]
    public void Factory_CreatesCorrectSubclass()
    {
        Assert.IsType<SoftTyre>(TyreFactory.Create(TyreCompound.Soft));
        Assert.IsType<MediumTyre>(TyreFactory.Create(TyreCompound.Medium));
        Assert.IsType<HardTyre>(TyreFactory.Create(TyreCompound.Hard));
    }

    [Fact]
    public void Factory_ThrowsOnUnknownCode()
    {
        Assert.Throws<ArgumentException>(() => TyreFactory.Create("X"));
    }

    [Fact]
    public void Letter_MatchesCompound()
    {
        Assert.Equal("S", new SoftTyre().Letter);
        Assert.Equal("M", new MediumTyre().Letter);
        Assert.Equal("H", new HardTyre().Letter);
    }
}

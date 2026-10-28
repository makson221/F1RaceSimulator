using F1RaceSimulator.Core.Components;
using F1RaceSimulator.Core.Simulation;
using F1RaceSimulator.Core.Tyres;

namespace F1RaceSimulator.Tests;

/// <summary>Тести компонентів боліда: температура шин, двигун, паливо, ERS.</summary>
public class ComponentTests
{
    /// <summary>Типовий контекст: 1 секунда їзди на колі 80 с.</summary>
    private static ComponentContext Racing(double push = 0.5, double air = 25, bool dirtyAir = false) =>
        new(Seconds: 1, Laps: 1 / 80.0, AirTemperature: air, Push: push, InDirtyAir: dirtyAir, IsRacing: true);

    private static void Run(CarComponent component, ComponentContext context, int seconds)
    {
        for (int i = 0; i < seconds; i++)
            component.Update(context);
    }

    // ---------- Шини ----------

    [Fact]
    public void NewTyre_IsColdAfterBlankets_AndLosesPace()
    {
        var tyre = new MediumTyre();

        Assert.Equal(Tyre.BlanketTemperature, tyre.Temperature, precision: 9);
        Assert.Equal(20, tyre.DegreesOutsideWindow, precision: 9);       // робоче вікно 90–112 °C
        Assert.True(tyre.TemperaturePenalty > 0);
        Assert.Equal(ComponentStatus.Warning, tyre.Status);
    }

    [Fact]
    public void Tyre_WarmsUpIntoWindowInAboutTwoLaps()
    {
        var tyre = new MediumTyre();
        Run(tyre, Racing(), seconds: 160);

        Assert.True(tyre.Temperature >= tyre.OptimalMinTemperature && tyre.Temperature <= tyre.OptimalMaxTemperature,
            $"{tyre.Temperature:0.0}");
        Assert.Equal(0, tyre.TemperaturePenalty, precision: 9);
    }

    [Fact]
    public void Tyre_DoesNotHeatInPitLane()
    {
        var tyre = new SoftTyre();
        Run(tyre, Racing() with { IsRacing = false, Laps = 0 }, seconds: 60);
        Assert.Equal(Tyre.BlanketTemperature, tyre.Temperature, precision: 9);
    }

    [Fact]
    public void DirtyAir_HeatsTyresMore()
    {
        var clean = new MediumTyre();
        var dirty = new MediumTyre();
        Run(clean, Racing(), 300);
        Run(dirty, Racing(dirtyAir: true), 300);
        Assert.True(dirty.Temperature > clean.Temperature);
    }

    [Fact]
    public void SoftTyre_HeatsMoreThanHard()
    {
        // Поліморфізм: однаковий виклик Update, але склади гріються по-різному.
        Tyre soft = new SoftTyre(), hard = new HardTyre();
        Run(soft, Racing(), 300);
        Run(hard, Racing(), 300);
        Assert.True(soft.Temperature > hard.Temperature);
    }

    [Fact]
    public void OverheatedTyre_WearsFaster()
    {
        var hot = new SoftTyre();
        var normal = new SoftTyre();
        Run(hot, Racing(push: 1, air: 45, dirtyAir: true), 400);
        Assert.True(hot.IsOverheating);
        Assert.True(hot.OverheatWearFactor > 1);

        hot.Wear(1, 1, 1);
        normal.Wear(1, 1, 1);
        Assert.True(hot.Condition < normal.Condition);
    }

    // ---------- Двигун ----------

    [Fact]
    public void PowerUnit_HeatsMoreWhenAttacking()
    {
        var saving = new PowerUnit();
        var attack = new PowerUnit();
        Run(saving, Racing(push: 0.25), 300);
        Run(attack, Racing(push: 1), 300);
        Assert.True(attack.Temperature > saving.Temperature + 5);
    }

    [Fact]
    public void PowerUnit_StaysBelowWarningInNormalConditions()
    {
        var engine = new PowerUnit(initialWear: 20);
        Run(engine, Racing(push: 0.5, air: 25), 600);
        Assert.True(engine.Temperature < PowerUnit.WarningTemperature);
        Assert.Equal(ComponentStatus.Normal, engine.Status);
        Assert.Equal(1.0, engine.PaceFactor, precision: 9);
    }

    [Fact]
    public void PowerUnit_WearGrowsWithDistance()
    {
        var engine = new PowerUnit();
        engine.Update(new ComponentContext(1, Laps: 10, 25, 0.5, false, true));
        Assert.Equal(10 * PowerUnit.WearPerLap, engine.Wear, precision: 9);
    }

    [Fact]
    public void WornPowerUnit_IsRiskier()
    {
        var fresh = new PowerUnit(10);
        var worn = new PowerUnit(80);

        Assert.True(worn.FailureRiskMultiplier > fresh.FailureRiskMultiplier);
        Assert.Equal(ComponentStatus.Warning, worn.Status);
        Assert.Equal(0.2, worn.Level, precision: 9);
    }

    // ---------- Паливо ----------

    [Fact]
    public void FuelTank_IsFilledForWholeRace()
    {
        var fuel = new FuelTank();
        fuel.FillForRace(laps: 50, lapLengthKm: 5);

        Assert.Equal(5 * FuelTank.KgPerKm, fuel.ConsumptionPerLap, precision: 9);
        Assert.Equal(50 * fuel.ConsumptionPerLap + FuelTank.ReserveKg + FuelTank.SampleKg, fuel.CapacityKg, precision: 9);
        Assert.True(fuel.LapsLeft > 50);
        Assert.Equal(1.0, fuel.Level, precision: 9);
    }

    [Fact]
    public void FuelTank_RejectsInvalidRace()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FuelTank().FillForRace(0, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FuelTank().FillForRace(50, -1));
    }

    [Fact]
    public void Attack_BurnsMoreFuelThanSaving()
    {
        var saving = new FuelTank();
        var attack = new FuelTank();
        saving.FillForRace(50, 5);
        attack.FillForRace(50, 5);

        saving.Update(new ComponentContext(80, Laps: 10, 25, Push: 0.25, false, true));
        attack.Update(new ComponentContext(80, Laps: 10, 25, Push: 1, false, true));

        Assert.True(attack.FuelKg < saving.FuelKg);
    }

    [Fact]
    public void FullTank_MakesCarSlower_EmptyTankFaster()
    {
        var fuel = new FuelTank();
        fuel.FillForRace(50, 5);
        Assert.True(fuel.LapTimePenaltySeconds > 0);

        fuel.Update(new ComponentContext(80, Laps: 49, 25, 0.5, false, true));
        Assert.True(fuel.LapTimePenaltySeconds < 0);
    }

    [Fact]
    public void FuelTank_NeverGoesNegative()
    {
        var fuel = new FuelTank();
        fuel.FillForRace(10, 5);
        fuel.Update(new ComponentContext(80, Laps: 100, 25, 1, false, true));

        Assert.Equal(0, fuel.FuelKg, precision: 9);
        Assert.True(fuel.IsEmpty);
        Assert.Equal(ComponentStatus.Critical, fuel.Status);
    }

    // ---------- ERS ----------

    [Fact]
    public void Ers_DeploysInAttack_AndRechargesOtherwise()
    {
        var battery = new ErsBattery();

        battery.Update(new ComponentContext(80, Laps: 1, 25, Push: 1, false, true));
        Assert.True(battery.IsDeploying);
        Assert.Equal(100 + ErsBattery.HarvestPerLap - ErsBattery.DeployPerLap, battery.Charge, precision: 9);
        Assert.True(battery.PaceFactor > 1);

        battery.Update(new ComponentContext(80, Laps: 1, 25, Push: 0.5, false, true));
        Assert.False(battery.IsDeploying);
        Assert.Equal(1.0, battery.PaceFactor, precision: 9);
        Assert.Equal(100 - ErsBattery.DeployPerLap + 2 * ErsBattery.HarvestPerLap, battery.Charge, precision: 9);
    }

    [Fact]
    public void Ers_NeedsMinimumChargeToStartDeploying()
    {
        var battery = new ErsBattery(charge: 30);
        battery.Update(new ComponentContext(1, Laps: 0.01, 25, Push: 1, false, true));
        Assert.False(battery.IsDeploying);
    }

    // ---------- Болід ----------

    [Fact]
    public void Car_HasFourComponents()
    {
        var car = TestHelpers.TestCar();

        Assert.Equal(4, car.Components.Count);
        Assert.Same(car.Tyre, car.Components[0]);
        Assert.Same(car.Engine, car.Components[1]);
        Assert.Same(car.Fuel, car.Components[2]);
        Assert.Same(car.Battery, car.Components[3]);
    }

    [Fact]
    public void PrepareForRace_FillsFuelAndSetsEngineWear()
    {
        var car = TestHelpers.TestCar();
        car.PrepareForRace(TestHelpers.SquareTrack(laps: 50), engineWear: 20);

        Assert.Equal(50 * 4 * FuelTank.KgPerKm + FuelTank.ReserveKg + FuelTank.SampleKg, car.Fuel.CapacityKg, precision: 9);
        Assert.Equal(20, car.Engine.Wear, precision: 9);
    }
}

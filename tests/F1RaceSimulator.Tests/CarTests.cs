using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Simulation;
using F1RaceSimulator.Core.Tyres;

namespace F1RaceSimulator.Tests;

/// <summary>Тести класу Car: інкапсуляція стану і піт-стоп.</summary>
public class CarTests
{
    [Fact]
    public void Advance_MovesCarAndWearsTyres()
    {
        var car = TestHelpers.TestCar(TyreCompound.Medium);
        car.Advance(2.0, trackWearFactor: 1.0);

        Assert.Equal(2.0, car.Distance, precision: 9);
        Assert.Equal(2, car.CompletedLaps);
        Assert.True(car.Tyre.Condition < 100);
    }

    [Fact]
    public void CarefulDriver_WearsTyresSlower()
    {
        var careful = new Car(TestHelpers.TestDriver(tyreManagement: 0.96), TestHelpers.TestTeam(),
            new MediumTyre(), new Core.Strategies.ThresholdPitStrategy());
        var aggressive = new Car(TestHelpers.TestDriver(tyreManagement: 0.80), TestHelpers.TestTeam(),
            new MediumTyre(), new Core.Strategies.ThresholdPitStrategy());

        careful.Advance(10, 1);
        aggressive.Advance(10, 1);

        Assert.True(careful.Tyre.Condition > aggressive.Tyre.Condition);
    }

    [Fact]
    public void PitStopCycle_ChangesTyresAndReturnsToTrack()
    {
        var car = TestHelpers.TestCar(TyreCompound.Medium, condition: 20);
        car.Advance(5.9, 1);

        car.EnterPitLane(pitSpan: 0.2, boxPosition: 0.5);
        Assert.Equal(CarState.InPitLane, car.State);

        bool atBox = car.MoveInPitLane(0.6);         // доїхав до боксу
        Assert.True(atBox);
        Assert.Equal(CarState.PitStop, car.State);

        car.BeginStop(2.5);
        Assert.False(car.TickStop(1.0));              // механіки ще працюють
        Assert.True(car.TickStop(2.0));               // зупинку завершено
        car.ChangeTyres(TyreFactory.Create(TyreCompound.Hard));

        car.MoveInPitLane(0.6);                       // доїхав до виїзду
        Assert.Equal(CarState.OnTrack, car.State);
        Assert.Equal(1, car.PitStops);
        Assert.Equal(100, car.Tyre.Condition);
        Assert.True(car.HasUsedTwoCompounds);
        Assert.Equal(5.9 + 0.2, car.Distance, precision: 9);
    }

    [Fact]
    public void Advance_ThrowsWhenCarIsInPits()
    {
        var car = TestHelpers.TestCar();
        car.EnterPitLane(0.1, 0.5);
        Assert.Throws<InvalidOperationException>(() => car.Advance(0.1, 1));
    }

    [Fact]
    public void RetiredCar_IsNotRunning()
    {
        var car = TestHelpers.TestCar();
        car.Retire("аварія");
        Assert.False(car.IsRunning);
        Assert.Equal("аварія", car.RetirementReason);
    }

    [Fact]
    public void FasterDriverAndCar_HaveHigherBasePace()
    {
        var fast = TestHelpers.TestCar(pace: 1.0);
        var slow = TestHelpers.TestCar(pace: 0.98);
        Assert.True(fast.BasePace > slow.BasePace);
    }
}

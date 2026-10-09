using F1RaceSimulator.Core.Season;
using F1RaceSimulator.Core.Simulation;

namespace F1RaceSimulator.Tests;

/// <summary>Інтеграційні тести гонки на справжніх даних сезону.</summary>
public class RaceTests
{
    private static (Race Race, EventLogObserver Log) RunRace(string trackId, int seed, double retirementChance = 0)
    {
        var data = TestHelpers.Season;
        var track = data.Tracks.First(t => t.Id == trackId);
        var random = new SystemRandomSource(seed);

        var cars = CarFactory.CreateCars(data);
        var qualifying = new Qualifying(random).Run(cars, track);
        var grid = CarFactory.CreateGrid(data, qualifying);

        var race = new Race(track, grid, random, retirementChance);
        var log = new EventLogObserver();
        race.Subscribe(log);
        race.RunToFinish();
        return (race, log);
    }

    [Fact]
    public void Race_FinishesWithAllCarsClassified()
    {
        var (race, _) = RunRace("it-1922", seed: 1);

        Assert.True(race.IsFinished);
        Assert.All(race.Cars, car => Assert.Equal(CarState.Finished, car.State));
        Assert.Equal(53, race.Leader.FinishedLaps);
    }

    [Fact]
    public void Positions_AreUniqueAndSequential()
    {
        var (race, _) = RunRace("mc-1929", seed: 2);
        var positions = race.Standings.Select(c => c.Position).ToList();
        Assert.Equal(Enumerable.Range(1, race.Cars.Count), positions);
    }

    [Fact]
    public void EveryFinisher_UsedTwoCompounds()
    {
        // Регламент: щонайменше два різні склади шин у суху гонку.
        var (race, _) = RunRace("gb-1948", seed: 3);
        Assert.All(race.Cars, car => Assert.True(car.HasUsedTwoCompounds, car.Driver.Code));
    }

    [Fact]
    public void AbrasiveTrack_NeedsMorePitStops()
    {
        var (qatar, _) = RunRace("qa-2004", seed: 4);    // TyreWearFactor 1.35
        var (monaco, _) = RunRace("mc-1929", seed: 4);   // TyreWearFactor 0.60
        Assert.True(qatar.TotalPitStops > monaco.TotalPitStops);
    }

    [Fact]
    public void Observers_ReceiveEvents()
    {
        var (race, log) = RunRace("au-1953", seed: 5);

        Assert.Equal(1, log.Count(RaceEventType.RaceStarted));
        Assert.Equal(1, log.Count(RaceEventType.LeaderFinished));
        Assert.Equal(1, log.Count(RaceEventType.RaceFinished));
        Assert.Equal(race.TotalPitStops, log.Count(RaceEventType.PitStop));
        Assert.True(log.Count(RaceEventType.PitEntry) >= race.TotalPitStops);
    }

    [Fact]
    public void SameSeed_GivesSameResult()
    {
        var (first, _) = RunRace("jp-1962", seed: 42);
        var (second, _) = RunRace("jp-1962", seed: 42);

        Assert.Equal(
            first.Standings.Select(c => c.Driver.Code),
            second.Standings.Select(c => c.Driver.Code));
    }

    [Fact]
    public void Results_GivePointsToTopTen()
    {
        var (race, _) = RunRace("it-1922", seed: 6);
        var results = race.GetResults();

        Assert.Equal(25, results[0].Points);
        Assert.Equal(18, results[1].Points);
        Assert.Equal(1, results[9].Points);
        Assert.Equal(0, results[10].Points);
        Assert.Equal(101, results.Sum(r => r.Points));
    }

    [Fact]
    public void RetiredCars_AreClassifiedLast()
    {
        var (race, log) = RunRace("sg-2008", seed: 7, retirementChance: 0.01);
        Assert.True(log.Count(RaceEventType.Retirement) > 0);

        var standings = race.Standings;
        int firstRetired = standings.ToList().FindIndex(c => c.State == CarState.Retired);
        Assert.All(standings.Skip(firstRetired), c => Assert.Equal(CarState.Retired, c.State));
    }

    [Fact]
    public void Step_BeforeStart_Throws()
    {
        var data = TestHelpers.Season;
        var race = new Race(data.Tracks[0], CarFactory.CreateCars(data), new FixedRandomSource(0.5));
        Assert.Throws<InvalidOperationException>(() => race.Step(1));
    }

    [Fact]
    public void Crossed_DetectsPitEntryLine()
    {
        Assert.True(Race.Crossed(4.85, 4.95, 0.9));
        Assert.False(Race.Crossed(4.80, 4.85, 0.9));
        Assert.True(Race.Crossed(4.95, 5.05, 0.0));   // лінія старту/фінішу
    }

    [Fact]
    public void Qualifying_SortsByLapTime()
    {
        var data = TestHelpers.Season;
        var results = new Qualifying(new SystemRandomSource(1)).Run(CarFactory.CreateCars(data), data.Tracks[0]);

        Assert.Equal(22, results.Count);
        for (int i = 1; i < results.Count; i++)
            Assert.True(results[i - 1].LapTimeSeconds <= results[i].LapTimeSeconds);
    }

    [Fact]
    public void LapTime_IsFormattedAsMinutesSecondsMillis()
    {
        Assert.Equal("1:21.345", QualifyingResult.FormatLapTime(81.345));
    }
}

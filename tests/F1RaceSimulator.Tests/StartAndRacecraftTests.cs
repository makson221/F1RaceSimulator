using F1RaceSimulator.Core.Geometry;
using F1RaceSimulator.Core.Season;
using F1RaceSimulator.Core.Simulation;
using F1RaceSimulator.Core.Tyres;

namespace F1RaceSimulator.Tests;

/// <summary>
/// Тести стартової процедури (прогрівальне коло, вогні, реакція), обгонів у зонах гальмування,
/// сейфті-кара і повільних піт-стопів.
/// </summary>
public class StartAndRacecraftTests
{
    private static (Race Race, EventLogObserver Log) NewRace(string trackId, int seed, double retirementChance = 0)
    {
        var data = TestHelpers.Season;
        var random = new SystemRandomSource(seed);
        var grid = CarFactory.CreateGrid(data, new Qualifying(random).Run(CarFactory.CreateCars(data), TestHelpers.TrackById(trackId)));
        var race = new Race(TestHelpers.TrackById(trackId), grid, random, retirementChance);
        var log = new EventLogObserver();
        race.Subscribe(log);
        return (race, log);
    }

    private static void StepUntil(Race race, Func<bool> condition, double step = 0.5, double limitSeconds = 4000)
    {
        double done = 0;
        while (!condition())
        {
            Assert.True(done < limitSeconds, "Умова не виконалася за відведений час.");
            race.Step(step);
            done += step;
        }
    }

    // ------------------------------------------------------------------
    // Прогрівальне коло і стартові вогні
    // ------------------------------------------------------------------

    [Fact]
    public void FormationLap_CarsDriveALapAndReturnToTheirGridSlots()
    {
        var (race, log) = NewRace("gb-1948", seed: 51);
        race.Start();
        Assert.Equal(RaceStartPhase.FormationLap, race.StartPhase);
        Assert.Equal(1, log.Count(RaceEventType.FormationLap));
        var grid = race.Cars.ToDictionary(c => c, c => c.Distance);

        // Посеред прогрівального кола поулмен уже далеко від решітки, а порядок не змінився.
        StepUntil(race, () => race.FormationLapProgress > 0.5);
        Assert.Equal(race.Cars.Select(c => c.Driver.Code), race.Standings.Select(c => c.Driver.Code));
        Assert.InRange(race.Cars[0].Telemetry.SpeedKmh, 60, 220);

        StepUntil(race, () => race.StartPhase == RaceStartPhase.StartLights);
        Assert.Equal(0, race.ElapsedSeconds);                   // час гонки ще не пішов
        Assert.Equal(0, log.Count(RaceEventType.Overtake));     // на прогрівальному колі не обганяють
        double lapTime = race.Track.TypicalLapSeconds / Race.FormationLapPace;
        Assert.InRange(race.PreRaceSeconds, lapTime, lapTime * 1.4);

        // Кожен болід стоїть точно на своєму місці, шини прогрілися.
        foreach (CarPosition cp in race.GetCarPositions())
        {
            Assert.Equal(Polyline.Wrap(grid[cp.Car]), cp.Fraction, precision: 6);
            Assert.Equal(0, cp.Car.Telemetry.SpeedKmh);
            Assert.True(cp.Car.Tyre.Temperature > Tyre.BlanketTemperature + 5, cp.Car.Driver.Code);
        }
    }

    [Fact]
    public void StartLights_LightUpOneByOne_ThenGoOutAndRaceStarts()
    {
        var (race, log) = NewRace("it-1922", seed: 52);
        race.Start();
        StepUntil(race, () => race.StartPhase == RaceStartPhase.StartLights);
        Assert.Equal(1, log.Count(RaceEventType.StartLights));
        Assert.Equal(0, log.Count(RaceEventType.RaceStarted));

        var lights = new List<int>();
        double allFiveSince = double.NaN, clock = 0;
        while (race.StartPhase == RaceStartPhase.StartLights)
        {
            lights.Add(race.StartLightsOn);
            if (race.StartLightsOn == Race.StartLightCount && double.IsNaN(allFiveSince)) allFiveSince = clock;
            race.Step(0.05);
            clock += 0.05;
        }

        // Вогні тільки додаються: 0 → 1 → … → 5, кожен приблизно через секунду.
        for (int i = 1; i < lights.Count; i++)
            Assert.True(lights[i] >= lights[i - 1]);
        Assert.Equal(Enumerable.Range(0, 6), lights.Distinct());
        Assert.InRange(lights.Count(n => n == 2) * 0.05, 0.9, 1.1);

        // Після п'ятого вогню — випадкова пауза, потім усі гаснуть і стартує гонка.
        Assert.InRange(clock - allFiveSince, Race.MinLightsOutDelay - 0.06, Race.MaxLightsOutDelay + 0.06);
        Assert.Equal(RaceStartPhase.Racing, race.StartPhase);
        Assert.Equal(0, race.StartLightsOn);
        Assert.Equal(1, log.Count(RaceEventType.RaceStarted));
        Assert.True(race.IsRacing);
    }

    [Fact]
    public void Step_PausesWhenFormationLapEnds_SoLightsCanBeShownInRealTime()
    {
        var (race, _) = NewRace("at-1969", seed: 53);
        race.Start();
        StepUntil(race, () => race.FormationLapProgress > 0.99, step: 1);
        race.Step(60);                                          // великий крок не «перестрибує» вогні
        Assert.Equal(RaceStartPhase.StartLights, race.StartPhase);
        Assert.Equal(0, race.StartLightsOn);
    }

    [Fact]
    public void NobodyJumpsTheStart_ReactionTakesAtLeastATenth()
    {
        var (race, _) = NewRace("au-1953", seed: 54);
        race.Start(formationLap: false);
        race.Step(0.1);
        Assert.All(race.Cars, c => Assert.Equal(0, c.Telemetry.SpeedKmh));

        race.Step(1.9);
        Assert.All(race.Cars, c => Assert.True(c.Telemetry.SpeedKmh > 20, c.Driver.Code));
    }

    // ------------------------------------------------------------------
    // Обгони
    // ------------------------------------------------------------------

    [Fact]
    public void OvertakingZones_AreFoundAtBrakingPointsAfterStraights()
    {
        var monza = new Race(TestHelpers.TrackById("it-1922"), CarFactory.CreateCars(TestHelpers.Season), new SystemRandomSource(1));
        Assert.True(monza.OvertakingZones.Count >= 3);
        Assert.All(monza.OvertakingZones, z =>
        {
            Assert.InRange(z.Fraction, 0, 1);
            Assert.InRange(z.Strength, 0.15, 1);
        });
        // Перша шикана Монци — найкраще місце для обгону.
        Assert.Contains(monza.OvertakingZones, z => z.Fraction is > 0.1 and < 0.2 && z.Strength >= 0.9);
    }

    [Fact]
    public void Monaco_HasFarFewerOvertakesThanMonza()
    {
        int monaco = 0, monza = 0;
        for (int seed = 61; seed <= 62; seed++)
        {
            var (mc, _) = NewRace("mc-1929", seed);
            mc.RunToFinish();
            monaco += mc.TotalOvertakes;
            var (it, _) = NewRace("it-1922", seed);
            it.RunToFinish();
            monza += it.TotalOvertakes;
        }
        Assert.True(monza > monaco * 2, $"Монца {monza}, Монако {monaco}");
        Assert.InRange(monaco / 2.0, 0, 40);
    }

    [Fact]
    public void FasterCar_CannotDriveThroughSlowerOne_WhereOvertakingIsImpossible()
    {
        // Траса, де обігнати неможливо: швидший болід стартує другим і так і фінішує — впритул.
        var track = TestHelpers.SquareTrack(laps: 12, overtaking: 0);
        var slow = TestHelpers.TestCar(code: "SLO", pace: 0.990);
        var fast = TestHelpers.TestCar(code: "FST", pace: 1.000);
        var race = new Race(track, new[] { slow, fast }, new SystemRandomSource(3), retirementChancePerLap: 0);
        race.RunToFinish();

        Assert.Equal("SLO", race.Standings[0].Driver.Code);
        Assert.InRange(fast.FinishTime!.Value - slow.FinishTime!.Value, 0, 1.0);
        Assert.Equal(0, race.TotalOvertakes);
    }

    [Fact]
    public void FasterCar_Overtakes_WhereOvertakingIsEasy()
    {
        var track = TestHelpers.SquareTrack(laps: 12, overtaking: 1);
        var slow = TestHelpers.TestCar(code: "SLO", pace: 0.985);
        var fast = TestHelpers.TestCar(code: "FST", pace: 1.000);
        var race = new Race(track, new[] { slow, fast }, new SystemRandomSource(3), retirementChancePerLap: 0);
        race.RunToFinish();

        Assert.Equal("FST", race.Standings[0].Driver.Code);
        Assert.True(race.TotalOvertakes >= 1);
    }

    // ------------------------------------------------------------------
    // Сейфті-кар, інциденти, піт-стопи
    // ------------------------------------------------------------------

    [Fact]
    public void SafetyCar_BunchesTheFieldAndForbidsOvertaking()
    {
        // Висока ймовірність сходів: беремо першу з кількох гонок, де після аварії виїхав сейфті-кар.
        Race? race = null;
        EventLogObserver? log = null;
        for (int seed = 71; seed <= 80 && race == null; seed++)
        {
            var (candidate, candidateLog) = NewRace("es-1991", seed, retirementChance: 0.004);
            candidate.Start(formationLap: false);
            StepUntil(candidate, () => candidate.SafetyCar == SafetyCarMode.SafetyCar || candidate.IsFinished, step: 1, limitSeconds: 20000);
            if (candidate.SafetyCar == SafetyCarMode.SafetyCar && candidate.CurrentLap < candidate.TotalLaps - 6)
                (race, log) = (candidate, candidateLog);
        }
        Assert.NotNull(race);
        Assert.NotNull(race!.SafetyCarFraction);
        Assert.Contains(log!.Events, e => e.Type == RaceEventType.SafetyCar && e.Message.StartsWith("Сейфті-кар виїжджає"));

        int overtakes = race.TotalOvertakes;
        double spreadAtStart = Spread(race);
        double spreadMin = spreadAtStart;
        while (race.SafetyCar == SafetyCarMode.SafetyCar && !race.IsChequeredFlag)
        {
            race.Step(1);
            if (race.SafetyCar == SafetyCarMode.SafetyCar) Assert.Equal(overtakes, race.TotalOvertakes);
            spreadMin = Math.Min(spreadMin, Spread(race));
        }

        // Колона стиснулася: перша десятка — в межах кількох секунд (якщо до сейфті-кара була розтягнута).
        Assert.True(spreadMin < Math.Max(12, spreadAtStart * 0.5), $"{spreadAtStart:0.0} → {spreadMin:0.0}");
        Assert.True(race.SafetyCarCount >= 1);
    }

    /// <summary>Відрив десятого від лідера серед тих, хто на трасі, секунди.</summary>
    private static double Spread(Race race)
    {
        var running = race.Standings.Where(c => c.State == CarState.OnTrack).Take(10).ToList();
        return running.Count < 2 ? 0 : (running[0].Distance - running[^1].Distance) * race.Track.TypicalLapSeconds;
    }

    [Fact]
    public void WithoutRetirements_ThereAreNoCrashesOrSafetyCars()
    {
        var (race, log) = NewRace("sg-2008", seed: 72);
        race.RunToFinish();
        Assert.Equal(0, race.SafetyCarCount + race.VirtualSafetyCarCount);
        Assert.Equal(0, log.Count(RaceEventType.Retirement));
        Assert.All(race.Cars, c => Assert.Equal(CarState.Finished, c.State));
    }

    [Fact]
    public void PitStops_AreUsuallyQuick_ButSometimesSlow()
    {
        int quick = 0, slow = 0;
        for (int seed = 81; seed <= 83; seed++)
        {
            var (race, log) = NewRace("qa-2004", seed);
            race.RunToFinish();
            foreach (RaceEvent e in log.Events.Where(e => e.Type == RaceEventType.PitStop))
            {
                if (e.Message.Contains("повільний")) slow++;
                else quick++;
            }
        }
        Assert.True(slow >= 1, "За три гонки має бути хоча б один повільний піт-стоп.");
        Assert.True(quick > slow * 5, $"Швидких {quick}, повільних {slow}");
    }

    // ------------------------------------------------------------------
    // Дані пілотів
    // ------------------------------------------------------------------

    [Fact]
    public void Verstappen_HasTheBestPaceAndRacecraft()
    {
        var cars = CarFactory.CreateCars(TestHelpers.Season);
        Car max = cars.Single(c => c.Driver.Code == "VER");
        Assert.Equal(cars.Max(c => c.BasePace), max.BasePace);
        Assert.Equal(cars.Max(c => c.Driver.Racecraft), max.Driver.Racecraft);
        Assert.All(cars, c => Assert.InRange(c.Driver.Racecraft, 0.5, 1));
    }

    [Fact]
    public void Tracks_HaveOvertakingDifficulty()
    {
        var tracks = TestHelpers.Season.Tracks;
        Assert.All(tracks, t => Assert.InRange(t.Overtaking, 0.01, 1));
        Assert.Equal("mc-1929", tracks.MinBy(t => t.Overtaking)!.Id);   // у Монако обганяти найважче
    }
}

using F1RaceSimulator.Core.Components;
using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Season;
using F1RaceSimulator.Core.Simulation;

namespace F1RaceSimulator.Tests;

/// <summary>
/// Тести гоночного вікенду: офіційні схеми трас (повороти, сектори, профіль швидкості),
/// кваліфікація Q1–Q3, телеметрія, коло охолодження і парк-ферме після фінішу.
/// </summary>
public class WeekendTests
{
    private static Track TrackById(string id) => TestHelpers.Season.Tracks.First(t => t.Id == id);

    private static QualifyingSession RunQualifying(string trackId, int seed, EventLogObserver? log = null)
    {
        var data = TestHelpers.Season;
        var session = new QualifyingSession(TrackById(trackId), CarFactory.CreateCars(data), new SystemRandomSource(seed));
        if (log != null) session.Subscribe(log);
        session.Start();
        session.RunToEnd();
        return session;
    }

    // ------------------------------------------------------------------
    // Дані трас
    // ------------------------------------------------------------------

    [Fact]
    public void EveryTrack_HasNumberedTurnsSectorsAndSpeedProfile()
    {
        Assert.All(TestHelpers.Season.Tracks, track =>
        {
            TrackLayout layout = track.Layout;
            Assert.True(layout.Turns.Length >= 10, track.Id);
            Assert.All(layout.Turns, t => Assert.InRange(t.Fraction, 0, 1));

            // Номери поворотів ідуть за зростанням (10, 10A, 11 …).
            var numbers = layout.Turns.Select(t => t.Number).ToList();
            Assert.Equal(numbers.OrderBy(n => n), numbers);

            Assert.Equal(2, layout.Sectors.Length);
            Assert.True(0 < layout.Sectors[0] && layout.Sectors[0] < layout.Sectors[1] && layout.Sectors[1] < 1, track.Id);

            Assert.False(track.Profile.IsEmpty, track.Id);
            Assert.InRange(track.Profile.Speed.Max(), 260, 360);
            Assert.InRange(track.Profile.Speed.Min(), 40, 160);   // шпилька Монако — близько 45 км/год
        });
    }

    [Theory]
    [InlineData("it-1922", 11)]   // Монца
    [InlineData("mc-1929", 19)]   // Монако
    [InlineData("gb-1948", 18)]   // Сільверстоун
    [InlineData("es-2026", 22)]   // Мадрід (Madring)
    public void TurnCount_MatchesOfficialMap(string trackId, int turns)
    {
        Assert.Equal(turns, TrackById(trackId).Layout.Turns.Max(t => t.Number));
    }

    [Fact]
    public void SpeedProfile_AveragesToOneOverLap()
    {
        // Відносна швидкість нормована так, що інтеграл часу по колу дорівнює часу кола.
        SpeedProfile profile = TrackById("it-1922").Profile;
        const int n = 4000;
        double timeShare = Enumerable.Range(0, n).Sum(i => 1.0 / profile.RelativeSpeedAt((i + 0.5) / n)) / n;
        Assert.InRange(timeShare, 0.98, 1.02);
    }

    [Fact]
    public void Monza_IsFasterThanMonaco()
    {
        Assert.True(TrackById("it-1922").Profile.Speed.Max() > TrackById("mc-1929").Profile.Speed.Max() + 30);
    }

    [Fact]
    public void SectorAt_SplitsLapIntoThree()
    {
        Track track = TrackById("gb-1948");
        Assert.Equal(1, track.SectorAt(0.01));
        Assert.Equal(2, track.SectorAt((track.Layout.Sectors[0] + track.Layout.Sectors[1]) / 2));
        Assert.Equal(3, track.SectorAt(0.99));
    }

    // ------------------------------------------------------------------
    // Кваліфікація
    // ------------------------------------------------------------------

    [Fact]
    public void Qualifying_EliminatesSixInQ1AndSixInQ2()
    {
        var session = RunQualifying("it-1922", seed: 21);
        var results = session.GetResults();

        Assert.True(session.IsCompleted);
        Assert.Equal(22, results.Count);
        Assert.Equal(Enumerable.Range(1, 22), results.Select(r => r.Position));
        Assert.Equal(6, results.Count(r => r.EliminatedIn == QualifyingSegment.Q1));
        Assert.Equal(6, results.Count(r => r.EliminatedIn == QualifyingSegment.Q2));
        Assert.All(results.Take(10), r => Assert.Null(r.EliminatedIn));

        // Хто вибув у Q1 — позиції 17–22, у Q2 — 11–16.
        Assert.All(results.Skip(16), r => Assert.Equal(QualifyingSegment.Q1, r.EliminatedIn));
        Assert.All(results.Skip(10).Take(6), r => Assert.Equal(QualifyingSegment.Q2, r.EliminatedIn));
    }

    [Fact]
    public void Qualifying_Top10OrderedByQ3Time()
    {
        var results = RunQualifying("gb-1948", seed: 22).GetResults();
        var top10 = results.Take(10).Where(r => r.Q3 != null).Select(r => r.Q3!.Value).ToList();
        Assert.True(top10.Count >= 8);
        Assert.Equal(top10.OrderBy(t => t), top10);
        Assert.NotNull(results[0].Q3);
    }

    [Fact]
    public void Qualifying_LapTimesAreRealistic()
    {
        Track track = TrackById("it-1922");
        var results = RunQualifying(track.Id, seed: 23).GetResults();
        double pole = results[0].LapTimeSeconds;
        // Поул у Монці — близько 1:19–1:21.
        Assert.InRange(pole, track.TypicalLapSeconds * 0.88, track.TypicalLapSeconds * 0.98);
        Assert.All(results, r => Assert.InRange(r.LapTimeSeconds, pole, pole * 1.05));
    }

    [Fact]
    public void Qualifying_RaisesEventsForEverySegment()
    {
        var log = new EventLogObserver();
        RunQualifying("mc-1929", seed: 24, log);
        Assert.Equal(3, log.Count(RaceEventType.QualifyingSegmentStarted));
        Assert.Equal(1, log.Count(RaceEventType.QualifyingFinished));
        Assert.True(log.Count(RaceEventType.QualifyingLap) >= 22 + 16 + 10);
        Assert.All(log.Events, e => Assert.StartsWith("Q", e.ToString()));
    }

    [Fact]
    public void Qualifying_TakesAboutAnHour()
    {
        var session = RunQualifying("at-1969", seed: 25);
        // 18 + 15 + 12 хвилин і дві перерви по 2.5 хвилини, плюс останні кола після прапора.
        Assert.InRange(session.ElapsedSeconds / 60, 50, 62);
    }

    [Fact]
    public void Qualifying_GridFeedsRace()
    {
        var data = TestHelpers.Season;
        var results = RunQualifying("hu-1986", seed: 26).GetResults();
        var grid = CarFactory.CreateGrid(data, results);
        Assert.Equal(results.Select(r => r.Car.Driver.Code), grid.Select(c => c.Driver.Code));
    }

    // ------------------------------------------------------------------
    // Гонка: телеметрія, коло охолодження, парк-ферме
    // ------------------------------------------------------------------

    private static (Race Race, EventLogObserver Log) NewRace(string trackId, int seed)
    {
        var data = TestHelpers.Season;
        var race = new Race(TrackById(trackId), CarFactory.CreateCars(data), new SystemRandomSource(seed), 0);
        var log = new EventLogObserver();
        race.Subscribe(log);
        race.Start();
        return (race, log);
    }

    [Fact]
    public void Telemetry_FollowsOfficialSpeedProfile()
    {
        var (race, _) = NewRace("it-1922", seed: 31);
        double maxSpeed = 0;
        int maxGear = 0;
        bool braked = false;
        for (int i = 0; i < 600; i++)       // 5 хвилин гонки по 0.5 с
        {
            race.Step(0.5);
            foreach (Car car in race.Cars.Where(c => c.State == CarState.OnTrack))
            {
                maxSpeed = Math.Max(maxSpeed, car.Telemetry.SpeedKmh);
                maxGear = Math.Max(maxGear, car.Telemetry.Gear);
                braked |= car.Telemetry.Brake;
                Assert.InRange(car.Telemetry.Throttle, 0, 100);
            }
        }
        Assert.InRange(maxSpeed, 300, 360);    // Монца — найшвидша траса сезону
        Assert.Equal(8, maxGear);
        Assert.True(braked);
    }

    [Fact]
    public void StandingStart_CarsAccelerateFromZero()
    {
        var (race, _) = NewRace("gb-1948", seed: 32);
        race.Step(0.5);
        Assert.All(race.Cars, c => Assert.InRange(c.Telemetry.SpeedKmh, 0, 40));
    }

    [Fact]
    public void Finish_CarsDoCoolDownLapThenPark()
    {
        var (race, log) = NewRace("at-1969", seed: 33);
        race.RunToFinish();

        // Протокол уже остаточний, але боліди не зупиняються на лінії — їдуть коло охолодження.
        Assert.True(race.IsFinished);
        Assert.False(race.IsCompleted);
        Assert.Contains(race.Cars, c => c.PostRace == PostRaceStage.CoolDownLap);
        Car someone = race.Cars.First(c => c.PostRace == PostRaceStage.CoolDownLap);
        double before = someone.CoolDownDistance;
        race.Step(5);
        Assert.True(someone.CoolDownDistance > before);
        Assert.True(someone.Telemetry.SpeedKmh > 50);

        race.RunToCompletion();
        Assert.True(race.IsCompleted);
        Assert.All(race.Cars.Where(c => c.State == CarState.Finished),
            c => Assert.Equal(PostRaceStage.Parked, c.PostRace));
        Assert.Equal(1, log.Count(RaceEventType.CarsParked));
        Assert.Equal(1, log.Count(RaceEventType.RaceFinished));

        // У парк-ферме всі боліди стоять із вимкненими двигунами.
        Assert.All(race.Cars, c => Assert.Equal(0, c.Telemetry.SpeedKmh));
    }

    [Fact]
    public void AfterCoolDown_EveryFinisherKeepsFuelSample()
    {
        var (race, _) = NewRace("mx-1962", seed: 34);
        race.RunToCompletion();
        Assert.All(race.Cars.Where(c => c.State == CarState.Finished),
            c => Assert.True(c.Fuel.FuelKg >= FuelTank.SampleKg * 0.5, $"{c.Driver.Code}: {c.Fuel.FuelKg:0.00} кг"));
    }

    [Fact]
    public void FastestLap_IsRecordedAndMatchesTiming()
    {
        var (race, log) = NewRace("nl-1948", seed: 35);
        race.RunToFinish();
        Assert.NotNull(race.FastestLapCar);
        Assert.Equal(race.FastestLap, race.FastestLapCar!.Timing.BestLap);
        Assert.Equal(race.Cars.Min(c => c.Timing.BestLap ?? double.MaxValue), race.FastestLap);
        Assert.True(log.Count(RaceEventType.FastestLap) >= 1);
        // Найшвидше коло гонки близьке до реального (typicalLapSeconds = найкраще коло × 1.015).
        Assert.InRange(race.FastestLap!.Value, race.Track.TypicalLapSeconds * 0.95, race.Track.TypicalLapSeconds * 1.03);
    }

    [Fact]
    public void LapTimer_RecordsSectorsAndBestLap()
    {
        var timer = new LapTimer();
        timer.Start(0);
        timer.CompleteSector(1, 25);
        timer.CompleteSector(2, 55);
        Assert.Equal(80, timer.CompleteLap(80));
        timer.CompleteSector(1, 104);
        timer.CompleteSector(2, 133);
        Assert.Equal(78, timer.CompleteLap(158));

        Assert.Equal(78, timer.BestLap);
        Assert.Equal(2, timer.BestLapNumber);
        Assert.Equal(24, timer.BestSectors[0]);
        Assert.Equal(29, timer.BestSectors[1]);
        Assert.Equal(25, timer.BestSectors[2]);
        Assert.Equal("1:18.000", LapTimer.Format(78));
    }

    [Fact]
    public void Sessions_ImplementCommonInterface()
    {
        var data = TestHelpers.Season;
        Track track = TrackById("jp-1962");
        ITrackSession[] sessions =
        {
            new QualifyingSession(track, CarFactory.CreateCars(data), new SystemRandomSource(1)),
            new Race(track, CarFactory.CreateCars(data), new SystemRandomSource(1))
        };
        foreach (ITrackSession session in sessions)
        {
            Assert.Same(track, session.Track);
            if (session is Race race) race.Start(); else ((QualifyingSession)session).Start();
            session.Step(600);                 // 10 хвилин сесії
            Assert.True(session.ElapsedSeconds >= 600);
            Assert.False(session.IsCompleted);
            // У гонці на трасі всі 22 боліди, у кваліфікації — лише ті, хто виїхав з боксу.
            int expected = session is Race ? 22 : ((QualifyingSession)session).Cars.Count(q => q.Stage != QualifyingStage.Garage);
            Assert.Equal(expected, session.GetCarPositions().Count);
            Assert.All(session.GetCarPositions(), p => Assert.InRange(p.Fraction, 0, 1));
        }
    }
}

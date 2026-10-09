using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Season;
using F1RaceSimulator.Core.Simulation;
using F1RaceSimulator.Core.Strategies;
using F1RaceSimulator.Core.Tyres;

namespace F1RaceSimulator.Tests;

/// <summary>Спільні дані та заготовки для тестів.</summary>
internal static class TestHelpers
{
    private static readonly Lazy<SeasonData> LazySeason =
        new(() => DataLoader.LoadSeason(Path.Combine(AppContext.BaseDirectory, "Data")));

    /// <summary>Справжні дані сезону 2026 з папки Data.</summary>
    public static SeasonData Season => LazySeason.Value;

    /// <summary>Проста квадратна траса для тестів стратегій.</summary>
    public static Track SquareTrack(int laps = 50, double wearFactor = 1.0) => new()
    {
        Id = "test",
        Round = 99,
        GrandPrix = "Тестове Гран-прі",
        City = "Тест",
        Circuit = "Квадрат",
        Date = "2026-01-01",
        LengthKm = 4,
        Laps = laps,
        TypicalLapSeconds = 80,
        TyreWearFactor = wearFactor,
        PitLaneSeconds = 20,
        Layout = new TrackLayout
        {
            Points = new[] { new double[] { 0, 0 }, new double[] { 100, 0 }, new double[] { 100, 100 }, new double[] { 0, 100 } },
            PitLane = new[] { new double[] { 0, 90 }, new double[] { 5, 50 }, new double[] { 0, 10 } },
            PitEntry = 0.9,
            PitExit = 0.1,
            StartFinish = new double[] { -5, 0, 5, 0 },
            LabelPosition = new double[] { 20, 20 }
        }
    };

    public static Team TestTeam(string id = "TST", double performance = 1.0, int box = 1) => new()
    {
        Id = id,
        Name = "Test Team " + id,
        Color = "#123456",
        CarPerformance = performance,
        PitBoxOrder = box
    };

    public static Driver TestDriver(string code = "TST", string teamId = "TST", double pace = 1.0,
        double tyreManagement = 0.9, double consistency = 0.9) => new()
    {
        Code = code,
        FirstName = "Тест",
        LastName = code,
        TeamId = teamId,
        Pace = pace,
        Consistency = consistency,
        TyreManagement = tyreManagement,
        Strategy = "threshold",
        PitThreshold = 25
    };

    public static Car TestCar(TyreCompound compound = TyreCompound.Medium, double condition = 100,
        IPitStrategy? strategy = null, string code = "TST", double pace = 1.0) =>
        new(TestDriver(code, pace: pace), TestTeam(), TyreFactory.Create(compound, condition),
            strategy ?? new ThresholdPitStrategy(25));
}

/// <summary>Передбачуване «випадкове» джерело: завжди повертає одне число.</summary>
internal sealed class FixedRandomSource : IRandomSource
{
    private readonly double _value;

    public FixedRandomSource(double value) => _value = value;

    public double NextDouble() => _value;
}

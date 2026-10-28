using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Simulation;

/// <summary>Результат кваліфікації одного пілота.</summary>
/// <param name="Position">Позиція на стартовій решітці.</param>
/// <param name="Car">Болід.</param>
/// <param name="LapTimeSeconds">Час, що визначив позицію (найкращий у сегменті, де пілот вибув, або в Q3).</param>
public sealed record QualifyingResult(int Position, Car Car, double LapTimeSeconds)
{
    /// <summary>Найкращий час у Q1 (null — немає часу).</summary>
    public double? Q1 { get; init; }

    /// <summary>Найкращий час у Q2.</summary>
    public double? Q2 { get; init; }

    /// <summary>Найкращий час у Q3.</summary>
    public double? Q3 { get; init; }

    /// <summary>Сегмент, у якому пілот вибув (null — пілот у топ-10 або кваліфікація з одного кола).</summary>
    public QualifyingSegment? EliminatedIn { get; init; }

    /// <summary>Час кола у форматі «1:21.345».</summary>
    public string LapTimeText => FormatLapTime(LapTimeSeconds);

    public static string FormatLapTime(double seconds)
    {
        int minutes = (int)(seconds / 60);
        double rest = seconds - minutes * 60;
        return minutes + ":" + rest.ToString("00.000", System.Globalization.CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Кваліфікація: кожен пілот проїжджає одне швидке коло,
/// за часом формується стартова решітка.
/// </summary>
public sealed class Qualifying
{
    private readonly IRandomSource _random;

    public Qualifying(IRandomSource random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>Проводить кваліфікацію і повертає результати від найшвидшого.</summary>
    public IReadOnlyList<QualifyingResult> Run(IEnumerable<Car> cars, Track track)
    {
        ArgumentNullException.ThrowIfNull(cars);
        ArgumentNullException.ThrowIfNull(track);

        // Кваліфікаційне коло приблизно на 6 % швидше за гоночне (мало палива, нові шини).
        double baseLap = track.TypicalLapSeconds * 0.94;

        return cars
            .Select(car =>
            {
                double spread = 0.006 * (2 - car.Driver.Consistency);   // нестабільний пілот частіше помиляється
                double noise = 1 + (_random.NextDouble() - 0.5) * spread;
                return (car, time: baseLap / car.BasePace * noise);
            })
            .OrderBy(x => x.time)
            .Select((x, i) => new QualifyingResult(i + 1, x.car, x.time))
            .ToList();
    }
}

using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Simulation;

/// <summary>Результат кваліфікації одного пілота.</summary>
public sealed record QualifyingResult(int Position, Car Car, double LapTimeSeconds)
{
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

using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Simulation;
using F1RaceSimulator.Core.Strategies;
using F1RaceSimulator.Core.Tyres;

namespace F1RaceSimulator.Core.Season;

/// <summary>
/// Створює боліди для гонки з даних сезону (патерн Factory).
/// </summary>
public static class CarFactory
{
    /// <summary>Створює по одному боліду для кожного пілота зі стартовими шинами Medium.</summary>
    public static List<Car> CreateCars(SeasonData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return data.Drivers
            .Select(d => new Car(
                d,
                data.GetTeam(d.TeamId),
                TyreFactory.Create(TyreCompound.Medium),
                PitStrategyFactory.Create(d.Strategy, d.PitThreshold)))
            .ToList();
    }

    /// <summary>
    /// Готує стартову решітку за результатами кваліфікації.
    /// Перша десятка стартує на Medium, решта обирає шини за стратегією:
    /// агресивні пілоти — Soft, обережні — Hard.
    /// </summary>
    public static List<Car> CreateGrid(SeasonData data, IReadOnlyList<QualifyingResult> qualifying)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(qualifying);

        var grid = new List<Car>();
        foreach (var q in qualifying.OrderBy(q => q.Position))
        {
            Driver d = q.Car.Driver;
            TyreCompound start = q.Position <= 10
                ? TyreCompound.Medium
                : (q.Car.Strategy is AggressivePitStrategy ? TyreCompound.Soft : TyreCompound.Hard);

            grid.Add(new Car(d, q.Car.Team, TyreFactory.Create(start), q.Car.Strategy));
        }
        return grid;
    }
}

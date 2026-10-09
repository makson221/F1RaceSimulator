using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Simulation;
using F1RaceSimulator.Core.Tyres;

namespace F1RaceSimulator.Core.Strategies;

/// <summary>
/// Базова стратегія «за порогом»: пілот заїжджає, коли стан шин падає нижче
/// порогу і цих шин не вистачить до фінішу. Нову гуму обирає найм'якшу з тих,
/// що гарантовано доїдуть до кінця гонки.
/// </summary>
public class ThresholdPitStrategy : IPitStrategy
{
    /// <summary>Нижче цього стану шин заїзд обов'язковий, незалежно від порогу.</summary>
    public const double CriticalCondition = 8;

    /// <summary>За скільки кіл до фінішу примусово виконати обов'язкову зміну складу.</summary>
    public const int MandatoryStopWindow = 4;

    public ThresholdPitStrategy(double threshold = 25)
    {
        if (threshold is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Поріг повинен бути в межах 0–100 %.");
        Threshold = threshold;
    }

    public virtual string Name => "Обережна";

    public double Threshold { get; }

    /// <summary>
    /// Яку частку ресурсу шини (%) стратегія готова використати до фінішу.
    /// Обережна стратегія залишає запас 20 %.
    /// </summary>
    protected virtual double MaxTyreUsage => 80;

    public virtual bool ShouldPit(Car car, int lapsRemaining, Track track)
    {
        ArgumentNullException.ThrowIfNull(car);
        ArgumentNullException.ThrowIfNull(track);

        if (lapsRemaining <= 1) return false;                        // на останньому колі не заїжджають
        if (car.Tyre.Condition < CriticalCondition) return true;     // шини «закінчилися»

        // Правило: у суху гонку потрібно використати два різні склади шин.
        if (!car.HasUsedTwoCompounds && lapsRemaining <= MandatoryStopWindow) return true;

        if (car.Tyre.Condition >= Threshold) return false;

        // Скільки відсотків шини потрібно, щоб доїхати до фінішу.
        double needed = lapsRemaining * car.Tyre.WearPerLap * track.TyreWearFactor * car.TyreWearRate;
        return needed > car.Tyre.Condition - 5;
    }

    public virtual TyreCompound ChooseCompound(Car car, int lapsRemaining, Track track)
    {
        ArgumentNullException.ThrowIfNull(car);
        ArgumentNullException.ThrowIfNull(track);

        TyreCompound[] fromSoftest = { TyreCompound.Soft, TyreCompound.Medium, TyreCompound.Hard };
        TyreCompound choice = TyreCompound.Hard;
        foreach (var compound in fromSoftest)
        {
            double needed = lapsRemaining * TyreFactory.WearPerLapOf(compound) * track.TyreWearFactor * car.TyreWearRate;
            if (needed < MaxTyreUsage)
            {
                choice = compound;
                break;
            }
        }

        // Якщо другий склад ще не використано — не ставимо той самий, що вже був.
        if (!car.HasUsedTwoCompounds && car.UsedCompounds.Contains(choice))
            choice = choice == TyreCompound.Hard ? TyreCompound.Medium : choice + 1;

        return choice;
    }

    public override string ToString() => $"{Name}, поріг {Threshold:0}%";
}

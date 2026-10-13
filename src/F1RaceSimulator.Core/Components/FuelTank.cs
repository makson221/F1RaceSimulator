namespace F1RaceSimulator.Core.Components;

/// <summary>
/// Паливний бак. Витрата залежить від інтенсивності їзди: у боротьбі болід спалює більше,
/// в режимі економії — менше. Повний бак робить болід важчим і повільнішим.
/// </summary>
public sealed class FuelTank : CarComponent
{
    /// <summary>На скільки секунд за коло повільніший болід з кожним кілограмом палива.</summary>
    public const double SecondsPerKg = 0.03;

    /// <summary>Витрата палива на кілометр траси, кг (умовна модель).</summary>
    public const double KgPerKm = 0.27;

    /// <summary>Резерв палива на початку гонки, кг.</summary>
    public const double ReserveKg = 3.0;

    public override string Name => "Паливо";

    /// <summary>Скільки палива заправлено на старті, кг.</summary>
    public double CapacityKg { get; private set; }

    /// <summary>Поточна кількість палива, кг.</summary>
    public double FuelKg { get; private set; }

    /// <summary>Середня витрата за коло на цій трасі, кг.</summary>
    public double ConsumptionPerLap { get; private set; }

    /// <summary>На скільки кіл вистачить палива при рівному темпі.</summary>
    public double LapsLeft => ConsumptionPerLap <= 0 ? 0 : FuelKg / ConsumptionPerLap;

    /// <summary>Бак порожній.</summary>
    public bool IsEmpty => CapacityKg > 0 && FuelKg <= 0;

    public override ComponentStatus Status =>
        CapacityKg <= 0 ? ComponentStatus.Normal :
        IsEmpty ? ComponentStatus.Critical :
        LapsLeft < 1 ? ComponentStatus.Warning :
        ComponentStatus.Normal;

    public override double Level => CapacityKg <= 0 ? 0 : FuelKg / CapacityKg;

    public override string Summary => $"{FuelKg:0.0} кг (на {LapsLeft:0} кіл)";

    /// <summary>Заправляє болід перед гонкою на всю дистанцію з невеликим резервом.</summary>
    /// <param name="laps">Кількість кіл гонки.</param>
    /// <param name="lapLengthKm">Довжина кола, км.</param>
    public void FillForRace(int laps, double lapLengthKm)
    {
        if (laps <= 0) throw new ArgumentOutOfRangeException(nameof(laps), "Кількість кіл має бути додатною.");
        if (lapLengthKm <= 0) throw new ArgumentOutOfRangeException(nameof(lapLengthKm), "Довжина кола має бути додатною.");

        ConsumptionPerLap = lapLengthKm * KgPerKm;
        CapacityKg = laps * ConsumptionPerLap + ReserveKg;
        FuelKg = CapacityKg;
    }

    /// <summary>
    /// Додатковий час на коло через масу палива відносно половини бака, секунди.
    /// На старті болід повільніший, наприкінці гонки — швидший.
    /// </summary>
    public double LapTimePenaltySeconds => SecondsPerKg * (FuelKg - CapacityKg / 2);

    public override void Update(in ComponentContext context)
    {
        if (context.Laps <= 0) return;
        double rate = 0.94 + 0.12 * context.Push;    // економія 0.97, рівний темп 1.0, боротьба 1.06
        FuelKg = Math.Max(0, FuelKg - ConsumptionPerLap * context.Laps * rate);
    }
}

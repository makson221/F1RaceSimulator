using System.Text.Json.Serialization;

namespace F1RaceSimulator.Core.Models;

/// <summary>
/// Профіль швидкого кола траси: швидкість, передача, оберти, газ і гальмо
/// у 240 точках, рівномірно розподілених уздовж кола. Побудований за справжньою
/// телеметрією F1 (найшвидше коло гонки, набір даних TracingInsights / FastF1).
/// Симуляція рухає боліди за цим профілем: на прямих швидко, у поворотах повільно.
/// </summary>
public sealed class SpeedProfile
{
    private double _harmonic = double.NaN;

    /// <summary>Час кола, на якому записано профіль, секунди.</summary>
    public double LapSeconds { get; init; }

    /// <summary>Довжина кола за телеметрією, метри.</summary>
    public double LapMeters { get; init; }

    /// <summary>Звідки взято телеметрію (етап, сесія, пілот, коло).</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>Швидкість, км/год.</summary>
    public int[] Speed { get; init; } = Array.Empty<int>();

    /// <summary>Передача (1–8).</summary>
    public int[] Gear { get; init; } = Array.Empty<int>();

    /// <summary>Оберти двигуна, об/хв.</summary>
    public int[] Rpm { get; init; } = Array.Empty<int>();

    /// <summary>Педаль газу, %.</summary>
    public int[] Throttle { get; init; } = Array.Empty<int>();

    /// <summary>Гальмо (1 — натиснуте).</summary>
    public int[] Brake { get; init; } = Array.Empty<int>();

    /// <summary>Чи є дані профілю.</summary>
    [JsonIgnore]
    public bool IsEmpty => Speed.Length == 0 || Speed.Any(v => v <= 0);

    /// <summary>Кількість точок профілю.</summary>
    [JsonIgnore]
    public int Count => Speed.Length;

    /// <summary>
    /// Відносна швидкість у точці кола: 1 — середня швидкість кола.
    /// Нормована так, що коло, пройдене з цією швидкістю, триває рівно один «час кола»:
    /// інтеграл від 1 / v по колу дорівнює 1.
    /// </summary>
    public double RelativeSpeedAt(double fraction)
    {
        if (IsEmpty) return 1;
        if (double.IsNaN(_harmonic))
            _harmonic = Speed.Average(v => 1.0 / v);       // середнє 1/v
        return SpeedAt(fraction) * _harmonic;
    }

    /// <summary>Швидкість у точці кола (лінійна інтерполяція), км/год.</summary>
    public double SpeedAt(double fraction)
    {
        if (Speed.Length == 0) return 0;
        double pos = Wrap(fraction) * Count - 0.5;
        int i = (int)Math.Floor(pos);
        double t = pos - i;
        double a = Speed[Index(i)], b = Speed[Index(i + 1)];
        return a + (b - a) * t;
    }

    /// <summary>
    /// Телеметрія боліда в точці кола, якщо він проходить коло за <paramref name="lapSeconds"/>.
    /// Швидкість і оберти масштабуються відносно кола, на якому записано профіль.
    /// </summary>
    public (double Speed, int Gear, int Rpm, int Throttle, bool Brake) SampleAt(double fraction, double lapSeconds)
    {
        if (IsEmpty) return (0, 0, 0, 0, false);
        double ratio = lapSeconds > 0 && LapSeconds > 0 ? LapSeconds / lapSeconds : 1;
        int i = Index((int)Math.Floor(Wrap(fraction) * Count));
        double speed = SpeedAt(fraction) * ratio;
        int rpm = (int)Math.Round(Math.Clamp(Value(Rpm, i) * (0.9 + 0.1 * ratio), 4000, 12500));
        return (speed, Value(Gear, i), rpm, Value(Throttle, i), Value(Brake, i) > 0);
    }

    private int Index(int i) => ((i % Count) + Count) % Count;

    private static int Value(int[] values, int i) => values.Length == 0 ? 0 : values[Math.Min(i, values.Length - 1)];

    private static double Wrap(double v)
    {
        double r = v - Math.Floor(v);
        return r >= 1 ? 0 : r;
    }
}

using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Зона обгону — точка гальмування в кінці прямої. Саме тут пілот, що їде впритул,
/// може пізніше загальмувати і пройти суперника. Зони знаходяться автоматично
/// за профілем швидкості траси: довга пряма з повним газом і сильне гальмування після неї.
/// </summary>
/// <param name="Fraction">Де починається гальмування (частка кола).</param>
/// <param name="Strength">Якість зони від 0 до 1: чим довша пряма і сильніше гальмування, тим легше обігнати.</param>
public readonly record struct OvertakingZone(double Fraction, double Strength)
{
    /// <summary>Зони за замовчуванням для траси без профілю швидкості.</summary>
    public static IReadOnlyList<OvertakingZone> Default { get; } = new[]
    {
        new OvertakingZone(0.05, 0.6), new OvertakingZone(0.30, 0.5),
        new OvertakingZone(0.55, 0.5), new OvertakingZone(0.80, 0.5)
    };

    /// <summary>Знаходить зони обгону за телеметрією швидкого кола.</summary>
    public static IReadOnlyList<OvertakingZone> Detect(SpeedProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        int n = profile.Count;
        if (profile.IsEmpty || profile.Brake.Length != n || profile.Throttle.Length != n)
            return Default;

        var zones = new List<OvertakingZone>();
        for (int i = 0; i < n; i++)
        {
            bool braking = profile.Brake[i] > 0;
            bool wasBraking = profile.Brake[(i - 1 + n) % n] > 0;
            if (!braking || wasBraking) continue;

            // Довжина прямої перед гальмуванням — скільки точок поспіль пілот тиснув газ у підлогу.
            // Перед самим гальмуванням пілот може на мить скинути газ (до 5 точок ≈ 2 % кола),
            // а на прямій — ледь пригальмувати в повороті-«кінку» (до двох точок).
            int skip = 1;
            while (skip <= 5 && profile.Throttle[(i - skip + n) % n] < 90 && profile.Brake[(i - skip + n) % n] == 0)
                skip++;
            int straight = 0, dips = 0;
            while (straight + skip < n)
            {
                int k = (i - skip - straight + n) % n;
                if (profile.Brake[k] > 0) break;
                if (profile.Throttle[k] < 90 && ++dips > 2) break;
                straight++;
            }

            // Наскільки сильно гальмують: від швидкості в кінці прямої до мінімальної в повороті.
            int top = profile.Speed[i], slowest = top;
            for (int j = 0; j < n / 8; j++)
                slowest = Math.Min(slowest, profile.Speed[(i + j) % n]);

            double length = (double)straight / n;
            double strength = Math.Clamp((length - 0.03) / 0.12, 0, 1) * Math.Clamp((top - slowest - 40) / 110.0, 0, 1);
            if (strength >= 0.15)
                zones.Add(new OvertakingZone((i + 0.5) / n, Math.Round(strength, 3)));
        }
        return zones.Count > 0 ? zones : Default;
    }
}

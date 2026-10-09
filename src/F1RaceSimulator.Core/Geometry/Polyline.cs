namespace F1RaceSimulator.Core.Geometry;

/// <summary>
/// Ламана лінія (послідовність точок). Використовується для схеми траси
/// (замкнена лінія) і для піт-лейну (відкрита лінія).
/// Дозволяє отримати точку за часткою пройденої довжини від 0 до 1.
/// </summary>
public sealed class Polyline
{
    private readonly Vec2[] _points;
    private readonly double[] _cumulative; // накопичена довжина до кожної точки

    /// <summary>Створює ламану з набору точок.</summary>
    /// <param name="points">Щонайменше дві точки.</param>
    /// <param name="isClosed">true — остання точка з'єднується з першою (кільце траси).</param>
    public Polyline(IEnumerable<Vec2> points, bool isClosed)
    {
        ArgumentNullException.ThrowIfNull(points);
        var list = points.ToList();
        if (list.Count < 2)
            throw new ArgumentException("Ламана повинна містити щонайменше дві точки.", nameof(points));

        if (isClosed && list[0] != list[^1])
            list.Add(list[0]); // замикаємо кільце

        IsClosed = isClosed;
        _points = list.ToArray();
        _cumulative = new double[_points.Length];
        for (int i = 1; i < _points.Length; i++)
            _cumulative[i] = _cumulative[i - 1] + Vec2.Distance(_points[i - 1], _points[i]);

        Length = _cumulative[^1];
        if (Length <= 0)
            throw new ArgumentException("Довжина ламаної повинна бути більшою за нуль.", nameof(points));
    }

    /// <summary>Створює ламану з масиву координат виду [[x, y], ...] (формат файлу JSON).</summary>
    public static Polyline FromArray(double[][] points, bool isClosed)
    {
        ArgumentNullException.ThrowIfNull(points);
        return new Polyline(points.Select(p => new Vec2(p[0], p[1])), isClosed);
    }

    /// <summary>Чи замкнена ламана.</summary>
    public bool IsClosed { get; }

    /// <summary>Повна довжина ламаної в одиницях схеми.</summary>
    public double Length { get; }

    /// <summary>Точки ламаної (для замкненої — з повтором першої точки в кінці).</summary>
    public IReadOnlyList<Vec2> Points => _points;

    /// <summary>
    /// Повертає точку, що лежить на частці <paramref name="fraction"/> довжини.
    /// Для замкненої ламаної частка береться за модулем 1 (1.25 = 0.25).
    /// Для відкритої — обмежується діапазоном [0; 1].
    /// </summary>
    public Vec2 PointAt(double fraction)
    {
        double f = IsClosed ? Wrap(fraction) : Math.Clamp(fraction, 0, 1);
        double target = f * Length;

        // Двійковий пошук відрізка, на якому лежить шукана точка.
        int lo = 0, hi = _cumulative.Length - 1;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) / 2;
            if (_cumulative[mid] <= target) lo = mid; else hi = mid;
        }

        double segment = _cumulative[hi] - _cumulative[lo];
        double t = segment < 1e-12 ? 0 : (target - _cumulative[lo]) / segment;
        return Vec2.Lerp(_points[lo], _points[hi], t);
    }

    /// <summary>Одиничний вектор напрямку руху в точці з часткою <paramref name="fraction"/>.</summary>
    public Vec2 DirectionAt(double fraction)
    {
        const double step = 0.002;
        Vec2 a = PointAt(fraction - step);
        Vec2 b = PointAt(fraction + step);
        return (b - a).Normalized();
    }

    /// <summary>Повертає дробову частину числа в діапазоні [0; 1).</summary>
    public static double Wrap(double value)
    {
        double r = value - Math.Floor(value);
        return r >= 1 ? 0 : r;
    }
}

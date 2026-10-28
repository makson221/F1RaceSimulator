namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Хронометраж одного боліда: час поточного кола, секторів, останнього і найкращого кола.
/// Приклад композиції: болід (<see cref="Car"/>) містить власний секундомір.
/// </summary>
public sealed class LapTimer
{
    private readonly double?[] _currentSectors = new double?[3];
    private readonly double?[] _lastSectors = new double?[3];
    private readonly double?[] _bestSectors = new double?[3];
    private double _lapStart;
    private double _sectorStart;
    private int _sector = 1;

    /// <summary>Скільки кіл зараховано.</summary>
    public int LapsTimed { get; private set; }

    /// <summary>Час останнього завершеного кола, секунди.</summary>
    public double? LastLap { get; private set; }

    /// <summary>Найкращий час кола, секунди.</summary>
    public double? BestLap { get; private set; }

    /// <summary>Номер кола, на якому показано найкращий час.</summary>
    public int BestLapNumber { get; private set; }

    /// <summary>Поточний сектор (1–3).</summary>
    public int Sector => _sector;

    /// <summary>Сектори поточного кола (null — ще не пройдено).</summary>
    public IReadOnlyList<double?> CurrentSectors => _currentSectors;

    /// <summary>Сектори останнього завершеного кола.</summary>
    public IReadOnlyList<double?> LastSectors => _lastSectors;

    /// <summary>Найкращі сектори.</summary>
    public IReadOnlyList<double?> BestSectors => _bestSectors;

    /// <summary>Скільки секунд триває поточне коло.</summary>
    public double CurrentLapTime(double now) => Math.Max(0, now - _lapStart);

    /// <summary>Починає відлік кола (старт гонки або виїзд на швидке коло).</summary>
    public void Start(double now)
    {
        _lapStart = now;
        _sectorStart = now;
        _sector = 1;
        Array.Clear(_currentSectors, 0, _currentSectors.Length);
    }

    /// <summary>Болід перетнув межу сектора <paramref name="sector"/> (1 або 2).</summary>
    public void CompleteSector(int sector, double now)
    {
        if (sector < 1 || sector > 2 || sector != _sector) return;
        Record(sector, now);
        _sector = sector + 1;
    }

    /// <summary>
    /// Болід перетнув лінію старту/фінішу: коло зараховано.
    /// Повертає час кола.
    /// </summary>
    public double CompleteLap(double now)
    {
        Record(3, now);
        double lap = now - _lapStart;
        LapsTimed++;
        LastLap = lap;
        for (int i = 0; i < 3; i++) _lastSectors[i] = _currentSectors[i];
        if (BestLap == null || lap < BestLap)
        {
            BestLap = lap;
            BestLapNumber = LapsTimed;
        }
        Start(now);
        return lap;
    }

    private void Record(int sector, double now)
    {
        double time = now - _sectorStart;
        _currentSectors[sector - 1] = time;
        if (_bestSectors[sector - 1] == null || time < _bestSectors[sector - 1])
            _bestSectors[sector - 1] = time;
        _sectorStart = now;
    }

    /// <summary>Час у форматі «1:21.345» або «—».</summary>
    public static string Format(double? seconds) =>
        seconds is { } s ? QualifyingResult.FormatLapTime(s) : "—";
}

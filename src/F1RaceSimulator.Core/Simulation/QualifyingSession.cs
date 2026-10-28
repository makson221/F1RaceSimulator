using F1RaceSimulator.Core.Geometry;
using F1RaceSimulator.Core.Models;

namespace F1RaceSimulator.Core.Simulation;

/// <summary>Сегмент кваліфікації.</summary>
public enum QualifyingSegment
{
    Q1 = 1,
    Q2 = 2,
    Q3 = 3
}

/// <summary>Що зараз робить болід у кваліфікації.</summary>
public enum QualifyingStage
{
    /// <summary>У боксі.</summary>
    Garage,

    /// <summary>Виїжджає піт-лейном.</summary>
    PitExit,

    /// <summary>Розігрівальне (вихідне) коло: прогрів шин перед швидким колом.</summary>
    OutLap,

    /// <summary>Швидке коло на час.</summary>
    FlyingLap,

    /// <summary>Повільне коло до піт-лейну.</summary>
    InLap,

    /// <summary>Заїжджає піт-лейном у бокс.</summary>
    PitEntry
}

/// <summary>
/// Учасник кваліфікації: болід, його стан на трасі і найкращі часи в Q1, Q2, Q3.
/// </summary>
public sealed class QualifyingCar
{
    private readonly double?[] _best = new double?[3];

    internal QualifyingCar(Car car) => Car = car;

    public Car Car { get; }

    public QualifyingStage Stage { get; internal set; } = QualifyingStage.Garage;

    /// <summary>Дистанція на трасі в колах (дробова частина — положення на колі).</summary>
    public double Distance { get; internal set; }

    /// <summary>Прогрес у піт-лейні (0..1).</summary>
    public double PitProgress { get; internal set; }

    /// <summary>Сегмент, у якому пілот вибув (null — ще в боротьбі або в топ-10).</summary>
    public QualifyingSegment? EliminatedIn { get; internal set; }

    /// <summary>Поточна позиція в таблиці кваліфікації.</summary>
    public int Position { get; internal set; }

    /// <summary>Телеметрія: швидкість, передача, оберти, газ, гальмо.</summary>
    public CarTelemetry Telemetry { get; internal set; } = CarTelemetry.Stopped;

    /// <summary>Хронометраж поточного швидкого кола і секторів.</summary>
    public LapTimer Timing { get; } = new();

    /// <summary>Останній показаний час кола (null — ще немає або анульовано).</summary>
    public double? LastLap { get; internal set; }

    /// <summary>Скільки виїздів зроблено в поточному сегменті.</summary>
    public int RunsDone { get; internal set; }

    internal double NextRunAt { get; set; } = double.PositiveInfinity;
    internal double TargetLap { get; set; }
    internal double LapStartTime { get; set; }
    internal bool LapSpoiled { get; set; }

    /// <summary>Чи вибув пілот.</summary>
    public bool IsEliminated => EliminatedIn != null;

    /// <summary>Найкращий час у сегменті.</summary>
    public double? Best(QualifyingSegment segment) => _best[(int)segment - 1];

    internal void SetBest(QualifyingSegment segment, double time)
    {
        int i = (int)segment - 1;
        if (_best[i] == null || time < _best[i]) _best[i] = time;
    }

    /// <summary>Стан словами для таблиці.</summary>
    public string StageText => Stage switch
    {
        QualifyingStage.Garage => IsEliminated ? "вибув" : "у боксі",
        QualifyingStage.PitExit => "виїзд",
        QualifyingStage.OutLap => "розігрів",
        QualifyingStage.FlyingLap => "швидке коло",
        QualifyingStage.InLap => "повернення",
        QualifyingStage.PitEntry => "піт-лейн",
        _ => Stage.ToString()
    };

    public override string ToString() => Car.Driver.Code;
}

/// <summary>
/// Кваліфікація у форматі F1: три сегменти Q1 (18 хв), Q2 (15 хв), Q3 (12 хв).
/// Після Q1 і Q2 по шість найповільніших пілотів вибувають (для 22 болідів),
/// у Q3 десятка розігрує поул. Кожен виїзд: піт-лейн → розігрівальне коло →
/// швидке коло на час → повільне коло → бокс. Боліди рухаються трасою так само,
/// як у гонці, — за профілем швидкості траси.
/// </summary>
public sealed class QualifyingSession : ITrackSession
{
    /// <summary>Тривалість сегментів, секунди.</summary>
    public static readonly double[] SegmentSeconds = { 18 * 60, 15 * 60, 12 * 60 };

    /// <summary>Перерва між сегментами (у реальності 7–8 хв; тут скорочено), секунди.</summary>
    public const double BreakSeconds = 150;

    /// <summary>Розігрівальне і повільне кола на стільки повільніші за швидке.</summary>
    public const double SlowLapFactor = 1.35;

    /// <summary>Ймовірність помилки на швидкому колі.</summary>
    public const double MistakeChance = 0.05;

    /// <summary>Ймовірність того, що коло анулюють через вихід за межі траси.</summary>
    public const double TrackLimitsChance = 0.035;

    private readonly List<QualifyingCar> _cars;
    private readonly List<IQualifyingObserver> _observers = new();
    private readonly IRandomSource _random;
    private List<QualifyingCar> _order;
    private double _segmentStart;
    private bool _inBreak;
    private double _breakEnds;
    private bool _flagAnnounced;

    public QualifyingSession(Track track, IEnumerable<Car> cars, IRandomSource random)
    {
        Track = track ?? throw new ArgumentNullException(nameof(track));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        ArgumentNullException.ThrowIfNull(cars);
        _cars = cars.Select(c => new QualifyingCar(c)).ToList();
        if (_cars.Count == 0)
            throw new ArgumentException("Для кваліфікації потрібен хоча б один болід.", nameof(cars));
        _order = _cars.ToList();
        for (int i = 0; i < _order.Count; i++) _order[i].Position = i + 1;
    }

    public Track Track { get; }

    /// <summary>Учасники в порядку поточної таблиці.</summary>
    public IReadOnlyList<QualifyingCar> Standings => _order;

    public IReadOnlyList<QualifyingCar> Cars => _cars;

    /// <summary>Поточний сегмент.</summary>
    public QualifyingSegment Segment { get; private set; } = QualifyingSegment.Q1;

    /// <summary>Час від початку кваліфікації, секунди.</summary>
    public double ElapsedSeconds { get; private set; }

    public bool IsStarted { get; private set; }

    /// <summary>Кваліфікацію завершено, стартова решітка визначена.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>Перерва між сегментами.</summary>
    public bool IsBreak => _inBreak;

    /// <summary>Клетчастий прапор поточного сегмента вже показано.</summary>
    public bool IsSegmentFlag => !_inBreak && SegmentTime >= SegmentSeconds[(int)Segment - 1];

    /// <summary>Час від початку поточного сегмента.</summary>
    public double SegmentTime => ElapsedSeconds - _segmentStart;

    /// <summary>Скільки часу залишилося до кінця сегмента (або перерви).</summary>
    public double TimeRemaining => _inBreak
        ? Math.Max(0, _breakEnds - ElapsedSeconds)
        : Math.Max(0, SegmentSeconds[(int)Segment - 1] - SegmentTime);

    /// <summary>Скільки пілотів проходить у Q2 і в Q3.</summary>
    public int AdvanceCount(QualifyingSegment segment) => segment switch
    {
        QualifyingSegment.Q1 => Math.Min(_cars.Count, 16),
        QualifyingSegment.Q2 => Math.Min(_cars.Count, 10),
        _ => _cars.Count
    };

    public void Subscribe(IQualifyingObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        if (!_observers.Contains(observer)) _observers.Add(observer);
    }

    public void Unsubscribe(IQualifyingObserver observer) => _observers.Remove(observer);

    /// <summary>Починає Q1.</summary>
    public void Start()
    {
        if (IsStarted) throw new InvalidOperationException("Кваліфікацію вже розпочато.");
        IsStarted = true;
        StartSegment(QualifyingSegment.Q1);
    }

    /// <summary>Просуває сесію на <paramref name="seconds"/> секунд.</summary>
    public void Step(double seconds)
    {
        if (!IsStarted) throw new InvalidOperationException("Спочатку викличте Start().");
        while (seconds > 1e-9 && !IsCompleted)
        {
            double dt = Math.Min(Race.MaxStepSeconds, seconds);
            StepInternal(dt);
            seconds -= dt;
        }
    }

    /// <summary>Проводить всю кваліфікацію до кінця (кнопка «Пропустити» і тести).</summary>
    public void RunToEnd()
    {
        if (!IsStarted) Start();
        double limit = SegmentSeconds.Sum() + 3 * BreakSeconds + 3 * 600;
        while (!IsCompleted)
        {
            if (ElapsedSeconds > limit)
                throw new InvalidOperationException("Кваліфікація не завершилася за відведений час.");
            Step(Race.MaxStepSeconds);
        }
    }

    /// <summary>Результати: топ-10 за часом Q3, далі вибулі в Q2, потім у Q1.</summary>
    public IReadOnlyList<QualifyingResult> GetResults() =>
        _order.Select((q, i) => new QualifyingResult(i + 1, q.Car, DecisiveTime(q) ?? 0)
        {
            Q1 = q.Best(QualifyingSegment.Q1),
            Q2 = q.Best(QualifyingSegment.Q2),
            Q3 = q.Best(QualifyingSegment.Q3),
            EliminatedIn = q.EliminatedIn
        }).ToList();

    public IReadOnlyList<CarPosition> GetCarPositions()
    {
        var list = new List<CarPosition>();
        foreach (var q in _order.AsEnumerable().Reverse())
        {
            switch (q.Stage)
            {
                case QualifyingStage.Garage:
                    continue;
                case QualifyingStage.PitExit:
                case QualifyingStage.PitEntry:
                    list.Add(new CarPosition(q.Car, q.PitProgress, true, q.Car.Driver.Code, false));
                    break;
                default:
                    bool flying = q.Stage == QualifyingStage.FlyingLap;
                    list.Add(new CarPosition(q.Car, Polyline.Wrap(q.Distance), false,
                        flying || q.Position <= 3 ? q.Car.Driver.Code : null, flying));
                    break;
            }
        }
        return list;
    }

    // ------------------------------------------------------------------

    private void StartSegment(QualifyingSegment segment)
    {
        Segment = segment;
        _segmentStart = ElapsedSeconds;
        _inBreak = false;
        _flagAnnounced = false;
        double length = SegmentSeconds[(int)segment - 1];

        foreach (var q in _cars)
        {
            q.RunsDone = 0;
            q.Stage = QualifyingStage.Garage;
            q.LastLap = null;
            // Перший виїзд — у першій третині сегмента.
            q.NextRunAt = q.IsEliminated ? double.PositiveInfinity : 40 + _random.NextDouble() * length * 0.3;
        }

        int active = _cars.Count(q => !q.IsEliminated);
        Notify(RaceEventType.QualifyingSegmentStarted,
            $"Почався {segment}: {UkrainianText.Minutes((int)(length / 60))}, на трасі {active} пілотів.");
    }

    private void StepInternal(double dt)
    {
        ElapsedSeconds += dt;

        if (_inBreak)
        {
            if (ElapsedSeconds >= _breakEnds) StartSegment(Segment + 1);
            return;
        }

        double length = SegmentSeconds[(int)Segment - 1];
        if (SegmentTime >= length && !_flagAnnounced)
        {
            _flagAnnounced = true;
            Notify(RaceEventType.QualifyingSegmentFinished, $"Клетчастий прапор {Segment}: швидкі кола, розпочаті до прапора, ще зараховуються.");
        }

        foreach (var q in _cars)
            UpdateCar(q, dt, length);

        UpdateOrder();

        // Сегмент закінчено, коли минув час і всі повернулися в бокси.
        if (SegmentTime >= length && _cars.All(q => q.Stage == QualifyingStage.Garage))
            FinishSegment();
    }

    private void UpdateCar(QualifyingCar q, double dt, double length)
    {
        double quali = QualifyingPace(q.Car);
        switch (q.Stage)
        {
            case QualifyingStage.Garage:
                q.Telemetry = CarTelemetry.Stopped;
                if (!q.IsEliminated && SegmentTime >= q.NextRunAt && SegmentTime < length - 30)
                {
                    q.Stage = QualifyingStage.PitExit;
                    q.PitProgress = Race.BoxPositionFor(q.Car.Team);
                }
                break;

            case QualifyingStage.PitExit:
                q.Telemetry = CarTelemetry.PitLimiter;
                q.PitProgress += dt / Track.PitLaneSeconds;
                if (q.PitProgress >= 1)
                {
                    q.Stage = QualifyingStage.OutLap;
                    q.Distance = Track.Layout.PitExit;            // виїзд з піт-лейну на трасу
                }
                break;

            case QualifyingStage.OutLap:
            {
                double before = q.Distance;
                Drive(q, dt, quali * SlowLapFactor, push: false);
                if (Math.Floor(q.Distance) > Math.Floor(before))
                {
                    // Лінія старту: якщо прапор ще не показано — починається швидке коло.
                    if (SegmentTime < length)
                        BeginFlyingLap(q, CrossingTime(before, q.Distance, dt), quali);
                    else
                        q.Stage = QualifyingStage.InLap;
                }
                break;
            }

            case QualifyingStage.FlyingLap:
            {
                double before = q.Distance;
                Drive(q, dt, q.TargetLap, push: true);
                double[] sectors = Track.Layout.Sectors;
                for (int i = 0; i < sectors.Length && i < 2; i++)
                    if (Race.Crossed(before, q.Distance, sectors[i]))
                        q.Timing.CompleteSector(i + 1, CrossingTime(before, q.Distance, dt, sectors[i]));
                if (Math.Floor(q.Distance) > Math.Floor(before))
                    CompleteFlyingLap(q, CrossingTime(before, q.Distance, dt), length);
                break;
            }

            case QualifyingStage.InLap:
            {
                double before = q.Distance;
                Drive(q, dt, quali * SlowLapFactor * 1.1, push: false);
                if (Race.Crossed(before, q.Distance, Track.Layout.PitEntry))
                {
                    q.Stage = QualifyingStage.PitEntry;
                    q.PitProgress = 0;
                }
                break;
            }

            case QualifyingStage.PitEntry:
            {
                q.Telemetry = CarTelemetry.PitLimiter;
                double box = Race.BoxPositionFor(q.Car.Team);
                q.PitProgress += dt / Track.PitLaneSeconds;
                if (q.PitProgress >= box)
                {
                    q.PitProgress = box;
                    q.Stage = QualifyingStage.Garage;
                    q.RunsDone++;
                    // Другий виїзд — ближче до кінця сегмента, щоб встигнути на найкращу трасу.
                    q.NextRunAt = q.RunsDone >= 2
                        ? double.PositiveInfinity
                        : Math.Max(SegmentTime + 60, length - PitAndOutLap(quali) - 30 - _random.NextDouble() * 100);
                }
                break;
            }
        }
    }

    /// <summary>Рух трасою за профілем швидкості так, щоб коло тривало <paramref name="lapSeconds"/>.</summary>
    private void Drive(QualifyingCar q, double dt, double lapSeconds, bool push)
    {
        double f = Polyline.Wrap(q.Distance);
        double rel = Track.Profile.RelativeSpeedAt(f);
        double laps = dt * rel / lapSeconds;
        rel = Track.Profile.RelativeSpeedAt(f + laps / 2);
        laps = dt * rel / lapSeconds;
        q.Distance += laps;

        var s = Track.Profile.SampleAt(Polyline.Wrap(q.Distance), lapSeconds);
        q.Telemetry = Track.Profile.IsEmpty
            ? new CarTelemetry(push ? 230 : 170, push ? 7 : 5, push ? 11000 : 9000, push ? 100 : 50, false)
            : push
                ? new CarTelemetry(s.Speed, s.Gear, s.Rpm, s.Throttle, s.Brake)
                : new CarTelemetry(s.Speed, s.Gear, (int)(s.Rpm * 0.85), Math.Min(s.Throttle, 60), s.Brake);
    }

    private void BeginFlyingLap(QualifyingCar q, double crossingTime, double quali)
    {
        q.Stage = QualifyingStage.FlyingLap;
        q.LapStartTime = crossingTime;
        q.Timing.Start(crossingTime);

        // Час швидкого кола: темп пілота і боліда, розкид (нестабільні пілоти помиляються частіше),
        // покращення траси впродовж сесії і помилки.
        double spread = 0.006 * (2 - q.Car.Driver.Consistency);
        double noise = 1 + (_random.NextDouble() - 0.5) * spread;
        double evolution = 1 - 0.0025 * ((int)Segment - 1) - 0.0015 * Math.Min(1, SegmentTime / SegmentSeconds[(int)Segment - 1]);
        double target = quali * noise * evolution;
        q.LapSpoiled = false;
        if (_random.NextDouble() < MistakeChance)
        {
            target += 0.6 + _random.NextDouble() * 1.8;
            q.LapSpoiled = true;
        }
        q.TargetLap = target;
    }

    private void CompleteFlyingLap(QualifyingCar q, double crossingTime, double length)
    {
        double lap = Math.Round(q.Timing.CompleteLap(crossingTime), 3);
        string name = q.Car.Driver.FullName;
        string turn = RandomTurn();

        if (_random.NextDouble() < TrackLimitsChance)
        {
            q.LastLap = null;
            Notify(RaceEventType.QualifyingLapDeleted,
                $"Час {QualifyingResult.FormatLapTime(lap)} пілота {name} анульовано: межі траси{turn}.", q.Car);
        }
        else
        {
            q.LastLap = lap;
            double? previous = q.Best(Segment);
            q.SetBest(Segment, lap);
            UpdateOrder();
            string place = $"P{q.Position}";
            string text = q.LapSpoiled
                ? $"{name} помилився{turn}: {QualifyingResult.FormatLapTime(lap)} ({place})."
                : previous == null || lap < previous
                    ? $"{name} — {QualifyingResult.FormatLapTime(lap)}, {place}."
                    : $"{name} — {QualifyingResult.FormatLapTime(lap)}, без покращення.";
            Notify(RaceEventType.QualifyingLap, text, q.Car);
        }

        // Після швидкого кола — повільне коло до боксів.
        q.Stage = QualifyingStage.InLap;
    }

    private string RandomTurn()
    {
        var turns = Track.Layout.Turns;
        if (turns.Length == 0) return string.Empty;
        return " в повороті " + turns[(int)(_random.NextDouble() * turns.Length) % turns.Length].Label;
    }

    private void FinishSegment()
    {
        var active = _order.Where(q => !q.IsEliminated).ToList();
        int advance = AdvanceCount(Segment);

        if (Segment == QualifyingSegment.Q3)
        {
            IsCompleted = true;
            UpdateOrder();
            var pole = _order[0];
            Notify(RaceEventType.QualifyingFinished,
                $"Поул — {pole.Car.Driver.FullName} ({pole.Car.Team.Name}), {QualifyingResult.FormatLapTime(pole.Best(QualifyingSegment.Q3) ?? 0)}.",
                pole.Car);
            return;
        }

        var knocked = active.Skip(advance).ToList();
        foreach (var q in knocked) q.EliminatedIn = Segment;
        UpdateOrder();
        Notify(RaceEventType.QualifyingSegmentFinished,
            $"{Segment} завершено. Вибули: {string.Join(", ", knocked.Select(q => q.Car.Driver.LastName))}.");

        _inBreak = true;
        _breakEnds = ElapsedSeconds + BreakSeconds;
    }

    /// <summary>
    /// Таблиця: спершу ті, хто ще бореться (за часом поточного сегмента), далі вибулі —
    /// у Q2, потім у Q1, кожна група за своїм часом.
    /// </summary>
    private void UpdateOrder()
    {
        _order = _cars
            .OrderBy(q => q.EliminatedIn == null ? 0 : 4 - (int)q.EliminatedIn!)
            .ThenBy(q => SortTime(q) ?? double.MaxValue)
            .ThenBy(q => PreviousTime(q) ?? double.MaxValue)
            .ThenBy(q => _cars.IndexOf(q))
            .ToList();
        for (int i = 0; i < _order.Count; i++) _order[i].Position = i + 1;
    }

    private double? SortTime(QualifyingCar q) => q.EliminatedIn switch
    {
        QualifyingSegment.Q1 => q.Best(QualifyingSegment.Q1),
        QualifyingSegment.Q2 => q.Best(QualifyingSegment.Q2),
        _ => q.Best(Segment)
    };

    /// <summary>Час попереднього сегмента — щоб до перших кіл таблиця не «стрибала».</summary>
    private double? PreviousTime(QualifyingCar q) =>
        q.EliminatedIn == null && Segment > QualifyingSegment.Q1 ? q.Best(Segment - 1) : null;

    private double? DecisiveTime(QualifyingCar q) =>
        q.EliminatedIn switch
        {
            QualifyingSegment.Q1 => q.Best(QualifyingSegment.Q1),
            QualifyingSegment.Q2 => q.Best(QualifyingSegment.Q2),
            _ => q.Best(QualifyingSegment.Q3) ?? q.Best(QualifyingSegment.Q2) ?? q.Best(QualifyingSegment.Q1)
        };

    /// <summary>Кваліфікаційний темп: швидше за гоночний на 6 % (мало палива, нові м'які шини).</summary>
    private double QualifyingPace(Car car) => Track.TypicalLapSeconds * 0.94 / car.BasePace;

    private double PitAndOutLap(double quali) => Track.PitLaneSeconds + quali * SlowLapFactor;

    private double CrossingTime(double before, double after, double dt, double fraction = 0)
    {
        double mark = Math.Floor(after - fraction) + fraction;
        return after > before ? ElapsedSeconds - dt * (after - mark) / (after - before) : ElapsedSeconds;
    }

    private void Notify(RaceEventType type, string message, Car? car = null)
    {
        int seconds = (int)Math.Ceiling(TimeRemaining);
        var e = new RaceEvent(type, (int)Segment, ElapsedSeconds, message, car)
        {
            Label = $"{Segment} · {seconds / 60:00}:{seconds % 60:00}"
        };
        foreach (var observer in _observers.ToList())
            observer.OnQualifyingEvent(this, e);
    }
}

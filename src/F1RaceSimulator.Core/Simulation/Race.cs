using F1RaceSimulator.Core.Components;
using F1RaceSimulator.Core.Models;
using F1RaceSimulator.Core.Tyres;

namespace F1RaceSimulator.Core.Simulation;

/// <summary>
/// Гонка — центральний клас симуляції.
/// Рухає боліди, вирішує піт-стопи через стратегії пілотів,
/// визначає позиції та повідомляє спостерігачів про події (патерн Observer).
/// Клас нічого не знає про інтерфейс програми.
/// </summary>
public sealed class Race
{
    /// <summary>Імовірність технічного сходу за одне коло для одного боліда.</summary>
    public const double DefaultRetirementChancePerLap = 0.0005;

    /// <summary>Максимальний крок симуляції, секунди. Великі кроки діляться на менші.</summary>
    public const double MaxStepSeconds = 1.0;

    /// <summary>Відставання від суперника, при якому болід потрапляє в турбулентне повітря, секунди.</summary>
    public const double DirtyAirGapSeconds = 1.0;

    /// <summary>Втрата темпу в турбулентному повітрі (менше притискної сили): 0.25 %.</summary>
    public const double DirtyAirPaceFactor = 0.9975;

    /// <summary>Повторне попередження того самого типу для пілота — не частіше ніж раз на стільки кіл.</summary>
    public const int WarningIntervalLaps = 10;

    private static readonly string[] RetirementReasons =
    {
        "відмова силової установки",
        "проблема з коробкою передач",
        "гідравліка",
        "аварія",
        "пошкодження підвіски",
        "перегрів гальм"
    };

    private readonly List<Car> _cars;
    private readonly List<IRaceObserver> _observers = new();
    private readonly IRandomSource _random;
    private readonly Dictionary<Car, PaceNoise> _noise = new();
    private readonly Dictionary<(string, string), double> _lastOvertake = new();
    private readonly Dictionary<(string Code, string Kind), double> _warningTimes = new();
    private List<Car> _standings;
    private int _leaderLapsAnnounced;

    /// <summary>Параметри плавного коливання темпу боліда (синусоїда).</summary>
    private readonly record struct PaceNoise(double Phase, double Omega, double Amplitude);

    /// <param name="track">Траса гонки.</param>
    /// <param name="grid">Боліди в порядку стартової решітки (перший — на поулі).</param>
    /// <param name="random">Джерело випадковості.</param>
    /// <param name="retirementChancePerLap">Імовірність сходу за коло (0 — без сходів).</param>
    public Race(Track track, IEnumerable<Car> grid, IRandomSource random,
        double retirementChancePerLap = DefaultRetirementChancePerLap)
    {
        Track = track ?? throw new ArgumentNullException(nameof(track));
        _random = random ?? throw new ArgumentNullException(nameof(random));
        ArgumentNullException.ThrowIfNull(grid);

        _cars = grid.ToList();
        if (_cars.Count == 0)
            throw new ArgumentException("Для гонки потрібен хоча б один болід.", nameof(grid));
        if (track.Laps <= 0)
            throw new ArgumentException("Кількість кіл повинна бути додатною.", nameof(track));
        if (track.TypicalLapSeconds <= 0)
            throw new ArgumentException("Час кола повинен бути додатним.", nameof(track));

        RetirementChancePerLap = retirementChancePerLap;
        _standings = _cars.ToList();
    }

    public Track Track { get; }

    /// <summary>Боліди в порядку стартової решітки.</summary>
    public IReadOnlyList<Car> Cars => _cars;

    /// <summary>Поточна класифікація (перший елемент — лідер).</summary>
    public IReadOnlyList<Car> Standings => _standings;

    public double RetirementChancePerLap { get; }

    /// <summary>Час від старту, секунди.</summary>
    public double ElapsedSeconds { get; private set; }

    public bool IsStarted { get; private set; }

    /// <summary>Лідер отримав клетчастий прапор.</summary>
    public bool IsChequeredFlag { get; private set; }

    /// <summary>Усі боліди фінішували або зійшли.</summary>
    public bool IsFinished { get; private set; }

    public int TotalLaps => Track.Laps;

    public Car Leader => _standings[0];

    /// <summary>Поточне коло лідера (1..TotalLaps).</summary>
    public int CurrentLap => Math.Clamp(Leader.CompletedLaps + 1, 1, TotalLaps);

    /// <summary>Загальна кількість піт-стопів у гонці.</summary>
    public int TotalPitStops => _cars.Sum(c => c.PitStops);

    /// <summary>Підписати спостерігача на події гонки.</summary>
    public void Subscribe(IRaceObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        if (!_observers.Contains(observer))
            _observers.Add(observer);
    }

    /// <summary>Відписати спостерігача.</summary>
    public void Unsubscribe(IRaceObserver observer) => _observers.Remove(observer);

    /// <summary>Розставляє боліди на решітці та дає старт гонці.</summary>
    public void Start()
    {
        if (IsStarted)
            throw new InvalidOperationException("Гонку вже розпочато.");

        for (int i = 0; i < _cars.Count; i++)
        {
            var car = _cars[i];
            // Кожна наступна позиція на решітці приблизно на 8 метрів позаду.
            car.PlaceOnGrid(i + 1, -0.0005 - 0.0015 * i);

            // Паливо на всю дистанцію; двигун уже пройшов кілька етапів сезону (знос 5–40 %).
            car.PrepareForRace(Track, engineWear: 5 + _random.NextDouble() * 35);

            double period = 60 + _random.NextDouble() * 90;             // 60–150 секунд
            _noise[car] = new PaceNoise(
                Phase: _random.NextDouble() * 2 * Math.PI,
                Omega: 2 * Math.PI / period,
                Amplitude: 0.0015 + (1 - car.Driver.Consistency) * 0.015);
        }

        IsStarted = true;
        Notify(RaceEventType.RaceStarted,
            $"Старт! {Track.GrandPrix}, {UkrainianText.Laps(TotalLaps)}. На поулі — {_cars[0].Driver.FullName}.",
            _cars[0]);
    }

    /// <summary>
    /// Просуває симуляцію на <paramref name="seconds"/> секунд гоночного часу.
    /// </summary>
    public void Step(double seconds)
    {
        if (!IsStarted)
            throw new InvalidOperationException("Спочатку викличте Start().");
        if (IsFinished || seconds <= 0) return;

        while (seconds > 1e-9 && !IsFinished)
        {
            double dt = Math.Min(MaxStepSeconds, seconds);
            StepInternal(dt);
            seconds -= dt;
        }
    }

    /// <summary>
    /// Проводить гонку до кінця (для тестів і швидкого розрахунку).
    /// </summary>
    /// <exception cref="InvalidOperationException">Якщо гонка не завершилась за розумний час.</exception>
    public void RunToFinish()
    {
        if (!IsStarted) Start();
        double limit = TotalLaps * Track.TypicalLapSeconds * 3;
        while (!IsFinished)
        {
            if (ElapsedSeconds > limit)
                throw new InvalidOperationException("Гонка не завершилася за відведений час.");
            Step(MaxStepSeconds);
        }
    }

    private void StepInternal(double dt)
    {
        ElapsedSeconds += dt;

        // Відставання від суперника попереду і відрив від суперника позаду (для режимів атаки й захисту).
        var gapAhead = new Dictionary<Car, double>();
        var gapBehind = new Dictionary<Car, double>();
        for (int i = 0; i < _standings.Count; i++)
        {
            Car car = _standings[i];
            gapAhead[car] = GapToCar(_standings, i - 1, car);
            gapBehind[car] = i + 1 < _standings.Count && _standings[i + 1].State == CarState.OnTrack
                ? (car.RaceDistance - _standings[i + 1].RaceDistance) * Track.TypicalLapSeconds
                : double.PositiveInfinity;
        }

        foreach (var car in _cars)
        {
            switch (car.State)
            {
                case CarState.OnTrack:
                    UpdateOnTrack(car, dt, gapAhead[car], gapBehind[car]);
                    break;
                case CarState.InPitLane:
                    UpdateInPitLane(car, dt);
                    break;
                case CarState.PitStop:
                    UpdatePitStop(car, dt);
                    break;
            }
        }

        UpdateStandings();

        if (!IsFinished && _cars.All(c => !c.IsRunning))
        {
            IsFinished = true;
            var winner = _standings[0];
            Notify(RaceEventType.RaceFinished,
                $"Гонку завершено. Перемога — {winner.Driver.FullName} ({winner.Team.Name}).", winner);
        }
    }

    /// <summary>Відставання боліда від суперника з індексом <paramref name="aheadIndex"/>, секунди.</summary>
    private double GapToCar(List<Car> standings, int aheadIndex, Car car) =>
        aheadIndex < 0 || standings[aheadIndex].State != CarState.OnTrack
            ? double.PositiveInfinity
            : (standings[aheadIndex].RaceDistance - car.RaceDistance) * Track.TypicalLapSeconds;

    private void UpdateOnTrack(Car car, double dt, double gapAheadSeconds, double gapBehindSeconds)
    {
        ChooseDrivingMode(car, gapAheadSeconds, gapBehindSeconds);

        // Темп: пілот і болід, шини (знос і температура), двигун, ERS, режим їзди,
        // турбулентне повітря і плавне коливання форми.
        var noise = _noise[car];
        double pace = car.BasePace
                      * car.Tyre.PaceMultiplier
                      * car.Engine.PaceFactor
                      * car.Battery.PaceFactor
                      * car.ModePaceFactor
                      * (car.InDirtyAir ? DirtyAirPaceFactor : 1)
                      * (1 + noise.Amplitude * Math.Sin(ElapsedSeconds * noise.Omega + noise.Phase));
        double lapTime = Track.TypicalLapSeconds / pace + car.Fuel.LapTimePenaltySeconds;
        double lapsDelta = dt / lapTime;

        double before = car.Distance;
        car.Advance(lapsDelta, Track.TyreWearFactor);
        car.UpdateSystems(new ComponentContext(dt, lapsDelta, Track.AirTemperature, car.Push, car.InDirtyAir, IsRacing: true));

        // Спочатку перетин лінії: болід, що доїхав до фінішу на останніх краплях палива, фінішує.
        HandleLineCrossing(car, before, car.Distance, dt);
        if (car.State != CarState.OnTrack) return;

        if (car.Fuel.IsEmpty)
        {
            car.Retire("закінчилося паливо");
            Notify(RaceEventType.Retirement, $"{car.Driver.FullName} — сход (закінчилося паливо).", car);
            return;
        }

        // Технічний сход: ризик зростає зі зносом і перегрівом двигуна, нестабільні пілоти ризикують більше.
        double chance = RetirementChancePerLap * lapsDelta * (2 - car.Driver.Consistency) * car.Engine.FailureRiskMultiplier;
        if (car.Distance > 0 && _random.NextDouble() < chance)
        {
            bool engineFault = car.Engine.Wear >= 60 || car.Engine.Temperature >= PowerUnit.CriticalTemperature;
            string reason = engineFault
                ? RetirementReasons[0]
                : RetirementReasons[(int)(_random.NextDouble() * RetirementReasons.Length) % RetirementReasons.Length];
            car.Retire(reason);
            Notify(RaceEventType.Retirement, $"{car.Driver.FullName} — сход ({reason}).", car);
            return;
        }

        // В'їзд у піт-лейн: стратегія пілота вирішує, чи заїжджати.
        if (!IsChequeredFlag && car.Distance > 0.5 && Crossed(before, car.Distance, Track.Layout.PitEntry))
        {
            int lapsRemaining = TotalLaps - car.CompletedLaps;
            if (car.Strategy.ShouldPit(car, lapsRemaining, Track))
            {
                car.EnterPitLane(Track.Layout.PitSpan, BoxPositionFor(car.Team));
                Notify(RaceEventType.PitEntry,
                    $"{car.Driver.FullName} заїжджає на піт-стоп (шини {car.Tyre.Condition:0}%).", car);
            }
        }
    }

    /// <summary>
    /// Обирає режим їзди. Економія — якщо перегріваються шини чи двигун або не вистачає палива
    /// (з гістерезисом: щоб вийти з економії, показники мають повернутися в норму із запасом).
    /// Атака — якщо суперник попереду ближче ніж за секунду; захист — якщо суперник позаду
    /// ближче ніж за секунду (обидва — з другого кола). Інакше — рівний темп.
    /// Перехід в економію записується в журнал.
    /// </summary>
    private void ChooseDrivingMode(Car car, double gapAheadSeconds, double gapBehindSeconds)
    {
        bool saving = car.Mode == DrivingMode.Saving;
        double lapsToGo = Math.Max(0, TotalLaps - car.Distance);

        // Команда тримає запас палива 0.2 кола; вийти з економії можна із запасом 0.5 кола.
        double fuelMargin = car.Fuel.LapsLeft - lapsToGo;
        bool fuelShort = car.Fuel.CapacityKg > 0 && fuelMargin < (saving ? 0.5 : 0.2);
        bool tyresHot = car.Tyre.Temperature > car.Tyre.OptimalMaxTemperature + (saving ? 0 : 5);
        bool engineHot = car.Engine.Temperature >= PowerUnit.WarningTemperature - (saving ? 5 : 0);

        car.InDirtyAir = gapAheadSeconds < DirtyAirGapSeconds;
        bool racing = car.Distance >= 1;
        DrivingMode mode = fuelShort || tyresHot || engineHot ? DrivingMode.Saving
            : racing && car.InDirtyAir ? DrivingMode.Attack
            : racing && gapBehindSeconds < DirtyAirGapSeconds ? DrivingMode.Defend
            : DrivingMode.Normal;

        if (mode == DrivingMode.Saving && !saving && car.Distance >= 1)
        {
            string name = car.Driver.FullName;
            if (engineHot)
                Warn(car, "engine", $"{name}: двигун перегрівся до {car.Engine.Temperature:0} °C, пілот знижує темп.");
            else if (tyresHot)
                Warn(car, "tyres", $"{name}: перегрів шин {car.Tyre.CompoundName} ({car.Tyre.Temperature:0} °C), пілот береже гуму.");
            else
                Warn(car, "fuel", $"{name} економить паливо: {car.Fuel.FuelKg:0.0} кг на {UkrainianText.Laps((int)Math.Ceiling(lapsToGo))}.");
        }
        car.Mode = mode;
    }

    /// <summary>
    /// Надсилає технічне попередження. Одне й те саме попередження для пілота
    /// повторюється не частіше ніж раз на <see cref="WarningIntervalLaps"/> кіл.
    /// </summary>
    private void Warn(Car car, string kind, string message)
    {
        var key = (car.Driver.Code, kind);
        double interval = WarningIntervalLaps * Track.TypicalLapSeconds;
        if (_warningTimes.TryGetValue(key, out double last) && ElapsedSeconds - last < interval) return;
        _warningTimes[key] = ElapsedSeconds;
        Notify(RaceEventType.TechnicalWarning, message, car);
    }

    private void UpdateInPitLane(Car car, double dt)
    {
        car.UpdateSystems(new ComponentContext(dt, 0, Track.AirTemperature, 0, false, IsRacing: false));
        double before = car.RaceDistance;
        bool reachedBox = car.MoveInPitLane(dt / Track.PitLaneSeconds);
        if (reachedBox)
        {
            double stopSeconds = 2.0 + _random.NextDouble() * 1.5;   // робота механіків 2.0–3.5 с
            car.BeginStop(stopSeconds);
        }

        HandleLineCrossing(car, before, car.RaceDistance, dt);
    }

    private void UpdatePitStop(Car car, double dt)
    {
        car.UpdateSystems(new ComponentContext(dt, 0, Track.AirTemperature, 0, false, IsRacing: false));
        if (!car.TickStop(dt)) return;

        int lapsRemaining = Math.Max(1, TotalLaps - car.CompletedLaps);
        TyreCompound compound = car.Strategy.ChooseCompound(car, lapsRemaining, Track);
        Tyre oldTyre = car.Tyre;
        car.ChangeTyres(TyreFactory.Create(compound));

        Notify(RaceEventType.PitStop,
            $"{car.Driver.FullName} — піт-стоп {car.LastStopDuration:0.0} с, {oldTyre.CompoundName} → {car.Tyre.CompoundName}.", car);
    }

    /// <summary>Перетин лінії старту/фінішу: завершене коло або фініш.</summary>
    private void HandleLineCrossing(Car car, double before, double after, double dt)
    {
        if (after < 1 || Math.Floor(after) <= Math.Floor(before)) return;
        int lapsDone = (int)Math.Floor(after);

        // Точний момент перетину лінії всередині кроку (лінійна інтерполяція).
        double overshoot = after - Math.Floor(after);
        double crossingTime = after > before
            ? ElapsedSeconds - dt * overshoot / (after - before)
            : ElapsedSeconds;

        if (IsChequeredFlag)
        {
            car.Finish(crossingTime);             // після лідера кожен фінішує, перетнувши лінію
            return;
        }

        if (lapsDone >= TotalLaps)
        {
            IsChequeredFlag = true;
            car.Finish(crossingTime);
            Notify(RaceEventType.LeaderFinished,
                $"Клетчастий прапор! Перемога — {car.Driver.FullName} ({car.Team.Name}).", car);
            return;
        }

        if (lapsDone > _leaderLapsAnnounced)
        {
            _leaderLapsAnnounced = lapsDone;
            Notify(RaceEventType.LapCompleted, $"{car.Driver.LastName} лідирує, починається коло {lapsDone + 1}.", car);
        }
    }

    /// <summary>Сортує боліди за пройденою дистанцією і фіксує обгони.</summary>
    private void UpdateStandings()
    {
        var ordered = _cars
            .OrderBy(c => c.State == CarState.Retired ? 1 : 0)
            .ThenByDescending(SortKey)
            .ThenBy(c => c.FinishTime ?? double.MaxValue)
            .ToList();

        for (int i = 0; i < ordered.Count - 1; i++)
        {
            Car a = ordered[i], b = ordered[i + 1];
            bool aWasBehind = a.Position > b.Position;
            if (aWasBehind && a.State == CarState.OnTrack && b.State == CarState.OnTrack && ElapsedSeconds > 3)
            {
                var key = string.CompareOrdinal(a.Driver.Code, b.Driver.Code) < 0
                    ? (a.Driver.Code, b.Driver.Code)
                    : (b.Driver.Code, a.Driver.Code);
                if (!_lastOvertake.TryGetValue(key, out double last) || ElapsedSeconds - last > 30)
                {
                    Notify(RaceEventType.Overtake,
                        $"Обгін: {a.Driver.FullName} виходить на P{i + 1}, позаду — {b.Driver.LastName}.", a, b);
                }
                _lastOvertake[key] = ElapsedSeconds;
            }
        }

        for (int i = 0; i < ordered.Count; i++)
            ordered[i].Position = i + 1;

        _standings = ordered;
    }

    private static double SortKey(Car car) =>
        car.State == CarState.Finished ? car.FinishedLaps : car.RaceDistance;

    /// <summary>Чи перетнула дистанція позначку <paramref name="fraction"/> кола.</summary>
    public static bool Crossed(double before, double after, double fraction) =>
        Math.Floor(after - fraction) > Math.Floor(before - fraction);

    /// <summary>Положення боксу команди в піт-лейні (частка від 0 до 1).</summary>
    public static double BoxPositionFor(Team team) =>
        Math.Clamp(0.76 - (team.PitBoxOrder - 1) * 0.054, 0.1, 0.9);

    /// <summary>Відставання від лідера в секундах (приблизно, за типовим часом кола).</summary>
    public double GapToLeaderSeconds(Car car)
    {
        ArgumentNullException.ThrowIfNull(car);
        Car leader = Leader;
        if (car == leader) return 0;
        if (car.State == CarState.Finished && leader.State == CarState.Finished
            && car.FinishedLaps == leader.FinishedLaps)
            return (car.FinishTime ?? 0) - (leader.FinishTime ?? 0);
        return (SortKey(leader) - SortKey(car)) * Track.TypicalLapSeconds;
    }

    /// <summary>Скільки кіл болід відстає від лідера.</summary>
    public int LapsBehindLeader(Car car)
    {
        ArgumentNullException.ThrowIfNull(car);
        if (car == Leader) return 0;
        if (car.State == CarState.Finished && Leader.State == CarState.Finished)
            return Leader.FinishedLaps - car.FinishedLaps;
        return Math.Max(0, (int)Math.Floor(SortKey(Leader) - SortKey(car)));
    }

    /// <summary>Текст відставання для таблиці: «Лідер», «+3.4», «+1 коло», «Сход».</summary>
    public string GapText(Car car)
    {
        ArgumentNullException.ThrowIfNull(car);
        if (car.State == CarState.Retired) return "Сход";
        if (car == Leader) return "Лідер";
        int lapsBehind = LapsBehindLeader(car);
        if (lapsBehind >= 1) return "+" + UkrainianText.Laps(lapsBehind);
        return "+" + GapToLeaderSeconds(car).ToString("0.0");
    }

    /// <summary>Фінішний протокол (або поточна класифікація, якщо гонка ще триває).</summary>
    public IReadOnlyList<RaceResult> GetResults()
    {
        var results = new List<RaceResult>();
        for (int i = 0; i < _standings.Count; i++)
        {
            Car car = _standings[i];
            int position = i + 1;
            string status;
            int points = 0;

            switch (car.State)
            {
                case CarState.Finished:
                    int down = LapsBehindLeader(car);
                    status = down == 0 ? "Фініш" : "+" + UkrainianText.Laps(down);
                    points = PointsSystem.ForPosition(position);
                    break;
                case CarState.Retired:
                    status = "Сход: " + car.RetirementReason;
                    break;
                default:
                    status = "У гонці";
                    break;
            }

            int laps = car.State == CarState.Finished ? car.FinishedLaps : car.CompletedLaps;
            results.Add(new RaceResult(position, car, laps, status, points));
        }
        return results;
    }

    private void Notify(RaceEventType type, string message, Car? car = null, Car? other = null)
    {
        var raceEvent = new RaceEvent(type, CurrentLapSafe(), ElapsedSeconds, message, car, other);
        foreach (var observer in _observers.ToList())
            observer.OnRaceEvent(this, raceEvent);
    }

    private int CurrentLapSafe() => _standings.Count == 0 ? 1 : CurrentLap;
}
